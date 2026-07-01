namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateContractInvitationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    ContractInvitationStatus Status,
    IEnumerable<ContractInvitationVendorDto> Vendors,
    IEnumerable<AcceptorRequest> Acceptors);

public class CreateContractInvitationRequestValidator : Validator<CreateContractInvitationRequest>
{
    public CreateContractInvitationRequestValidator()
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
            .WithMessage("ข้อมูลผู้มีอำนาจเห็ตชอบต้องไม่เป็นค่าว่าง")
            .NotEmpty()
            .WithMessage("ข้อมูลผู้มีอำนาจเห็ตชอบต้องไม่เป็นค่าว่าง")
            .When(x => x.Status == ContractInvitationStatus.WaitingApproval);

        this.RuleForEach(x => x.Acceptors)
            .SetValidator(new AcceptorRequestValidator());

        this.RuleForEach(x => x.Acceptors)
            .Must(a => a.AcceptorType == AcceptorType.Approver)
            .WithMessage("ประเภทผู้อนุมัติ/เห็นชอบต้องเป็น ผู้มีอำนาจเห็นชอบ เท่านั้น");
    }
}

public class CreateContractAgreementInvitationEndpoint
    : ContractInvitationEndpointBase<CreateContractInvitationRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateContractAgreementInvitationEndpoint(
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
        this.Post("procurement/{ProcurementId:guid}/contractInvitation");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreateContractInvitationRequest req,
        CancellationToken ct)
    {
        var procurementExisting = await this.ValidateRequestAsync(req, ct);

        if (procurementExisting.SupplyMethodType is null)
        {
            this.ThrowError(
                r => r.ProcurementId,
                $"ไม่พบประเภทวิธีการจัดซื้อจัดจ้างในระบบ",
                StatusCodes.Status404NotFound);
        }

        var newContractInvitation =
            CaContractInvitation.Create(
                ProcurementId.From(req.ProcurementId));

        _ =
            req.Vendors.Map(this.MapToInvitationVendors)
               .Map(newContractInvitation.AddVendor)
               .ToHashSet();

        _ = await req.Vendors.Select(
            async x =>
            {
                var entrepreneursData = newContractInvitation.Vendors.FirstOrDefault(e => e.PurchaseOrderApprovalContractId == PurchaseOrderApprovalContractId.From(x.PurchaseOrderApprovalContractId));

                if (entrepreneursData is null)
                {
                    return unit;
                }

                if (x.Attachments is not null && x.Attachments.Any())
                {
                    await this.ValidateDocumentTypeCode(x.Attachments, ct);
                    await this.UpsertAttachments(newContractInvitation, entrepreneursData, x.Attachments);
                }

                return unit;
            }).SequenceSerial();

        var acceptors =
            await this.CreateAcceptorAsync(
                newContractInvitation.Id,
                req.Status,
                [.. req.Acceptors]);

        acceptors.Iter(a =>
        {
            a.SetSendToAcceptorId(UserId.From(req.UserId));
            newContractInvitation.AddAcceptor(a);
        });

        if (req.Status == ContractInvitationStatus.WaitingApproval)
        {
            newContractInvitation.UpdateStatus(req.Status);
        }

        await this.SetDefaultDocumentTemplate(
            newContractInvitation,
            ct);

        this.dbContext.CaContractInvitations.Add(newContractInvitation);

        await this.dbContext.SaveChangesAsync(ct);

        var contractInvitation = await this.GetById(
            ContractInvitationId.From(newContractInvitation.Id.Value),
            ProcurementId.From(req.ProcurementId),
            ct);

        await this.MapAndReplaceDocumentTemplate(
           contractInvitation,
           ct);

        await SyncSuVendorShareholdersAsync(contractInvitation.Vendors, req.Vendors);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(
            string.Empty,
            newContractInvitation.Id.Value);
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

    private async Task<Procurement> ValidateRequestAsync(
        CreateContractInvitationRequest req,
        CancellationToken ct)
    {
        var procurementExisting =
            await this.dbContext.Procurements
                      .Include(p => p.PurchaseOrderApprovals)
                      .ThenInclude(poa => poa.Contracts)
                      .AsNoTracking()
                      .FirstOrDefaultAsync(
                          p =>
                              p.Id == ProcurementId.From(req.ProcurementId) &&
                              !p.IsDeleted,
                          ct);

        if (procurementExisting is null)
        {
            this.ThrowError(
                r => r.ProcurementId,
                $"ไม่พบการจัดซื้อจัดจ้างในระบบ",
                StatusCodes.Status404NotFound);
        }

        var hasEditPermission =
            await this.HasEditPermission(
                ProcurementId.From(req.ProcurementId),
                req.UserId,
                ct);

        if (!hasEditPermission)
        {
            this.ThrowError(
                r => r.UserId,
                $"ผู้ใช้ {req.UserId} ไม่ใช่ผู้ได้รับมอบหมายให้บันทึกข้อมูลหนังสือเชิญชวนทำสัญญา",
                StatusCodes.Status403Forbidden);
        }

        var purchaseOrderApproverExisting =
            procurementExisting.PurchaseOrderApprovals
                               .FirstOrDefault(p => p is
                               {
                                   IsDeleted: false,
                                   Status: PurchaseOrderApprovalStatus.Assigned
                               });

        if (purchaseOrderApproverExisting is null)
        {
            this.ThrowError(
                r =>
                    r.ProcurementId,
                $"ไม่พบการอนุมัติใบสั่งซื้อ/จ้าง/่เช่า และแจ้งทำสัญญา ในระบบ",
                StatusCodes.Status404NotFound);
        }

        var contractInvitationsExisting =
            await this.dbContext.CaContractInvitations
                      .AsNoTracking()
                      .Where(ci =>
                          ci.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                          !ci.IsDeleted)
                      .FirstOrDefaultAsync(ct);

        if (contractInvitationsExisting is not null)
        {
            this.ThrowError(
                r =>
                    r.ProcurementId,
                $"มีหนังสือเชิญชวนทำสัญญาอยู่แล้วในระบบ",
                StatusCodes.Status409Conflict);
        }

        var purchaseOrderApprovalContractIdsExisting =
            purchaseOrderApproverExisting
                .Contracts
                .Select(c => c.Id)
                .ToArray();

        var reqPurchaseOrderApprovalContractIds =
            req.Vendors
               .Select(v => v.PurchaseOrderApprovalContractId)
               .ToArray();

        var hasInvalidPurchaseOrderApprovalContractIdRequest =
            reqPurchaseOrderApprovalContractIds.Any(id =>
                !purchaseOrderApprovalContractIdsExisting.Contains(PurchaseOrderApprovalContractId.From(id)));

        if (hasInvalidPurchaseOrderApprovalContractIdRequest)
        {
            this.ThrowError(
                r => r.Vendors,
                $"ไม่พบสัญญาในระบบ",
                StatusCodes.Status404NotFound);
        }

        return procurementExisting;
    }

    private CaContractInvitationVendors MapToInvitationVendors(ContractInvitationVendorDto vendor)
    {
        var newVendor = CaContractInvitationVendors.Create(
            new CaContractInvitationVendors.InvitationVendorInfo(
                PurchaseOrderApprovalContractId.From(vendor.PurchaseOrderApprovalContractId),
                vendor.DocumentId,
                vendor.Email ?? string.Empty,
                vendor.ContractName ?? string.Empty,
                vendor.PoNumber ?? string.Empty,
                vendor.ContractNumber ?? string.Empty,
                vendor.AgreedPrice ?? 0,
                vendor.HasContractGuarantee ?? false,
                vendor.ContractGuaranteePercent,
                vendor.GuaranteeAmount,
                vendor.ContractOfficerName ?? string.Empty,
                vendor.ContractOfficerPhone ?? string.Empty,
                vendor.ContractOfficerEmail ?? string.Empty,
                vendor.EgpResult,
                vendor.EgpRemark,
                vendor.EgpDate.NullIfInfinity(),
                vendor.CoiResult,
                vendor.CoiRemark,
                vendor.CoiDate.NullIfInfinity(),
                vendor.WatchListResult,
                vendor.WatchListRemark,
                vendor.WatchListDate.NullIfInfinity(),
                vendor.DocumentTemplateCode is not null ? ParameterCode.From(vendor.DocumentTemplateCode) : null));

        if (vendor.DocumentDate is not null)
        {
            newVendor.SetDocumentDate(vendor.DocumentDate);
        }

        if (vendor.CoiCheckerResult is not null)
        {
            var resultAt = vendor.CoiCheckerResult.ResultAt.NullIfInfinity();
            if (resultAt is not null)
            {
                newVendor.AddChecker(
                    QualificationType.COI,
                    vendor.CoiCheckerResult.Result,
                    resultAt.Value,
                    vendor.CoiCheckerResult.Remark);
            }
        }

        if (vendor.WatchlistCheckerResult is not null)
        {
            var resultAt = vendor.WatchlistCheckerResult.ResultAt.NullIfInfinity();
            if (resultAt is not null)
            {
                newVendor.AddChecker(
                    QualificationType.Watchlist,
                    vendor.WatchlistCheckerResult.Result,
                    resultAt.Value,
                    vendor.WatchlistCheckerResult.Remark);
            }
        }

        if (vendor.Shareholder != null && vendor.Shareholder.Any())
        {
            var shareholders = vendor.Shareholder.SelectMany(s =>
            {
                var checkTypes = s.CheckType != null ? new[] { s.CheckType } : new[] { "COI", "Watchlist" };
                return checkTypes.Select(checkType =>
                {
                    var newShareholder = CaContractInvitationVendorShareholders
                                         .Create(
                                             s.Sequence,
                                             s.TaxId,
                                             s.FirstName,
                                             s.LastName,
                                             s.IsDirector,
                                             s.IsShareholder,
                                             s.IsJuristic)
                                         .SetCheckType(checkType)
                                         .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt.NullIfInfinity())
                                         .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt.NullIfInfinity())
                                         .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt.NullIfInfinity());

                    if (s.CoiCheckerResult is not null)
                    {
                        var resultAt = s.CoiCheckerResult.ResultAt.NullIfInfinity();
                        if (resultAt is not null)
                        {
                            newShareholder.AddChecker(
                                QualificationType.COI,
                                s.CoiCheckerResult.Result,
                                resultAt.Value,
                                s.CoiCheckerResult.Remark);
                        }
                    }

                    if (s.WatchlistCheckerResult is not null)
                    {
                        var resultAt = s.WatchlistCheckerResult.ResultAt.NullIfInfinity();
                        if (resultAt is not null)
                        {
                            newShareholder.AddChecker(
                                QualificationType.Watchlist,
                                s.WatchlistCheckerResult.Result,
                                resultAt.Value,
                                s.WatchlistCheckerResult.Remark);
                        }
                    }

                    return newShareholder;
                });
            }).ToList();

            newVendor.AddCaContractInvitationVendorShareholderList(shareholders);
        }

        return newVendor;
    }

    private async ValueTask<IEnumerable<CaContractInvitationAcceptor>> CreateAcceptorAsync(
        ContractInvitationId contractInvitationId,
        ContractInvitationStatus status,
        AcceptorRequest[] acceptors)
    {
        var acceptorUserIds =
            acceptors.Select(a => UserId.From(a.UserId));

        var users =
            await this.dbContext.SuUsers
                      .Include(e => e.Employee)
                      .ThenInclude(e => e.View)
                      .Where(u => acceptorUserIds.Contains(u.Id))
                      .ToArrayAsync(CancellationToken.None);

        this.ValidateUsers(
            users,
            [.. acceptorUserIds]);

        var acceptorUsers =
            acceptors.Join(
                users,
                acceptorRequest => acceptorRequest.UserId,
                user => user.Id.Value,
                (acceptorRequest, user) =>
                    CaContractInvitationAcceptor.Create(
                        contractInvitationId,
                        acceptorRequest.AcceptorType,
                        user,
                        acceptorRequest.Sequence,
                        status));

        return acceptorUsers;
    }
}