namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoSap;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoSap.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoSap;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPoSapByIdRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractAmendmentId,
    Guid? Id);

public record PaymentTermDto(
    Guid? Id,
    int? PaymentTermNo,
    int? LeadTime,
    DateTimeOffset? DeliveryDate,
    decimal? InstallmentPercentage,
    decimal? Amount,
    decimal? AdvanceDeductionAmount,
    decimal? PerformanceDeductionAmount,
    string? Title,
    string? Description,
    int? Sequence);

public record ContractInfo(
    string ContractNo,
    Guid? VendorId,
    string VendorName,
    string SapNumber,
    string? PoNumber);

public record PoSapInfo(
    Guid? Id,
    Guid CamContractAmendmentId,
    ContractInfo? OldContract,
    ContractInfo? NewContract,
    CamContractAmendmentPoSapStatus Status,
    AcceptorNoIdResponse[] Acceptors,
    PaymentTermDto[] OldPaymentTerms,
    PaymentTermDto[] PaymentTerms,
    bool HasPermission)
{
    public static PoSapInfo? MapToDto(CamContractAmendmentPoSap? poSap, Guid userId)
    {
        if (poSap == null)
        {
            return null;
        }

        var acceptors = poSap.Acceptors
                             .Select(DelegatorExtensions.DelegatorToAcceptor)
                             .Select(a => new AcceptorNoIdResponse(
                                 a.Id.Value,
                                 a.Type,
                                 a.UserId.Value,
                                 a.Sequence,
                                 a.FullName,
                                 a.PositionName,
                                 a.BusinessUnitName,
                                 a.Status,
                                 a.Remark,
                                 a.ActionAt,
                                 IsCurrent: a.IsCurrentApprover(),
                                 DelegateeUserId: a.Delegatee?.SuUserId.Value))
                             .OrderBy(o => o.Sequence)
                             .ToArray();

        var paymentTerms =
            poSap.CamContractAmendment.PoAddendum
                 .PaymentTerms.Select(p => new PaymentTermDto(
                     p.Id.Value,
                     p.PaymentTermNo,
                     p.LeadTime,
                     p.DeliveryDate,
                     p.InstallmentPercentage,
                     p.Amount,
                     p.AdvanceDeductionAmount,
                     p.PerformanceDeductionAmount,
                     p.Title,
                     p.Description,
                     p.Sequence))
                 .OrderBy(p => p.Sequence)
                 .ToArray();

        var oldPaymentTerms = poSap.CamContractAmendment.ContractDraftVendor.PaymentTerms
                                   .OrderBy(p => p.Sequence)
                                   .Select(p => new PaymentTermDto(
                                       p.Id.Value,
                                       p.PaymentTermNo,
                                       p.LeadTime,
                                       p.DeliveryDate,
                                       p.InstallmentPercentage,
                                       p.Amount,
                                       p.AdvanceDeductionAmount,
                                       p.PerformanceDeductionAmount,
                                       string.Empty,
                                       p.Description,
                                       p.Sequence))
                                   .ToArray();

        return new PoSapInfo(
            poSap.Id.Value,
            poSap.CamContractAmendmentId.Value,
            new ContractInfo(
                poSap.CamContractAmendment.ContractDraftVendor.ContractNumber,
                poSap.CamContractAmendment.ContractDraftVendor.Vendor.VendorId.Value,
                poSap.CamContractAmendment.ContractDraftVendor.Vendor.VendorInfo.EstablishmentName,
                poSap.CamContractAmendment.PoAddendum.SapNumber,
                poSap.CamContractAmendment.PoAddendum.PoNumber),
            new ContractInfo(
                poSap.CamContractAmendment.PoAddendum.ContractNumber,
                poSap.CamContractAmendment.PoAddendum.Vendor.Id.Value,
                poSap.CamContractAmendment.PoAddendum.Vendor.EstablishmentName,
                poSap.CamContractAmendment.PoAddendum.SapNumber,
                poSap.PoSapNumber),
            poSap.Status,
            acceptors,
            oldPaymentTerms,
            paymentTerms,
            poSap.CamContractAmendment.PoAddendum.Acceptors.Where(w => w.Type is AcceptorType.AcceptanceCommittee).Any(x => x.UserId == UserId.From(userId)));
    }

    public static PoSapInfo? MapToDto(CamContractAmendment? camContractAmendment, Guid userId)
    {
        if (camContractAmendment == null)
        {
            return null;
        }

        var paymentTerms =
            camContractAmendment.PoAddendum
                                .PaymentTerms.Select(p => new PaymentTermDto(
                                    p.Id.Value,
                                    p.PaymentTermNo,
                                    p.LeadTime,
                                    p.DeliveryDate,
                                    p.InstallmentPercentage,
                                    p.Amount,
                                    p.AdvanceDeductionAmount,
                                    p.PerformanceDeductionAmount,
                                    p.Title,
                                    p.Description,
                                    p.Sequence))
                                .OrderBy(p => p.Sequence)
                                .ToArray();

        var oldPaymentTerms = camContractAmendment.ContractDraftVendor.PaymentTerms
                                                  .OrderBy(p => p.Sequence)
                                                  .Select(p => new PaymentTermDto(
                                                      p.Id.Value,
                                                      p.PaymentTermNo,
                                                      p.LeadTime,
                                                      (DateTimeOffset)p.DeliveryDate,
                                                      p.InstallmentPercentage,
                                                      p.Amount,
                                                      p.AdvanceDeductionAmount,
                                                      p.PerformanceDeductionAmount,
                                                      string.Empty,
                                                      p.Description,
                                                      p.Sequence))
                                                  .ToArray();

        return new PoSapInfo(
            null,
            camContractAmendment.Id.Value,
            new ContractInfo(
                camContractAmendment.ContractDraftVendor.ContractNumber,
                camContractAmendment.ContractDraftVendor.Vendor.VendorId.Value,
                camContractAmendment.ContractDraftVendor.Vendor.VendorInfo.EstablishmentName,
                camContractAmendment.PoAddendum.SapNumber,
                camContractAmendment.ContractDraftVendor.PoNumber),
            new ContractInfo(
                camContractAmendment.PoAddendum.ContractNumber,
                camContractAmendment.PoAddendum.Vendor.Id.Value,
                camContractAmendment.PoAddendum.Vendor.EstablishmentName,
                camContractAmendment.PoAddendum.SapNumber,
                camContractAmendment.PoAddendum.PoNumber),
            CamContractAmendmentPoSapStatus.Draft,
            [],
            oldPaymentTerms,
            paymentTerms,
            camContractAmendment.PoAddendum.Acceptors.Where(w => w.Type is AcceptorType.AcceptanceCommittee).Any(x => x.UserId == UserId.From(userId)));
    }
}

