namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public record UpdateContractInvitationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Id,
    bool? IsDocumentReplace,
    ContractInvitationStatus Status,
    IEnumerable<ContractInvitationVendorDto> Vendors,
    IEnumerable<AcceptorRequest> Acceptors);

public class UpdateContractInvitationRequestValidator : Validator<UpdateContractInvitationRequest>
{
    public UpdateContractInvitationRequestValidator()
    {
        this.RuleFor(x => x.ProcurementId)
            .NotEmpty().WithMessage("ต้องระบุรหัสการจัดซื้อจัดจ้าง");

        this.RuleFor(x => x.Status)
            .IsInEnum().WithMessage("สถานะไม่ถูกต้อง");

        this.RuleFor(x => x.Vendors)
            .NotNull().WithMessage("ต้องระบุผู้ค้าไม่น้อยกว่า 1 ราย");

        this.RuleForEach(x => x.Vendors)
            .SetValidator(new ContractInvitationVendorDtoValidator())
            .When(x => x.Status == ContractInvitationStatus.WaitingApproval);

        this.RuleFor(x => x.Acceptors)
            .NotNull()
            .WithMessage("ข้อมูลผู้มีอำนาจเห็นชอบต้องไม่เป็นค่าว่าง")
            .NotEmpty()
            .WithMessage("ข้อมูลผู้มีอำนาจเห็นชอบต้องไม่เป็นค่าว่าง")
            .When(x => x.Status == ContractInvitationStatus.WaitingApproval);

        this.RuleForEach(x => x.Acceptors)
            .SetValidator(new AcceptorRequestValidator());

        this.RuleForEach(x => x.Acceptors)
            .Must(a => a.AcceptorType == AcceptorType.Approver)
            .WithMessage("ประเภทผู้อนุมัติ/เห็นชอบต้องเป็น ผู้มีอำนาจเห็นชอบ เท่านั้น");
    }
}

