namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Abstract;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

public record UpdateContractDraftResponse(Guid ContractDraftVendorId);

public class UpdateContractDraftEndpoint : ContractDraftEndpointBase<ContractDraftRequest, Results<Ok<UpdateContractDraftResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
#pragma warning disable CS0169 // Field is never used - false positive, used in base class
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;
#pragma warning restore CS0169

    public UpdateContractDraftEndpoint(
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<GetVendorEndpoint> logger,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId}/contract-draft/{ContractDraftId}/vendor/{Id}");
        this.Description(b => b
                              .WithTags(nameof(ContractDraft))
                              .WithName("UpdateContractDraft")
                              .Produces<Ok<UpdateContractDraftResponse>>()
                              .Produces<NotFound<string>>()
                              .WithSummary("Update Contract Draft")
                              .WithDescription(@"Update a contract draft for a procurement.
                                              This endpoint allows you to create or update a contract draft based on the provided procurement ID."));
    }

    protected override async ValueTask<Results<Ok<UpdateContractDraftResponse>, NotFound<string>>> HandleRequestAsync(
        ContractDraftRequest req,
        CancellationToken ct)
    {
        if (!req.IsSaveDraft)
        {
            this.ValidateDocument(req);
        }

        await this.ValidateProcurementAsync(req.ProcurementId, ct);

        var contractDraft =
            await this.QueryContractDraftsAsync(
                req.ProcurementId,
                req.ContractDraftId,
                ct);

        var contractDraftVendor =
            contractDraft
                .Vendors
                .FirstOrDefault(v =>
                    v.Id == ContractDraftVendorId.From(req.Id));

        if (contractDraftVendor is null)
        {
            return TypedResults.NotFound("ไม่พบร่างสัญญาที่ระบุ");
        }

        var isChangeTemplate =
            contractDraftVendor.TemplateCode != ParameterCode.From(req.Template);

        var newAcceptorUserIds = req.Acceptors?
                                    .Select(a => UserId.From(a.UserId))
                                    .ToArray() ?? [];

        var users = req.Acceptors?.Length > 0
            ? await this.dbContext.SuUsers
                        .Include(u => u.Employee)
                        .ThenInclude(e => e.View)
                        .Where(u => newAcceptorUserIds.Contains(u.Id))
                        .ToArrayAsync(ct)
            : null;

        var entity = req.Upsert(contractDraftVendor, users, UserId.From(req.UserId));

        if (req.Status == ContractDraftVendorStatus.Pending
            || req.DocumentDate is not null)
        {
            entity.SetDocumentDate(req.DocumentDate);
        }

        UpdateShareholderList(contractDraftVendor, req);

        if (!req.IsSaveDraft && (isChangeTemplate || contractDraftVendor.DocumentHistories.Count == 0))
        {
            await this.AddDocumentTemplateAsync(contractDraftVendor);
        }

        if (!req.IsSaveDraft && !contractDraftVendor.DocumentHistories.Where(c => c.DocumentType == CaContractDraftVendorDocumentType.ConfidentialContractDraft).Any())
        {
            await this.AddConfidentialDocumentTemplateAsync(contractDraftVendor);
        }

        if (entity.Status != ContractDraftVendorStatus.Pending)
        {
            await this.ManageDocumentHistoryAsync(contractDraftVendor, CaContractDraftVendorDocumentType.ContractDraft, req.IsContractDraftDocumentIdReplace ?? false, ct);
            await this.ManageDocumentHistoryAsync(contractDraftVendor, CaContractDraftVendorDocumentType.ApprovalContractDraft, req.IsApprovalContractDraftDocumentIdReplace ?? false, ct);
            await this.ManageDocumentHistoryAsync(contractDraftVendor, CaContractDraftVendorDocumentType.ConfidentialContractDraft, req.IsConfidentialContractDraftDocumentIdReplace ?? false, ct);
        }

        if (!req.IsSaveDraft)
        {
            await this.ValidateParameter(entity);
        }

        if (entity.Status == ContractDraftVendorStatus.Pending)
        {
            var processingOptions = new DocumentProcessingOptions(
                contractDraft.Procurement.SupplyMethodCode,
                contractDraft.Procurement.SupplyMethodSpecialTypeCode,
                entity.Status == ContractDraftVendorStatus.Pending,
                true,
                false);

            await this.UpdateDocumentAsync(entity, processingOptions, UserId.From(req.UserId), ct);

            var approvers = contractDraftVendor.Acceptors
                                               .Where(p => p.Type == AcceptorType.Approver)
                                               .OrderBy(a => a.Sequence)
                                               .ToList();

            var firstPending = approvers.Select(DelegatorExtensions.DelegatorToAcceptor).FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);

            if (firstPending != null)
            {
                var programName = contractDraft.Procurement.Type == ProcurementType.Rent
                    ? ProgramConstant.BranchSpaceRent.Name
                    : ProgramConstant.ContractDraft.Name;

                foreach (var targetUserId in firstPending.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        contractDraft,
                        targetUserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(NotificationConstant.WaitForLike.Message, programName, contractDraft.Procurement.ProcurementNumber));
                }
            }
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(
            new UpdateContractDraftResponse(
                entity.Id.Value));
    }

    private void ValidateDocument(ContractDraftRequest req)
    {
        var contractDraftDocumentReplace = req.ContractDraftDocumentId == null;

        if (contractDraftDocumentReplace && req.Status == ContractDraftVendorStatus.Pending)
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private async Task ValidateParameter(CaContractDraftVendor contractDraft)
    {
        var properties =
            FindAllParameterCodeProperties(contractDraft)
                .Where(p => !string.IsNullOrWhiteSpace(p.Code.ToString()))
                .ToArray();

        if (!properties.Any())
        {
            return;
        }

        var parameterCodes = properties
                             .Select(p => p.Code)
                             .ToArray();

        var parametersCodes = await this.dbContext
                                        .SuParameters
                                        .Where(p => parameterCodes.Contains(p.Code))
                                        .Select(p => p.Code)
                                        .ToArrayAsync();

        var parameterExists =
            properties
                .Where(p => !parametersCodes.Contains(p.Code))
                .ToArray();

        if (!parameterExists.Any())
        {
            return;
        }

        foreach (var parameter in parameterExists)
        {
            this.AddError($"ไม่พบรหัสพารามิเตอร์ {parameter.Path} = {parameter.Code}");
        }

        this.ThrowIfAnyErrors();
    }

    private static IEnumerable<(string Path, ParameterCode Code)> FindAllParameterCodeProperties(CaContractDraftVendor contractDraft)
    {
        var results = new List<(string Path, ParameterCode Code)>();

        // Find direct ParameterCode properties
        var directProperties = contractDraft.GetType()
                                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(p => p.PropertyType == typeof(ParameterCode?) && p.GetValue(contractDraft) != null)
                                            .Select(p => (Path: p.Name, Code: (ParameterCode)p.GetValue(contractDraft)!));

        results.AddRange(directProperties);

        // Search in nested objects
        var complexProperties = contractDraft.GetType()
                                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p =>
                                                 p.PropertyType.IsClass &&
                                                 p.PropertyType != typeof(string) &&
                                                 !p.PropertyType.IsGenericType &&
                                                 p.GetValue(contractDraft) != null);

        foreach (var prop in complexProperties)
        {
            var nestedObj = prop.GetValue(contractDraft);

            if (nestedObj == null)
            {
                continue;
            }

            var nestedProperties = nestedObj.GetType()
                                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(p => p.PropertyType == typeof(ParameterCode?) && p.GetValue(nestedObj) != null)
                                            .Select(p => (Path: $"{prop.Name}.{p.Name}", Code: (ParameterCode)p.GetValue(nestedObj)!));

            results.AddRange(nestedProperties);
        }

        return results;
    }

    private static void RemoveUnusedShareholders(CaContractDraftVendor vendors, ContractDraftRequest req)
    {
        if (req.Shareholder == null || req.Shareholder.Length() == 0)
        {
            var all = vendors.Shareholders.ToList();

            foreach (var shareholder in all)
            {
                vendors.RemoveCaContractDraftVendorShareholder(shareholder.Id);
            }

            return;
        }

        var allKnownIds = req.Shareholder
            .SelectMany(s => new[] { s.CoiId, s.WatchlistId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var toRemove = vendors.Shareholders.Where(a => !allKnownIds.Contains(a.Id.Value)).ToList();

        foreach (var shareholder in toRemove)
        {
            vendors.RemoveCaContractDraftVendorShareholder(shareholder.Id);
        }
    }

    private static void ApplyCheckerResults(CaContractDraftVendorShareholders shareholder, ShareholderDto dto)
    {
        if (dto.CoiCheckerResult is not null)
        {
            shareholder.AddChecker(
                QualificationType.COI,
                dto.CoiCheckerResult.Result,
                dto.CoiCheckerResult.ResultAt,
                dto.CoiCheckerResult.Remark);
        }

        if (dto.WatchlistCheckerResult is not null)
        {
            shareholder.AddChecker(
                QualificationType.Watchlist,
                dto.WatchlistCheckerResult.Result,
                dto.WatchlistCheckerResult.ResultAt,
                dto.WatchlistCheckerResult.Remark);
        }
    }

    private static CaContractDraftVendorShareholders CreateNewShareholder(ShareholderDto dto, string checkType)
    {
        var newShareholder = CaContractDraftVendorShareholders
                             .Create(dto.Sequence, dto.TaxId, dto.FirstName, dto.LastName, dto.IsDirector, dto.IsShareholder, dto.IsJuristic)
                             .SetCheckType(checkType)
                             .SetWatchlist(dto.WatchlistResult, dto.WatchlistResultRemark, dto.WatchlistResultAt)
                             .SetCoi(dto.CoiResult, dto.CoiResultRemark, dto.CoiResultAt)
                             .SetEgp(dto.EgpResult, dto.EgpRemark, dto.EgpResultAt);

        ApplyCheckerResults(newShareholder, dto);

        return newShareholder;
    }

    private static void UpdateExistingShareholder(CaContractDraftVendorShareholders existing, ShareholderDto dto)
    {
        existing.Update(dto.Sequence, dto.TaxId, dto.FirstName, dto.LastName, dto.IsDirector, dto.IsShareholder, dto.IsJuristic)
                .SetWatchlist(dto.WatchlistResult, dto.WatchlistResultRemark, dto.WatchlistResultAt)
                .SetCoi(dto.CoiResult, dto.CoiResultRemark, dto.CoiResultAt)
                .SetEgp(dto.EgpResult, dto.EgpRemark, dto.EgpResultAt);

        ApplyCheckerResults(existing, dto);
    }

    private static void UpdateShareholderList(CaContractDraftVendor vendors, ContractDraftRequest req)
    {
        RemoveUnusedShareholders(vendors, req);

        if (req.Shareholder == null || req.Shareholder.Length() == 0)
        {
            return;
        }

        foreach (var shareholderDto in req.Shareholder)
        {
            ProcessShareholder(vendors, shareholderDto);
        }
    }

    private static void ProcessShareholder(CaContractDraftVendor vendors, ShareholderDto shareholderDto)
    {
        var processTypes = shareholderDto.CheckType != null
            ? new[] { shareholderDto.CheckType }
            : new[] { "COI", "Watchlist" };

        foreach (var checkType in processTypes)
        {
            var id = checkType == "COI" ? shareholderDto.CoiId : shareholderDto.WatchlistId;
            var existing = FindExistingShareholder(vendors, id);
            if (existing == null)
            {
                vendors.AddCaContractDraftVendorShareholder(CreateNewShareholder(shareholderDto, checkType));
            }
            else
            {
                UpdateExistingShareholder(existing, shareholderDto);
                vendors.UpdateCaContractDraftVendorShareholder(existing);
            }
        }
    }

    private static CaContractDraftVendorShareholders? FindExistingShareholder(
        CaContractDraftVendor vendors,
        Guid? id)
    {
        return id.HasValue
            ? vendors.Shareholders.FirstOrDefault(a => a.Id == CaContractDraftVendorShareholderId.From(id.Value))
            : null;
    }

    private static async Task SendNotificationAsync(CaContractDraft contractDraft, UserId userId, string title, string message)
    {
        var notificationProgram = NotificationProgram.ContractAgreement;

        var programUrl = contractDraft.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.Procurement.Url;

        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  notificationProgram)
              .SetReferenceId(contractDraft.Id.Value)
              .SetLinkUrl(string.Format(programUrl, contractDraft.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}