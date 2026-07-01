namespace GHB.DP2.Application.Features.Procurement.Jp006.Dto;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;

public record CreateJp006Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    PurchaseOrderStatus Status,
    IEnumerable<CreateJp006Entrepreneur> Entrepreneurs,
    IEnumerable<Jp006AcceptorInfo>? Acceptors,
    IEnumerable<Jp006AssigneeInfo>? Assignees,
    DateTimeOffset? DocumentDate = null)
{
    public PPurchaseOrder MapToEntity(Domain.Procurement.Procurement procurement)
    {
        var jp006 = PPurchaseOrder.Create(procurement);

        foreach (var entrepreneur in this.Entrepreneurs)
        {
            jp006.AddEntrepreneur(entrepreneur.MapToEntity(jp006.Id));
        }

        return jp006;
    }

    public class Validator : Validator<CreateJp006Request>
    {
        public Validator()
        {
            this.RuleFor(x => x.ProcurementId)
                .NotNull()
                .WithMessage("ต้องมีข้อมูลจัดซื้อจัดจ้างเบื้องต้น");

            this.RuleFor(x => x.Entrepreneurs)
                .NotEmpty()
                .WithMessage("ต้องมีข้อมูลรายการผู้ประกอบการเสนอราคาอย่างน้อย 1 รายการ");

            this.RuleForEach(x => x.Entrepreneurs)
                .SetValidator(new CreateJp006Entrepreneur.Validator());
        }
    }
}

public record CreateJp006Entrepreneur(
    Guid VendorId,
    bool EmailSended,
    int Sequence,
    EntrepreneurCheckConditions Coi,
    EntrepreneurCheckConditions Watchlist,
    EntrepreneurCheckConditions Egp,
    bool IsWinner,
    string? SelectionReasonCode,
    string? Remark,
    IEnumerable<CreateJp006PriceDetails> PriceDetails,
    IEnumerable<ShareholderDto>? Shareholder,
    EntrepreneurResponseAttachment[]? Attachments,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult)
{
    public PPurchaseOrderEntrepreneur MapToEntity(Domain.Procurement.PPurchaseOrder.PurchaseOrderId purchaseOrderId)
    {
        var entrepreneur = PPurchaseOrderEntrepreneur.Create(
            purchaseOrderId,
            SuVendorId.From(this.VendorId));

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

        foreach (var priceDetail in this.PriceDetails)
        {
            entrepreneur.AddPriceDetails(priceDetail.MapToEntity(entrepreneur.Id));
        }

        if (this.Shareholder != null && this.Shareholder.Any())
        {
            var shareholders = this.Shareholder.SelectMany(s =>
            {
                var checkTypes = s.CheckType != null
                    ? new[] { s.CheckType }
                    : new[] { "COI", "Watchlist" };

                return checkTypes.Select(checkType =>
                {
                    var newShareholder = PPurchaseOrderEntrepreneurShareholders.Create(
                                                                                    s.Sequence,
                                                                                    s.TaxId,
                                                                                    s.FirstName,
                                                                                    s.LastName,
                                                                                    s.IsDirector,
                                                                                    s.IsShareholder,
                                                                                    s.IsJuristic)
                                                                                .SetCheckType(checkType)
                                                                                .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                                                                                .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                                                                                .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

                    if (s.CoiCheckerResult is not null)
                    {
                        newShareholder.AddChecker(
                            QualificationType.COI,
                            s.CoiCheckerResult.Result,
                            s.CoiCheckerResult.ResultAt,
                            s.CoiCheckerResult.Remark);
                    }

                    if (s.WatchlistCheckerResult is not null)
                    {
                        newShareholder.AddChecker(
                            QualificationType.Watchlist,
                            s.WatchlistCheckerResult.Result,
                            s.WatchlistCheckerResult.ResultAt,
                            s.WatchlistCheckerResult.Remark);
                    }

                    return newShareholder;
                });
            }).ToList();

            entrepreneur.AddPurchaseOrderEntrepreneurShareholderList(shareholders);
        }

        return entrepreneur;
    }

    public class Validator : Validator<CreateJp006Entrepreneur>
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

            this.RuleForEach(x => x.Attachments)
                .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                    .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()))
                .When(x => x.Attachments is not null);
        }
    }
}

public record CreateJp006PriceDetails(
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
        var priceDetails = PPurchaseOrderPriceDetails.Create(purchaseOrderEntrepreneurId);
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

    public class Validator : Validator<CreateJp006PriceDetails>
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

public record ShareholderDto(
    int Sequence,
    string TaxId,
    string FirstName,
    string LastName,
    bool IsDirector,
    bool? IsShareholder,
    bool? IsJuristic,
    bool? WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool? CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool? EgpResult,
    string? EgpRemark,
    DateTimeOffset? EgpResultAt,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    string? CheckType = null
);

public record EntrepreneurCheckConditions(
    [property: Description("ผลการตรวจสอบ")]
    bool? Result,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("วันที่ตรวจสอบ")]
    DateTimeOffset? Date);