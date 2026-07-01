namespace GHB.DP2.Application.Features.Procurement.PPettyCash;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.PPettyCash.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdatePPettyCashRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    PettyCashStatus Status,
    DateTimeOffset PPettyCashDate,
    string DepartmentCode,
    int BudgetYear,
    string SupplyMethodCode,
    string SupplyMethodTypeCode,
    string SupplyMethodSpecialTypeCode,
    string? Subject,
    string Source,
    decimal Budget,
    string? Reasons,
    string? PettyCaseDepartmentCode,
    DateTimeOffset? DeliveryDate,
    int? DeliveryPeriod,
    string? DeliveryPeriodTypeCode,
    string? DeliveryConditionCode,
    bool IsAdvance,
    PPettyCashAdvanceResponseDto Advance,
    CategoriesDto[]? Categories,
    VendorResponseDto[]? Vendors,
    GLAccountResponseDto[]? GLAccounts,
    AcceptorRequest[]? Acceptors,
    AssigneeRequest[]? Assignees,
    CommitteeDto[] Committees,
    AttachmentsDto[] Attachments,
    CashType CashType,
    Guid? ApprovalRequestDocumentId,
    bool? IsApprovalRequestDocumentReplace,
    bool? IsFromJorPor001);

public record UpdatePPettyCashResponse(Guid? NewApprovalRequestDocumentFileId);

