namespace GHB.DP2.Application.Features.Procurement.Invite;

using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetEmailTemplateDefault(
    Guid ProcurementId,
    Guid InviteId,
    Guid EntrepreneursId);

public class GetEmailTemplateDefaultEndPoint : InviteEndpointBase<SendEmailRequest, Results<Ok<string>, BadRequest<string>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetEmailTemplateDefaultEndPoint(
        Dp2DbContext dbContext,
        ILogger<SendEmailEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/entrepreneurs/{EntrepreneursId:guid}/default-email-template");
        this.Options(builder =>
            builder.WithTags("Procurement/Invite")
                   .WithName("DefaultEmailTemplate")
                   .Produces<string>(StatusCodes.Status200OK)
                   .Produces<string>(StatusCodes.Status400BadRequest)
                   .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<string>, BadRequest<string>, NotFound<string>>> HandleRequestAsync(SendEmailRequest req, CancellationToken ct)
    {
        var inviteQuery =
            this.dbContext.PInvites
                .Where(i =>
                    i.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                    i.Id == PInviteId.From(req.InviteId))
                .AsQueryable();

        var invite =
            await inviteQuery
                  .Include(i => i.Procurement)
                  .SingleOrDefaultAsync(ct);

        if (invite is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลหนังสือเชิญชวน");
        }

        if (invite.Status is not PInviteStatus.Approved)
        {
            return TypedResults.BadRequest($"ไม่สามารถส่งอีเมลได้ เนื่องจากสถานะหนังสือเชิญชวนไม่ถูกต้อง");
        }

        var entrepreneurs =
            await inviteQuery
                  .Include(i => i.InvitedEntrepreneurs)
                  .ThenInclude(e => e.Vendor)
                  .SelectMany(i => i.InvitedEntrepreneurs)
                  .SingleOrDefaultAsync(
                      i =>
                          i.Id == PInvitedEntrepreneursId.From(req.EntrepreneursId),
                      ct);

        if (entrepreneurs is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการที่ได้รับเชิญ");
        }

        var vendor = entrepreneurs.Vendor;
        var procurement = invite.Procurement;
        var purchaseRequisition =
            await this.dbContext.PpPurchaseRequisitions
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.TechnicalSpecifications)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.TorDraft)
                      .ThenInclude(ppTorDraft => ppTorDraft!.PpTorDraftQualifications)
                      .FirstOrDefaultAsync(
                          p =>
                              p.ProcurementId == procurement.Id,
                          ct);

        if (purchaseRequisition is null)
        {
            return TypedResults.BadRequest($"ไม่พบข้อมูลใบขอซื้อ/ขอจ้าง");
        }

        var qualifications =
            purchaseRequisition.TorDraft is null
                ? []
                : purchaseRequisition.TorDraft
                                     .PpTorDraftQualifications
                                     .OrderBy(q => q.Sequence)
                                     .Select(q => $"4.{q.Sequence} {q.Description}")
                                     .ToArray();
        var parcelDescription
            = purchaseRequisition.TechnicalSpecifications
                                 .Select(ts => $"1.{ts.Sequence} {ts.Description}")
                                 .ToArray();

        var parcelDescriptionHtml = string.Join(string.Empty, parcelDescription.Select(p => $"<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{p}</p>"));
        var qualificationsHtml = string.Join(string.Empty, qualifications.Select(q => $"<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{q}</p>"));

        var template = $@"<p>เรียน&nbsp;{vendor.EstablishmentName}</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ด้วย&nbsp;ธนาคารอาคารสงเคราะห์&nbsp;มีความประสงค์จะ&nbsp;{procurement.SupplyMethodType?.Label ?? "-"}&nbsp;{procurement.Name}&nbsp;โดยวิธี&nbsp;{procurement.SupplyMethodSpecialType?.Label ?? "-"}&nbsp;จึงขอเชิญเสนอราคา</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1.&nbsp;รายการพัสดุที่ต้องการซื้อและจ้าง&nbsp;</p>
{parcelDescriptionHtml}
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2.&nbsp;วงเงินงบประมาณ&nbsp;{procurement.Budget.ToCurrencyStringWithComma() ?? "-"}&nbsp;บาท&nbsp;{procurement.Budget.ThaiBahtText() ?? "-"}</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;3.&nbsp;ราคากลาง&nbsp;{purchaseRequisition.MedianPriceAmount.ToCurrencyStringWithComma() ?? "-"}&nbsp;{purchaseRequisition.MedianPriceAmount.ThaiBahtText() ?? "-"}</p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;4.&nbsp;คุณสมบัติของผู้เสนอราคา</p>
{qualificationsHtml}
<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;5.&nbsp;เกณฑ์การพิจารณาผลการยื่นข้อเสนอครั้งนี้&nbsp;จะพิจารณาตัดสินโดยใช้หลักเกณฑ์ราคา</p>
<p></p>
<p>ขอแสดงความนับถือ&nbsp;ธนาคารอาคารสงเคราะห์</p>";

        return TypedResults.Ok(template);
    }
}