public class GetPoSapById : PoSapEndpointBase<GetPoSapByIdRequest, Results<Ok<PoSapInfo>, NotFound<string>>>
{
    public GetPoSapById(Dp2DbContext dbContext, ILogger<GetPoSapById> logger)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Get(
            "contract-amendment/{ContractAmendmentId:guid}/po-sap/{Id:guid}",
            "contract-amendment/{ContractAmendmentId:guid}/po-sap");
        this.Description(b => b
                              .WithTags("ContractAmendment/PoSap")
                              .Produces<PoSapInfo>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<PoSapInfo>, NotFound<string>>> HandleRequestAsync(GetPoSapByIdRequest req, CancellationToken ct)
    {
        var poSapAsync =
            req.Id is not null
                ? this.DbContext.CamContractAmendmentPoSaps
                      .Include(camContractAmendmentPoSap => camContractAmendmentPoSap.Acceptors)
                      .Include(camContractAmendmentPoSap => camContractAmendmentPoSap.CamContractAmendment)
                      .ThenInclude(camContractAmendment => camContractAmendment.PoAddendum)
                      .ThenInclude(camContractAmendmentPoAddendum => camContractAmendmentPoAddendum.PaymentTerms)
                      .Include(cam => cam.CamContractAmendment)
                      .ThenInclude(c => c.ContractDraftVendor)
                      .ThenInclude(v => v.Vendor)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(
                          c =>
                              c.Id == CamContractAmendmentPoSapId.From(req.Id!.Value)
                              && c.CamContractAmendmentId == CamContractAmendmentId.From(req.ContractAmendmentId),
                          ct)
                      .Map(s => PoSapInfo.MapToDto(s, req.UserId))
                : this.DbContext.CamContractAmendments
                      .Include(camContractAmendment => camContractAmendment.PoAddendum)
                      .ThenInclude(camContractAmendmentPoAddendum => camContractAmendmentPoAddendum.PaymentTerms)
                      .Include(cam => cam.ContractDraftVendor)
                      .ThenInclude(c => c.Vendor)
                      .FirstOrDefaultAsync(
                          c =>
                              c.Id == CamContractAmendmentId.From(req.ContractAmendmentId),
                          ct)
                      .Map(s => PoSapInfo.MapToDto(s, req.UserId));

        var poSap = await poSapAsync;

        if (poSap is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล PO SAP ที่ระบุ");
        }

        return TypedResults.Ok(poSap);
    }
}