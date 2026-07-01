namespace GHB.DP2.Application.Features.Procurement.Jp006.Dto;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;

public record UpdateJp006Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Domain.Procurement.PPurchaseOrder.PurchaseOrderId Jp006Id,
    ProcurementId ProcurementId,
    PurchaseOrderStatus Status,
    Guid? Jp006DocumentId,
    bool? IsJp006DocumentIdReplaced,
    Guid? WinnerDocumentId,
    bool? IsWinnerDocumentIdReplaced,
    IEnumerable<UpdateJp006Entrepreneur> Entrepreneurs,
    IEnumerable<Jp006AcceptorInfo>? Acceptors,
    IEnumerable<Jp006AssigneeInfo>? Assignees,
    DateTimeOffset? DocumentDate = null,
    DateTimeOffset? LastModifiedAt = null)
{
    public class Validator : Validator<UpdateJp006Request>
    {
        public Validator()
        {
            this.RuleFor(x => x.Jp006Id)
                .NotNull()
                .WithMessage("ต้องมีรหัสรายการจัดซื้อจัดจ้าง");

            this.RuleFor(x => x.ProcurementId)
                .NotNull()
                .WithMessage("ต้องมีข้อมูลจัดซื้อจัดจ้างเบื้องต้น");

            this.RuleFor(x => x.Entrepreneurs)
                .NotEmpty()
                .WithMessage("ต้องมีข้อมูลรายการผู้ประกอบการเสนอราคาอย่างน้อย 1 รายการ");
        }
    }
}

public record UpdateJp006Entrepreneur(
    Domain.Procurement.PPurchaseOrder.PurchaseOrderEntrepreneurId? EntrepreneurId,
    SuVendorId VendorId,
    bool EmailSended,
    int Sequence,
    EntrepreneurCheckConditions Coi,
    EntrepreneurCheckConditions Watchlist,
    EntrepreneurCheckConditions Egp,
    bool IsWinner,
    string? SelectionReasonCode,
    string? Remark,
    IEnumerable<UpdateJp006PriceDetails> PriceDetails,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    UpdateShareholderDto[]? Shareholder)
{
    public PPurchaseOrderEntrepreneur MapToEntity(Domain.Procurement.PPurchaseOrder.PurchaseOrderId purchaseOrderId)
    {
        var entrepreneur = this.EntrepreneurId.HasValue
            ? PPurchaseOrderEntrepreneur.CreateWithId(
                this.EntrepreneurId.Value,
                purchaseOrderId,
                this.VendorId)
            : PPurchaseOrderEntrepreneur.Create(
                purchaseOrderId,
                this.VendorId);

        entrepreneur.SetEmailSended(this.EmailSended)
                    .SetSequence(this.Sequence)
                    .SetCoiResult(this.Coi.Result, this.Coi.Remark, this.Coi.Date)
                    .SetWatchlistResult(this.Watchlist.Result, this.Watchlist.Remark, this.Watchlist.Date)
                    .SetEgpResult(this.Egp.Result, this.Egp.Remark, this.Egp.Date)
                    .SetIsWinner(this.IsWinner)
                    .SetSelectionReasonCode(this.SelectionReasonCode)
                    .SetRemark(this.Remark);

        if (this.CoiCheckerResult is not null)
        {
            entrepreneur.AddChecker(
                QualificationType.COI,
                this.CoiCheckerResult.Result,
                this.CoiCheckerResult.ResultAt,
                this.CoiCheckerResult.Remark);
        }

        if (this.WatchlistCheckerResult is not null)
        {
            entrepreneur.AddChecker(
                QualificationType.Watchlist,
                this.WatchlistCheckerResult.Result,
                this.WatchlistCheckerResult.ResultAt,
                this.WatchlistCheckerResult.Remark);
        }

        return entrepreneur;
    }

    public class Validator : Validator<UpdateJp006Entrepreneur>
    {
        public Validator()
        {
            this.RuleFor(x => x.VendorId)
                .NotNull()
                .WithMessage("ต้องมีรหัสผู้ประกอบการ");

            this.RuleFor(x => x.EmailSended)
                .NotNull()
                .WithMessage("ต้องระบุว่ามีการส่งอีเมลหรือไม่");

            this.RuleFor(x => x.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับผู้ประกอบการต้องมากกว่า 0");

            this.RuleFor(x => x.Coi)
                .NotNull()
                .WithMessage("ต้องมีข้อมูลการตรวจสอบ Coi");

            this.RuleFor(x => x.Watchlist)
                .NotNull()
                .WithMessage("ต้องมีข้อมูลการตรวจสอบ Watchlist");

            this.RuleFor(x => x.Egp)
                .NotNull()
                .WithMessage("ต้องมีข้อมูลการตรวจสอบ Egp");

            this.RuleFor(x => x.IsWinner)
                .NotNull()
                .WithMessage("ต้องระบุว่าผู้ประกอบการเป็นผู้ชนะหรือไม่");

            this.RuleFor(x => x.SelectionReasonCode)
                .MaximumLength(50)
                .WithMessage("รหัสเหตุผลการเลือกต้องไม่เกิน 50 ตัวอักษร");
        }
    }
}

public record UpdateJp006PriceDetails(
    Domain.Procurement.PPurchaseOrder.PPurchaseOrderPriceDetailsId? PriceDetailsId,
    int Sequence,
    string ParcelName,
    int ParcelQuantity,
    string ParcelUnitCode,
    string? VatTypeCode,
    decimal OfferedPrice,
    decimal AgreedPrice,
    string Description)
{
    public PPurchaseOrderPriceDetails MapToEntity(Domain.Procurement.PPurchaseOrder.PurchaseOrderEntrepreneurId purchaseOrderEntrepreneurId)
    {
        var priceDetails = this.PriceDetailsId.HasValue
            ? PPurchaseOrderPriceDetails.Create(this.PriceDetailsId.Value, purchaseOrderEntrepreneurId)
            : PPurchaseOrderPriceDetails.Create(purchaseOrderEntrepreneurId);

        var detailsInfo = new PPurchaseOrderPriceDetails.PriceDetailsInfo(
            this.Sequence,
            this.ParcelName,
            this.ParcelQuantity,
            this.ParcelUnitCode,
            this.VatTypeCode,
            this.OfferedPrice,
            this.AgreedPrice,
            this.Description);
        priceDetails.SetDetails(detailsInfo);

        return priceDetails;
    }

    public class Validator : Validator<UpdateJp006PriceDetails>
    {
        public Validator()
        {
            this.RuleFor(x => x.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับรายการราคาต้องมากกว่า 0");

            this.RuleFor(x => x.ParcelName)
                .NotEmpty()
                .WithMessage("ต้องระบุชื่อพัสดุ");

            this.RuleFor(x => x.ParcelQuantity)
                .GreaterThan(0)
                .WithMessage("จำนวนพัสดุต้องมากกว่า 0");

            this.RuleFor(x => x.ParcelUnitCode)
                .NotEmpty()
                .WithMessage("ต้องระบุรหัสหน่วยนับพัสดุ");

            this.RuleFor(x => x.OfferedPrice)
                .GreaterThan(0)
                .WithMessage("ราคาที่เสนอจะต้องมากกว่า 0");

            this.RuleFor(x => x.AgreedPrice)
                .GreaterThan(0)
                .WithMessage("ราคาที่ตกลงจะต้องมากกว่า 0");
        }
    }
}