public class UpdateContractAgreementInvitationEndpoint
    : ContractInvitationEndpointBase<UpdateContractInvitationRequest, Results<Ok<ContractInvitationId>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateContractAgreementInvitationEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient,
        ILogger<UpsertAttachmentsEndpoint> logger)
        : base(dbContext, operationService, fileServiceClient, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("ContractAgreement/ContractInvitation"));
        this.Put("procurement/{ProcurementId:guid}/contractInvitation/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<ContractInvitationId>, NotFound<string>>> HandleRequestAsync(
        UpdateContractInvitationRequest req,
        CancellationToken ct)
    {
        var contractInvitationExisting = await this.ValidateRequestAsync(req, ct);

        await this.UpdateContractInvitationVendorsAsync(
            contractInvitationExisting,
            [.. req.Vendors],
            req.Status,
            req.IsDocumentReplace,
            ct);

        await SyncSuVendorShareholdersAsync(contractInvitationExisting.Vendors, req.Vendors);

        await this.UpsertAcceptorAsync(
            contractInvitationExisting,
            [.. req.Acceptors],
            ct,
            UserId.From(req.UserId));

        contractInvitationExisting.UpdateStatus(req.Status);

        if (req.Status is ContractInvitationStatus.WaitingApproval)
        {
            // foreach (var vendor in contractInvitationExisting.Vendors)
            // {
            //    await this.UpdateDocumentAsync(vendor, req.IsDocumentReplace ?? false, false, ct);
            // }
            var approvers = contractInvitationExisting.Acceptors
                                      .Where(p => p.Type == AcceptorType.Approver)
                                      .OrderBy(a => a.Sequence)
                                      .ToList();

            var firstPending = approvers.Select(DelegatorExtensions.DelegatorToAcceptor).FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);

            if (firstPending != null)
            {
                var programName = contractInvitationExisting.Procurement.Type == ProcurementType.Rent
                    ? ProgramConstant.BranchSpaceRent.Name
                    : ProgramConstant.ContractInvitation.Name;

                foreach (var targetUserId in firstPending.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(contractInvitationExisting, targetUserId, NotificationConstant.WaitForLike.Title, string.Format(NotificationConstant.WaitForLike.Message, programName, contractInvitationExisting.Procurement.ProcurementNumber));
                }
            }
        }

        this.dbContext.CaContractInvitations.Update(contractInvitationExisting);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(contractInvitationExisting.Id);
    }

    private static Task SyncSuVendorShareholdersAsync(
        IEnumerable<CaContractInvitationVendors> existingVendors,
        IEnumerable<ContractInvitationVendorDto> vendorDtos)
    {
        foreach (var vendorDto in vendorDtos ?? [])
        {
            var existingVendor = (existingVendors ?? []).FirstOrDefault(v =>
                v.PurchaseOrderApprovalContractId ==
                PurchaseOrderApprovalContractId.From(vendorDto.PurchaseOrderApprovalContractId));

            var vendorId = existingVendor?.PurchaseOrderApprovalContract?.Entrepreneur?.SuVendor?.Id;
            if (vendorId is null)
            {
                continue;
            }
        }

        return Task.CompletedTask;
    }

    private async Task<CaContractInvitation> ValidateRequestAsync(
        UpdateContractInvitationRequest req,
        CancellationToken ct)
    {
        var contractInvitationExisting =
            await this.GetById(
                ContractInvitationId.From(req.Id),
                ProcurementId.From(req.ProcurementId),
                ct);

        var canEdit =
            contractInvitationExisting.Status is
                ContractInvitationStatus.Draft or
                ContractInvitationStatus.Edit or
                ContractInvitationStatus.Rejected or
                ContractInvitationStatus.WaitingApproval;

        var isApproved =
            contractInvitationExisting.Status ==
            ContractInvitationStatus.WaitingApproval &&
            contractInvitationExisting.Acceptors
                                      .Any(x => x.Status != AcceptorStatus.Pending);

        if (!canEdit || isApproved)
        {
            this.ThrowError(
                r =>
                    req.Id,
                $"หนังสือเชิญชวนทำสัญญาที่ระบุไม่อยู่ในสถานะที่สามารถแก้ไขได้ (สถานะปัจจุบัน: {contractInvitationExisting.Status})",
                StatusCodes.Status409Conflict);
        }

        return contractInvitationExisting;
    }

    private async Task UpdateContractInvitationVendorsAsync(
        CaContractInvitation contractInvitationExisting,
        ContractInvitationVendorDto[] vendorsReq,
        ContractInvitationStatus status,
        bool? isDocumentReplace,
        CancellationToken ct)
    {
        var pairs =
            contractInvitationExisting
                .Vendors
                .Join(
                    vendorsReq.Where(vReq => vReq.Id.HasValue),
                    vendorExisting => vendorExisting.Id.Value,
                    vendorReq => vendorReq.Id,
                    (vendorExisting, vendorReq) => (vendorExisting, vendorReq));

        foreach (var (vendorExisting, vendorReq) in pairs)
        {
            var vendorInfo =
                new CaContractInvitationVendors.InvitationVendorInfo(
                    PurchaseOrderApprovalContractId.From(vendorReq.PurchaseOrderApprovalContractId),
                    vendorReq.DocumentId,
                    vendorReq.Email ?? string.Empty,
                    vendorReq.ContractName ?? string.Empty,
                    vendorReq.PoNumber ?? string.Empty,
                    vendorReq.ContractNumber ?? string.Empty,
                    vendorReq.AgreedPrice ?? 0,
                    vendorReq.HasContractGuarantee ?? false,
                    vendorReq.ContractGuaranteePercent,
                    vendorReq.GuaranteeAmount,
                    vendorReq.ContractOfficerName ?? string.Empty,
                    vendorReq.ContractOfficerPhone ?? string.Empty,
                    vendorReq.ContractOfficerEmail ?? string.Empty,
                    vendorReq.EgpResult,
                    vendorReq.EgpRemark,
                    vendorReq.EgpDate,
                    vendorReq.CoiResult,
                    vendorReq.CoiRemark,
                    vendorReq.CoiDate,
                    vendorReq.WatchListResult,
                    vendorReq.WatchListRemark,
                    vendorReq.WatchListDate,
                    vendorReq.DocumentTemplateCode is not null ? ParameterCode.From(vendorReq.DocumentTemplateCode) : null);

            vendorExisting.Update(vendorInfo);

            if (vendorReq.CoiCheckerResult is not null)
            {
                vendorExisting
                    .AddChecker(
                        QualificationType.COI,
                        vendorReq.CoiCheckerResult.Result,
                        vendorReq.CoiCheckerResult.ResultAt,
                        vendorReq.CoiCheckerResult.Remark);
            }

            if (vendorReq.WatchlistCheckerResult is not null)
            {
                vendorExisting
                    .AddChecker(
                        QualificationType.Watchlist,
                        vendorReq.WatchlistCheckerResult.Result,
                        vendorReq.WatchlistCheckerResult.ResultAt,
                        vendorReq.WatchlistCheckerResult.Remark);
            }

            UpdateShareholderList(vendorExisting, vendorReq);

            if (status == ContractInvitationStatus.WaitingApproval
                || vendorReq.DocumentDate is not null)
            {
                vendorExisting.SetDocumentDate(vendorReq.DocumentDate);
            }

            if (vendorReq.DocumentId.HasValue)
            {
                await this.UpdateDocumentAsync(vendorExisting, isDocumentReplace ?? false, false, status, ct);
            }
        }
    }

    private async Task UpsertAcceptorAsync(
        CaContractInvitation contractInvitationExisting,
        AcceptorRequest[] acceptorsRequest,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        _ = contractInvitationExisting.Acceptors.Where(w => !acceptorsRequest.Select(s => s.Id).Contains(w.Id.Value))
                                      .Iter(s => contractInvitationExisting.RemoveAcceptor(s));

        var userIdsIncoming =
            acceptorsRequest.Map(s => s.UserId)
                            .Map(UserId.From)
                            .ToArray();

        var usersIncoming =
            await this.dbContext.SuUsers
                      .Include(r => r.Employee)
                      .ThenInclude(r => r.View)
                      .Where(w => userIdsIncoming.Contains(w.Id))
                      .ToArrayAsync(ct);

        var userNotExistsInDb
            = userIdsIncoming
              .Except(usersIncoming.Map(u => u.Id))
              .ToArray();

        if (userNotExistsInDb.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userNotExistsInDb)} not found.",
                StatusCodes.Status404NotFound);
        }

        var newAcceptors =
            acceptorsRequest.Where(ar => !ar.Id.HasValue)
                            .Join(
                                usersIncoming,
                                a => a.UserId,
                                u => u.Id.Value,
                                (a, u) => CaContractInvitationAcceptor.Create(
                                    contractInvitationExisting.Id,
                                    a.AcceptorType,
                                    u,
                                    a.Sequence,
                                    contractInvitationExisting.Status))
                            .ToHashSet();

        _ = contractInvitationExisting.Acceptors
                                      .Join(
                                          acceptorsRequest.Where(w => w.Id.HasValue),
                                          db => db.Id.Value,
                                          payload => payload.Id,
                                          (db, payload) =>
                                          {
                                              db.SetSequence(payload.Sequence)
                                                .SetStatus(
                                                    contractInvitationExisting.Status is ContractInvitationStatus.WaitingApproval
                                                        ? AcceptorStatus.Pending
                                                        : db.Status);

                                              db.SetSendToAcceptorId(sendToAcceptorId);

                                              return db;
                                          }).ToHashSet();

        newAcceptors.Iter(a =>
        {
            a.SetSendToAcceptorId(sendToAcceptorId);
            contractInvitationExisting.AddAcceptor(a);
        });
    }

    private static void RemoveUnusedShareholders(CaContractInvitationVendors vendors, ContractInvitationVendorDto req)
    {
        if (req.Shareholder == null || req.Shareholder.Length() == 0)
        {
            var all = vendors.Shareholders.ToList();
            foreach (var shareholder in all)
            {
                vendors.RemoveCaContractInvitationVendorShareholder(shareholder.Id);
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
            vendors.RemoveCaContractInvitationVendorShareholder(shareholder.Id);
        }
    }

    private static void ApplyCheckerResults(CaContractInvitationVendorShareholders shareholder, ShareholderDto dto)
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

    private static CaContractInvitationVendorShareholders CreateNewShareholder(ShareholderDto dto, string checkType)
    {
        var newShareholder = CaContractInvitationVendorShareholders
            .Create(dto.Sequence, dto.TaxId, dto.FirstName, dto.LastName, dto.IsDirector, dto.IsShareholder, dto.IsJuristic)
            .SetCheckType(checkType)
            .SetWatchlist(dto.WatchlistResult, dto.WatchlistResultRemark, dto.WatchlistResultAt)
            .SetCoi(dto.CoiResult, dto.CoiResultRemark, dto.CoiResultAt)
            .SetEgp(dto.EgpResult, dto.EgpRemark, dto.EgpResultAt);

        ApplyCheckerResults(newShareholder, dto);
        return newShareholder;
    }

    private static void UpdateExistingShareholder(CaContractInvitationVendorShareholders existing, ShareholderDto dto)
    {
        existing.Update(dto.Sequence, dto.TaxId, dto.FirstName, dto.LastName, dto.IsDirector, dto.IsShareholder, dto.IsJuristic)
            .SetWatchlist(dto.WatchlistResult, dto.WatchlistResultRemark, dto.WatchlistResultAt)
            .SetCoi(dto.CoiResult, dto.CoiResultRemark, dto.CoiResultAt)
            .SetEgp(dto.EgpResult, dto.EgpRemark, dto.EgpResultAt);

        ApplyCheckerResults(existing, dto);
    }

    private static void UpdateShareholderList(CaContractInvitationVendors vendors, ContractInvitationVendorDto req)
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

    private static void ProcessShareholder(CaContractInvitationVendors vendors, ShareholderDto shareholderDto)
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
                vendors.AddCaContractInvitationVendorShareholder(CreateNewShareholder(shareholderDto, checkType));
            }
            else
            {
                UpdateExistingShareholder(existing, shareholderDto);
                vendors.UpdateCaContractInvitationVendorShareholder(existing);
            }
        }
    }

    private static CaContractInvitationVendorShareholders? FindExistingShareholder(
        CaContractInvitationVendors vendors,
        Guid? id)
    {
        return id.HasValue
            ? vendors.Shareholders.FirstOrDefault(a => a.Id == CaContractInvitationVendorShareholderId.From(id.Value))
            : null;
    }

    private static async Task SendNotificationAsync(CaContractInvitation contractInvitation, UserId userId, string title, string message)
    {
        var notificationProgram = NotificationProgram.ContractAgreement;

        var programUrl = contractInvitation.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.Procurement.Url;

        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  notificationProgram)
              .SetReferenceId(contractInvitation.Id.Value)
              .SetLinkUrl(string.Format(programUrl, contractInvitation.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}
