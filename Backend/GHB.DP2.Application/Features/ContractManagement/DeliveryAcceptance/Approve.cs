namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApproveDeliveryAcceptanceRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public class ApproveDeliveryAcceptanceEndpoint
    : EndpointBase<ApproveDeliveryAcceptanceRequest,
        Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveDeliveryAcceptanceEndpoint(
        Dp2DbContext dbContext,
        ILogger<ApproveDeliveryAcceptanceEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance")
             .WithName("ApproveDeliveryAcceptance")
             .Produces<Ok>()
             .Produces<string>(StatusCodes.Status404NotFound)
             .Produces<string>(StatusCodes.Status400BadRequest)
             .Accepts<ApproveDeliveryAcceptanceRequest>());
        this.Put("delivery-acceptance/{Id:guid}/approve");
    }

    protected override async ValueTask<
        Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ApproveDeliveryAcceptanceRequest req,
        CancellationToken ct)
    {
        var deliveryAcceptance =
            await this.dbContext.CmDeliveryAcceptances
                      .Include(da => da.Periods)
                      .ThenInclude(p => p.Acceptors)
                      .Include(cmDeliveryAcceptance => cmDeliveryAcceptance.Periods)
                      .ThenInclude(cmDeliveryAcceptancePeriod => cmDeliveryAcceptancePeriod.PaymentTerms)
                      .FirstOrDefaultAsync(
                          da => da.Id == CmDeliveryAcceptanceId.From(req.Id),
                          ct);

        if (deliveryAcceptance is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการส่งมอบและตรวจรับ");
        }

        if (deliveryAcceptance.Status == CmDeliveryAcceptanceStatus.Completed)
        {
            return TypedResults.BadRequest("การส่งมอบและตรวจรับได้รับการอนุมัติแล้ว");
        }

        var isAcceptanceCommitteeMember =
            deliveryAcceptance.Periods
                              .SelectMany(p => p.Acceptors)
                              .Any(a =>
                                  a.Type == AcceptorType.AcceptanceCommittee &&
                                  a.UserId == UserId.From(req.UserId) &&
                                  a.IsActive);

        if (!isAcceptanceCommitteeMember)
        {
            return TypedResults.BadRequest(
                "ไม่มีสิทธิ์อนุมัติ ผู้ใช้ไม่ได้เป็นคณะกรรมการตรวจรับพัสดุ");
        }

        var allPeriodsPaid =
            deliveryAcceptance.Periods
                              .All(p => p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.Paid);

        if (!allPeriodsPaid)
        {
            return TypedResults.BadRequest(
                "ไม่สามารถอนุมัติได้ เนื่องจากยังมีงวดที่ยังไม่ได้ชำระเงิน");
        }

        if (deliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
        {
            var contractDraftVendor = await this.dbContext.CaContractDraftVendors
                                                .Include(c => c.DraftTermsConditions)
                                                .Where(c => c.Id == ContractDraftVendorId.From((Guid)deliveryAcceptance.RefId))
                                                .FirstOrDefaultAsync(ct);

            if (contractDraftVendor is null)
            {
                return TypedResults.BadRequest("ไม่พบข้อมูลคู่ค้า");
            }

            var isGuaranteeSubmitted = contractDraftVendor.DraftTermsConditions.Guarantee.IsSubmitted ?? false;
            var warranty = contractDraftVendor.DraftTermsConditions.Warranty;

            if (isGuaranteeSubmitted && warranty is { HasWarranty: true, WarrantyPeriod: not null })
            {
                var lastPeriod = deliveryAcceptance
                                 .Periods
                                 .OrderByDescending(p => p.PaymentTerms.Max(pt => pt.PaymentTerm))
                                 .First();

                var lastAcceptanceDate = lastPeriod
                                         .Acceptors
                                         .Where(x => x.Type == AcceptorType.Approver && x.ActionAt.HasValue)
                                         .Max(x => x.ActionAt!.Value);

                var warrantyStartDate = lastAcceptanceDate.AddDays(1);

                var warrantyPeriod = warranty.WarrantyPeriod;
                var warrantyEndDate = warrantyStartDate
                                      .AddYears(warrantyPeriod.Year ?? 0)
                                      .AddMonths(warrantyPeriod.Month ?? 0)
                                      .AddDays((warrantyPeriod.Day ?? 0) - 1);

                warranty.WarrantyStartDate = warrantyStartDate;
                warranty.WarrantyEndDate = warrantyEndDate;
            }

            this.dbContext.CaContractDraftVendors.Update(contractDraftVendor);
        }
        else if (deliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit)
        {
            var vendorEdit = await this.dbContext.CaContractDraftVendorEdits
                .Where(ve => ve.Id == ContractDraftVendorEditId.From((Guid)deliveryAcceptance.RefId))
                .FirstOrDefaultAsync(ct);

            if (vendorEdit is null)
            {
                return TypedResults.BadRequest("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา");
            }

            var contractDraftVendorForEdit = await this.dbContext.CaContractDraftVendors
                .Include(c => c.DraftTermsConditions)
                .Where(c => c.Id == vendorEdit.ContractDraftVendorId)
                .FirstOrDefaultAsync(ct);

            if (contractDraftVendorForEdit is null)
            {
                return TypedResults.BadRequest("ไม่พบข้อมูลคู่ค้า");
            }

            var isGuaranteeSubmittedEdit = contractDraftVendorForEdit.DraftTermsConditions.Guarantee.IsSubmitted ?? false;
            var warrantyEdit = contractDraftVendorForEdit.DraftTermsConditions.Warranty;

            if (isGuaranteeSubmittedEdit && warrantyEdit is { HasWarranty: true, WarrantyPeriod: not null })
            {
                var lastPeriodEdit = deliveryAcceptance
                    .Periods
                    .OrderByDescending(p => p.PaymentTerms.Max(pt => pt.PaymentTerm))
                    .First();

                var lastAcceptanceDateEdit = lastPeriodEdit
                    .Acceptors
                    .Where(x => x.Type == AcceptorType.Approver && x.ActionAt.HasValue)
                    .Max(x => x.ActionAt!.Value);

                var warrantyStartDateEdit = lastAcceptanceDateEdit.AddDays(1);

                var warrantyPeriod = warrantyEdit.WarrantyPeriod;
                var warrantyEndDateEdit = warrantyStartDateEdit
                    .AddYears(warrantyPeriod.Year ?? 0)
                    .AddMonths(warrantyPeriod.Month ?? 0)
                    .AddDays((warrantyPeriod.Day ?? 0) - 1);

                warrantyEdit.WarrantyStartDate = warrantyStartDateEdit;
                warrantyEdit.WarrantyEndDate = warrantyEndDateEdit;
            }

            this.dbContext.CaContractDraftVendors.Update(contractDraftVendorForEdit);
        }

        deliveryAcceptance.ApproveDeliveryAcceptance();

        this.dbContext.CmDeliveryAcceptances.Update(deliveryAcceptance);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}