public class UpdatePPettyCashRequestValidator : Validator<UpdatePPettyCashRequest>
{
    public UpdatePPettyCashRequestValidator()
    {
        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class UpdatePPettyCashEndpoint : PPettyCashEndpointBase<UpdatePPettyCashRequest, Results<Ok<UpdatePPettyCashResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public UpdatePPettyCashEndpoint(
        Dp2DbContext dbContext,
        ILogger<UpdatePPettyCashEndpoint> logger,
        IFileServiceClient fileServiceClient)
        : base(dbContext, logger, fileServiceClient)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("PPettyCash")
             .WithName("UpdatePPettyCash")
             .Produces<UpdatePPettyCashResponse>(StatusCodes.Status200OK)
             .Produces<NotFound>()
             .Accepts<UpdatePPettyCashRequest>("application/json"));
        this.Put("PPettyCash/{id:guid}");
    }

    protected override async ValueTask<Results<Ok<UpdatePPettyCashResponse>, NotFound<string>>> HandleRequestAsync(
        UpdatePPettyCashRequest req,
        CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var data = await this.dbContext.PPettyCashs
                             .Include(pw => pw.Vendors)
                             .ThenInclude(pPettyCashVendor => pPettyCashVendor.VendorParcels)
                             .Include(pPettyCash => pPettyCash.Categories)
                             .Include(pPettyCash => pPettyCash.GLAccounts)
                             .Include(pPettyCash => pPettyCash.Categories)
                             .Include(pPettyCash => pPettyCash.Committees)
                             .Include(p => p.Acceptors)
                             .ThenInclude(a => a.User)
                             .ThenInclude(u => u.Employee)
                             .Include(p => p.Assignees)
                             .Include(pPettyCash => pPettyCash.DocumentHistories)
                             .SingleOrDefaultAsync(p => p.Id == PettyCashId.From(req.Id), ct);

        this.ValidateDocument(req, data);

        if (data is null)
        {
            return TypedResults.NotFound($"PPettyCash with Id {req.Id} not found");
        }

        data.SetPettyCashDate(req.PPettyCashDate)
            .SetDocumentDate(req.PPettyCashDate)
            .SetSupplyMethod(ParameterCode.From(req.SupplyMethodCode), ParameterCode.From(req.SupplyMethodTypeCode), ParameterCode.From(req.SupplyMethodSpecialTypeCode))
            .SetDepartmentId(BusinessUnitId.From(req.DepartmentCode))
            .SetBudgetYear(req.BudgetYear)
            .SetSubject(req.Subject)
            .SetSource(req.Source)
            .SetReasons(req.Reasons)
            .SetDeliveryDate(req.DeliveryDate)
            .SetPettyCaseDepartmentCode(req.PettyCaseDepartmentCode)
            .SetBudget(req.Budget)
            .SetIsAdvance(req.IsAdvance)
            .SetCashType(req.CashType)
            .SetIsFromJorPor001(req.IsFromJorPor001);

        _ = data.SetAdvanceName(req.Advance.AdvanceName)
                    .SetAdvancePayment(
                        req.Advance.AdvancePaymentMethodCode.IsNullOrEmpty()
                            ? null
                            : ParameterCode.From(req.Advance.AdvancePaymentMethodCode!),
                        req.Advance.AdvancePaymentDate)
                    .SetAdvanceBank(
                        req.Advance.AdvanceBankCode != null
                            ? ParameterCode.From(req.Advance.AdvanceBankCode)
                            : null,
                        req.Advance.AdvanceBankAccount,
                        req.Advance.AdvanceBankBranch,
                        req.Advance.AdvanceBankAccountName)
                    .SetAdvanceDetail(req.Advance.AdvanceDetail);

        this.ProcessVendorUpdates(data, req.Vendors);

        ProcessGlAccountUpdates(data, req.GLAccounts);

        ProcessCategoriesUpdates(data, req.Categories);

        await this.ProcessCommitteeUpdates(data, req.Committees, ct);

        await this.ProcessAssigneeUpdates(data, req.Assignees, ct, UserId.From(req.UserId));

        var newApprovalFileId = await this.ProcessStatusAndWorkflowAsync(data, req, ct);

        var deleteFileIds = await this.ManageAttachments(data, req.Attachments, ct);

        foreach (var fileId in deleteFileIds)
        {
            await this.fileServiceClient.DeleteAsync(fileId, CancellationToken.None);
        }

        this.dbContext.PPettyCashs.Update(data);
        await this.dbContext.SaveChangesAsync(ct);

        // Re-replace document from original template — only when form data changed
        if (data.Status is PettyCashStatus.Draft or PettyCashStatus.Edit or PettyCashStatus.Rejected or PettyCashStatus.WaitingApproval)
        {
            var pettyCashReloaded = await this.GetPettyCashById(data.Id, ct);
            var documentService = this.Resolve<IDocumentService>();
            var replaceDto = await this.MapToReplaceDto(
                pettyCashReloaded,
                hasAcceptor: false,
                ct,
                userId: null);

            var isReplace = req.IsApprovalRequestDocumentReplace ?? false;

            var sourceFileId = isReplace
                ? (FileId?)await this.GetDocumentTemplateForReplace(pettyCashReloaded.IsFromJorPor001, ct)
                : pettyCashReloaded.LastedDocument()?.FileId;

            if (sourceFileId is not null)
            {
                var shouldCopy = isReplace || req.Status == PettyCashStatus.WaitingApproval;

                var finalFileId = shouldCopy
                    ? await documentService.CopyDocumentTemplateAsync(
                        sourceFileId.Value,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                        parentDirectory: $"{DocumentTemplateGroups.PettyCash}/{pettyCashReloaded.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                        cancellationToken: ct)
                    : sourceFileId;

                if (finalFileId is null)
                {
                    this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
                }

                if (finalFileId.HasValue)
                {
                    data.AddDocumentHistory(finalFileId.Value);
                    await this.dbContext.SaveChangesAsync(ct);
                }
            }
        }

        return TypedResults.Ok(new UpdatePPettyCashResponse(newApprovalFileId?.Value));
    }

    private async Task<FileId> GetDocumentTemplateForReplace(bool? isFromJorPor001, CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateCode = PettyCashTemplateConstant.GetTemplateCode(isFromJorPor001);

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.PettyCash &&
                dt.IsActive &&
                dt.Code == templateCode,
            ct);

        if (templateFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.TemplateNotFoundForReset,
                StatusCodes.Status404NotFound);
        }

        return templateFileId.Value;
    }

    private void ValidateDocument(UpdatePPettyCashRequest req, PPettyCash? data)
    {
        if (req is { ApprovalRequestDocumentId: not null, Status: PettyCashStatus.WaitingApproval } &&
            (data != null && !data.DocumentHistories.Any()))
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private static PPettyCashVendor CreatePPettyCashVendorFromRequest(PPettyCash data, VendorResponseDto req)
    {
        var vendor = PPettyCashVendor.Create(data.Id)
                                     .SetSequence(req.Sequence)
                                     .SetVendorType(req.VendorType)
                                     .SetVendor(req.SuVendorId != null ? SuVendorId.From(req.SuVendorId.Value) : null, req.TaxNumber, req.VendorName, req.VendorBranchNumber)
                                     .SetBill(
                                         req.VatIncludeTypeCode != null ? ParameterCode.From(req.VatIncludeTypeCode) : null,
                                         ParameterCode.From(req.BillTypeCode),
                                         req.BillTypeOther,
                                         req.BillBookNo,
                                         req.BillDate,
                                         req.BillDetail);

        if (req.VendorParcels is not null)
        {
            CreatePPettyCashVendorParcelFromRequest(vendor!, req.VendorParcels);
        }

        data.AddVendor(vendor);

        return vendor;
    }

    private static void CreatePPettyCashVendorParcelFromRequest(PPettyCashVendor data, IEnumerable<VendorParcelResponseDto> reqParcels)
    {
        data.UpdateVendorsParcels(reqParcels
            .Where(rgl => !string.IsNullOrWhiteSpace(rgl.UnitCode))
            .Map(rgl =>
            data.VendorParcels
                .FirstOrNone(vd => vd.Id == (rgl.Id != null ? PettyCashVendorParcelId.From(rgl.Id.Value) : null))
                .Match(
                    Some: p => p.SetSequence(rgl.Sequence)
                                .SetItem(rgl.Item, rgl.ItemDetail)
                                .SetPrice(
                                    rgl.Quantity,
                                    ParameterCode.From(rgl.UnitCode),
                                    rgl.UnitPrice,
                                    rgl.TotalPrice,
                                    rgl.TotalPriceVat),
                    None: () => CreateVendorParcelFromRequest(data!, rgl))));
    }

    private static PPettyCashVendorParcel CreateVendorParcelFromRequest(PPettyCashVendor data, VendorParcelResponseDto reqParcel)
    {
        var parcel = PPettyCashVendorParcel.Create(data.Id)
                                           .SetSequence(reqParcel.Sequence)
                                           .SetItem(reqParcel.Item, reqParcel.ItemDetail)
                                           .SetPrice(
                                               reqParcel.Quantity,
                                               ParameterCode.From(reqParcel.UnitCode),
                                               reqParcel.UnitPrice,
                                               reqParcel.TotalPrice,
                                               reqParcel.TotalPriceVat);

        data.AddVendorParcels(parcel);

        return parcel;
    }

    private void ProcessVendorUpdates(PPettyCash data, VendorResponseDto[]? vendors)
    {
        if (vendors is null || vendors.Length == 0)
        {
            return;
        }

        data.AddVendors(vendors
            .Where(v => !string.IsNullOrWhiteSpace(v.BillTypeCode))
            .Map(rvd =>
            data.Vendors
                .FirstOrNone(vd => vd.Id == GetPettyCashVendorIdValue(rvd))
                .Match(
                    Some: vendor =>
                    {
                        vendor.SetSequence(rvd.Sequence)
                              .SetVendorType(rvd.VendorType)
                              .SetVendor(
                                  GetSuVendorIdValue(rvd),
                                  rvd.TaxNumber,
                                  rvd.VendorName,
                                  rvd.VendorBranchNumber)
                              .SetBill(
                                  GetParameterCodeValue(rvd),
                                  ParameterCode.From(rvd.BillTypeCode),
                                  rvd.BillTypeOther,
                                  rvd.BillBookNo,
                                  rvd.BillDate,
                                  rvd.BillDetail);

                        if (rvd.VendorParcels.Any())
                        {
                            CreatePPettyCashVendorParcelFromRequest(vendor, rvd.VendorParcels);
                        }

                        return vendor;
                    },
                    None: () => CreatePPettyCashVendorFromRequest(data!, rvd))));
    }

    private static PettyCashVendorId? GetPettyCashVendorIdValue(VendorResponseDto rvd)
    {
        return rvd.Id != null ? PettyCashVendorId.From(rvd.Id.Value) : null;
    }

    private static SuVendorId? GetSuVendorIdValue(VendorResponseDto rvd)
    {
        return rvd.SuVendorId != null ? SuVendorId.From(rvd.SuVendorId.Value) : null;
    }

    private static ParameterCode? GetParameterCodeValue(VendorResponseDto rvd)
    {
        return rvd.VatIncludeTypeCode != null ? ParameterCode.From(rvd.VatIncludeTypeCode) : null;
    }

    private static void ProcessGlAccountUpdates(PPettyCash data, GLAccountResponseDto[]? glAccounts)
    {
        if (glAccounts is not null)
        {
            data.AddGLAccounts(glAccounts.Map(rgl =>
                data.GLAccounts
                    .FirstOrNone(vd => vd.Id == (rgl.Id != null ? PettyCashGLAccountId.From(rgl.Id.Value) : null))
                    .Match(
                        Some: gl => gl.SetGLAccount(
                            rgl.Sequence,
                            rgl.SolId,
                            ParameterCode.From(rgl.BudgetTypeCode),
                            ParameterCode.From(rgl.GLAccountCode),
                            rgl.ProjectNumber,
                            rgl.Amount),
                        None: () => CreateGLAccountFromRequest(data!, rgl))));
        }
    }

    private static void ProcessCategoriesUpdates(PPettyCash data, CategoriesDto[]? categories)
    {
        if (categories is not null)
        {
            var categoriesToDelete =
                data.Categories
                    .Where(x =>
                        categories.Where(r => r.Id.HasValue)
                                  .All(r => x.Id != PettyCashCategoriesId.From(r.Id.Value))).ToList();

            foreach (var item in categoriesToDelete)
            {
                data.RemoveCategories(item);
            }

            categories
                .Where(x => !x.Id.HasValue)
                .Iter(x =>
                    CreateCategoryFromRequest(data, x));
        }
    }

    private async Task ProcessCommitteeUpdates(PPettyCash data, CommitteeDto[] committees, CancellationToken ct)
    {
        if (committees is not null)
        {
            var userIds =
                committees
                    .Map(a => a.UserId)
                    .Map(UserId.From)
                    .ToArray();

            var users =
                await this.dbContext.SuUsers
                          .Where(u => userIds.Contains(u.Id))
                          .ToArrayAsync(CancellationToken.None);

            var committeePositionCodes = committees.Select(a => ParameterCode.From(a.CommitteePositionCode)).ToArray();

            var positionCodes = await this.dbContext.SuParameters
                                          .Where(p => committeePositionCodes.Contains(p.Code))
                                          .ToArrayAsync(ct);

            data.AddCommittees(committees.Map(rct =>
                data.Committees
                    .FirstOrNone(vd => vd.Id == (rct.Id != null ? PPettyCashCommitteeId.From(rct.Id.Value) : null))
                    .Match(
                        Some: committee =>
                        {
                            return committee.Update(
                                rct.FullName,
                                rct.PositionName ?? string.Empty,
                                ParameterCode.From(rct.CommitteePositionCode),
                                rct.CommitteePositionName,
                                rct.GroupType,
                                rct.Sequence);
                        },
                        None: () =>
                        {
                            var user = users.SingleOrDefault(u => u.Id == UserId.From(rct.UserId));

                            if (user is null)
                            {
                                throw new InvalidOperationException($"User not found for ID {rct.UserId}");
                            }

                            return CreateCommitteeFromRequest(data!, rct, user, positionCodes);
                        })));
        }
    }

    private async Task ProcessAssigneeUpdates(PPettyCash data, AssigneeRequest[]? assignees, CancellationToken ct, UserId? sendToAcceptorId = null)
    {
        if (assignees is not null && assignees.Any())
        {
            await this.ManageAssigneeAsync(data, assignees, ct, sendToAcceptorId);
        }
    }

    private async Task<FileId?> ProcessStatusAndWorkflowAsync(PPettyCash data, UpdatePPettyCashRequest req, CancellationToken ct)
    {
        var validStatuses = new[]
        {
            PettyCashStatus.Draft,
            PettyCashStatus.Edit,
            PettyCashStatus.Rejected,
        };

        if (req.Status == data.Status)
        {
            data.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                ActivityLogActionTypeConstant.Update,
                nameof(data.Status)));
        }

        var newApprovalFileId = await this.UpdateDocumentAsync(data, req, ct);

        data.SetStatus(req.Status);

        await this.dbContext.SaveChangesAsync(ct);

        if (req.Status == PettyCashStatus.WaitingApproval && req.Acceptors is not null)
        {
            await this.AddInspectorToAcceptor(data, req.Acceptors, ct, UserId.From(req.UserId));
            SetAcceptorPendingHandler(data, AcceptorType.DepartmentDirectorAgree);

            data.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.SendApprove,
                    ActivityLogActionTypeConstant.SendApprove,
                    data.Status.ToString()));
        }

        if (req.Acceptors is not null && validStatuses.Contains(req.Status))
        {
            await this.MangeAcceptorsAsync(data, req.Acceptors, ct, UserId.From(req.UserId));
        }

        return newApprovalFileId;
    }

    private async Task<FileId?> UpdateDocumentAsync(PPettyCash data, UpdatePPettyCashRequest req, CancellationToken ct)
    {
        var isApprovalRequestDocumentReplace = req.IsApprovalRequestDocumentReplace ?? false;

        var mustSaveApprovalDocument =
            req.ApprovalRequestDocumentId.HasValue &&
            data.Status != PettyCashStatus.WaitingApproval &&
            isApprovalRequestDocumentReplace;

        if (mustSaveApprovalDocument)
        {
            var newFileId = await this.UpdateDocumentHistoryAsync(
                data,
                FileId.From(req.ApprovalRequestDocumentId!.Value),
                isApprovalRequestDocumentReplace,
                ct);

            return newFileId;
        }

        return null;
    }

    private async Task AddInspectorToAcceptor(PPettyCash pPettyCash, AcceptorRequest[] acceptors, CancellationToken ct, UserId? sendToAcceptorId = null)
    {
        if (pPettyCash.Committees is not null)
        {
            var inspectionAcceptors = pPettyCash.Committees
                                                .Where(c => c.GroupType == GroupType.InspectionCommittee)
                                                .Select(c => new AcceptorRequest(
                                                    default,
                                                    AcceptorType.InspectionCommittee,
                                                    c.SuUserId.Value,
                                                    c.Sequence));

            var newAcceptors = acceptors.Filter(x => x.AcceptorType != AcceptorType.InspectionCommittee).Concat(inspectionAcceptors).ToArray();

            await this.MangeAcceptorsAsync(pPettyCash, newAcceptors, ct, sendToAcceptorId);
        }
    }

    private static PPettyCashGLAccount CreateGLAccountFromRequest(PPettyCash data, GLAccountResponseDto rgl)
    {
        var glAcc = PPettyCashGLAccount.Create(data.Id)
                                       .SetGLAccount(
                                           rgl.Sequence,
                                           rgl.SolId,
                                           ParameterCode.From(rgl.BudgetTypeCode),
                                           ParameterCode.From(rgl.GLAccountCode),
                                           rgl.ProjectNumber,
                                           rgl.Amount);

        return glAcc;
    }

    private static void SetAcceptorPendingHandler(
        PPettyCash pPettyCash, AcceptorType acceptorType)
    {
        pPettyCash.Acceptors
                  .Where(w => w.Type == acceptorType)
                  .Iter(r => r.Pending());

        var approver = pPettyCash.Acceptors
                                 .FirstOrDefault(p => p.Sequence == 1 && p.Type == acceptorType);

        if (approver != null)
        {
            approver.SetIsCurrent(true);

            foreach (var targetUserId in approver.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    pPettyCash,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PettyCash.Name, pPettyCash.PettyCashNumber));
            }
        }
    }

    private static PPettyCashCategories CreateCategoryFromRequest(PPettyCash data, CategoriesDto category)
    {
        var cate = PPettyCashCategories.Create(data.Id)
                                       .SetCategoryType(ParameterCode.From(category.CategoryTypeCode));
        data.AddCategory(cate);

        return cate;
    }

    private static PPettyCashCommittee CreateCommitteeFromRequest(PPettyCash pPettyCash, CommitteeDto committee, SuUser user, SuParameter[] paramPositions)
    {
        var committeePositions = paramPositions.SingleOrDefault(u => u.Code == ParameterCode.From(committee.CommitteePositionCode));

        var comm = PPettyCashCommittee.Create(
            pPettyCash.Id,
            committee.GroupType,
            user!.Id,
            user.FullName,
            user.Employee.View!.FullPositionName,
            ParameterCode.From(committee.CommitteePositionCode),
            committeePositions != null ? committeePositions.Label : string.Empty,
            committee.Sequence);

        pPettyCash.AddCommittee(comm);

        return comm;
    }

    private async Task ValidateRequestAsync(UpdatePPettyCashRequest req, CancellationToken ct)
    {
        // Check if department exists
        var department = await this.dbContext.RawBusinessUnits
                                   .FirstOrDefaultAsync(d => d.Id == BusinessUnitId.From(req.DepartmentCode), ct);

        if (department is null)
        {
            this.ThrowError(
                r => r.DepartmentCode,
                $"Department with code {req.DepartmentCode} not found.",
                StatusCodes.Status404NotFound);
        }

        // Validate supply method parameter
        var supplyMethod = await this.dbContext.SuParameters
                                     .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodCode), ct);

        if (supplyMethod is null)
        {
            this.ThrowError(
                r => r.SupplyMethodCode,
                $"Supply method with code {req.SupplyMethodCode} not found.",
                StatusCodes.Status404NotFound);
        }

        if (req.SupplyMethodSpecialTypeCode is not null)
        {
            var supplyMethodSpecialType = await this.dbContext.SuParameters
                                                    .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct);

            if (supplyMethodSpecialType is null)
            {
                this.ThrowError(
                    r => r.SupplyMethodSpecialTypeCode,
                    $"Supply method special type with code {req.SupplyMethodSpecialTypeCode} not found.",
                    StatusCodes.Status404NotFound);
            }
        }

        if (req.IsFromJorPor001 == false)
        {
            return;
        }

        if (req.Committees is null)
        {
            this.ThrowError(
                "กรุณาระบุผู้ขอซื้อขอจ้าง และผู้ตรวจรับ",
                StatusCodes.Status400BadRequest);
        }

        if (req.Committees?.Any(c => c.GroupType == GroupType.ProcurementCommittee) != true)
        {
            this.ThrowError(
                "กรุณาระบุผู้ขอซื้อขอจ้าง",
                StatusCodes.Status400BadRequest);
        }

        if (req.Committees?.Any(c => c.GroupType == GroupType.InspectionCommittee) != true)
        {
            this.ThrowError(
                "กรุณาระบุผู้ตรวจรับ",
                StatusCodes.Status400BadRequest);
        }
    }

    private async Task<IEnumerable<FileId>> ManageAttachments(
        PPettyCash pPettyCash,
        AttachmentsDto[] attachments,
        CancellationToken ct)
    {
        await this.ValidateDocumentTypes(attachments, ct);

        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => (
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic)))
                       .ToArray();

        var deleteIds = pPettyCash.Attachments
                                  .ExceptBy(
                                      fileList.Select(s => s.FileId),
                                      w => w.Id.Value)
                                  .Select(s => s.Id)
                                  .Map(r =>
                                  {
                                      pPettyCash.RemoveAttachmentById(r);

                                      return r;
                                  }) ?? [];

        _ = fileList
            .ExceptBy(
                pPettyCash.Attachments.Select(s => s.Id.Value),
                w => w.FileId)
            .Map(a => PPettyCashAttachments.Create(
                ParameterCode.From(a.DocumentTypeCode),
                FileId.From(a.FileId),
                a.FileName,
                a.Sequence,
                a.IsPublic))
            .Iter(a => pPettyCash.AddAttachment(a));

        return deleteIds;
    }

    private async Task ValidateDocumentTypes(
        AttachmentsDto[] attachments,
        CancellationToken ct)
    {
        var documentTypeCodes = attachments.Select(a => ParameterCode.From(a.DocumentTypeCode)).ToArray();

        var documentTypes = await this.dbContext.SuParameters
                                      .Where(p => documentTypeCodes.Contains(p.Code))
                                      .ToArrayAsync(ct);

        var foundDocumentTypeCodes = documentTypes.Select(p => p.Code).ToArray();

        var missingDocumentTypeCodes = documentTypeCodes.Except(foundDocumentTypeCodes).ToArray();

        if (missingDocumentTypeCodes.Any())
        {
            this.ThrowError(
                r => r.Attachments,
                $"Document types with codes {string.Join(", ", missingDocumentTypeCodes)} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    private async Task ManageAssigneeAsync(
        PPettyCash pPettyCash,
        AssigneeRequest[] assignees,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = pPettyCash.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        if (lastAssigneeUserId is not null)
        {
            sendToAcceptorId = lastAssigneeUserId;
        }

        _ = pPettyCash.Assignees
                      .ExceptBy(
                          assignees
                              .Where(w => w.Id.HasValue)
                              .Select(s => s.Id.Value),
                          a => a.Id.Value)
                      .Iter(r => pPettyCash.RemoveAssigneeById(r.Id));

        _ = assignees.Where(w => w.Id.HasValue)
                     .Join(
                         pPettyCash.Assignees,
                         db => db.Id.Value,
                         payload => payload.Id.Value,
                         (payload, db) => new { db, payload })
                     .Iter(r =>
                     {
                         r.db.SetSequence(r.payload.Sequence);
                         r.db.SetSendToAcceptorId(sendToAcceptorId);
                     });

        var newIds = assignees
                     .Where(w => !w.Id.HasValue)
                     .Select(s => UserId.From(s.UserId))
                     .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => newIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        _ = assignees.Where(w => !w.Id.HasValue)
                     .Join(
                         userData,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => PPettyCashAssignee.Create(
                             a.AssigneeType,
                             u,
                             a.Sequence))
                     .Iter(a =>
                     {
                         a.SetSendToAcceptorId(sendToAcceptorId);
                         pPettyCash.AddAssignee(a);
                     });
    }

    private async Task MangeAcceptorsAsync(
        PPettyCash pPettyCash,
        AcceptorRequest[] acceptors,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = pPettyCash.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        if (lastAssigneeUserId is not null)
        {
            sendToAcceptorId = lastAssigneeUserId;
        }

        _ = pPettyCash.Acceptors
                      .ExceptBy(
                          acceptors
                              .Where(w => w.Id.HasValue)
                              .Select(s => s.Id.Value),
                          a => a.Id.Value)
                      .Iter(r => pPettyCash.RemoveAcceptorById(r.Id));

        _ = acceptors.Where(w => w.Id.HasValue)
                     .Join(
                         pPettyCash.Acceptors,
                         db => db.Id.Value,
                         payload => payload.Id.Value,
                         (payload, db) => new { db, payload })
                     .Iter(r =>
                     {
                         r.db.SetSequence(r.payload.Sequence);
                         r.db.SetSendToAcceptorId(sendToAcceptorId);
                     });

        var newIds = acceptors
                     .Where(w => !w.Id.HasValue)
                     .Select(s => UserId.From(s.UserId))
                     .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => newIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        _ = acceptors.Where(w => !w.Id.HasValue)
                     .Join(
                         userData,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => PPettyCashAcceptor.Create(
                             a.AcceptorType,
                             u,
                             a.Sequence))
                     .Iter(a =>
                     {
                         a.SetSendToAcceptorId(sendToAcceptorId);
                         pPettyCash.AddAcceptor(a);
                     });
    }

    private static async Task SendNotificationAsync(PPettyCash pPettyCash, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(pPettyCash.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PettyCash.Url, pPettyCash.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private async ValueTask UpdateAndReplaceDocumentAsync(
        PPettyCash entity,
        bool? isDocumentIdReplaced,
        CancellationToken ct,
        UserId? creatorUserId)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDraftApprovalDocument = entity.LastedDraftDocument();

        if (lastedDraftApprovalDocument is not null)
        {
            var copiedApprovalFileId = await CopyDocument(lastedDraftApprovalDocument.FileId);

            entity.AddDocumentHistory(copiedApprovalFileId);

            var replaceDto =
                await this.MapToReplaceDto(entity, false, ct, creatorUserId);

            var approvalFileId =
                await ReplaceDocument(
                    lastedDraftApprovalDocument.FileId,
                    isDocumentIdReplaced ?? false);

            entity.AddDocumentHistory(
                approvalFileId,
                true);

            return;

            async Task<FileId> CopyDocument(FileId sourceFileId)
            {
                var replaceDocumentAsync =
                    documentService.CopyDocumentTemplateAsync(
                        sourceFileId,
                        parentDirectory: $"{DocumentTemplateGroups.PettyCash}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                        cancellationToken: ct);

                var fileIdResult = await replaceDocumentAsync;

                if (fileIdResult is null)
                {
                    this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
                }

                return (FileId)fileIdResult;
            }

            async Task<FileId> ReplaceDocument(
                FileId fileId,
                bool isReplace)
            {
                if (!isReplace)
                {
                    return fileId;
                }

                var replaceDocumentAsync =
                    documentService.CopyDocumentTemplateAsync(
                        fileId,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                        parentDirectory: $"{DocumentTemplateGroups.PettyCash}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                        cancellationToken: ct);

                var fileIdResult = await replaceDocumentAsync;

                if (fileIdResult is null)
                {
                    this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
                }

                return (FileId)fileIdResult;
            }
        }
    }
}