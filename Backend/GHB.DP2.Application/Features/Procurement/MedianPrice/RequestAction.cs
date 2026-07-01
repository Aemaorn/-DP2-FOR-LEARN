namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using System.Text.RegularExpressions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record MdpRequestActionRequest(
    Guid ProcurementId,
    Guid MdpId,
    string Reason,
    bool IsCancel = false);

public class MdpRequestActionEndPoint : MedianPriceEndpointBase<MdpRequestActionRequest, Results<Created<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public MdpRequestActionEndPoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<MdpRequestActionEndPoint> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(MedianPrice))
             .WithName("RequestActionMedianPrice")
             .AllowAnonymous()
             .Accepts<MdpRequestActionRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/median-price/{MdpId:guid}/request-action");
    }

    protected override async ValueTask<Results<Created<Guid>, NotFound<string>>> HandleRequestAsync(MdpRequestActionRequest req, CancellationToken ct)
    {
        var mdp = await this.dbContext.PpMedianPrices
                            .Include(ppMedianPrice => ppMedianPrice.Procurement)
                            .Include(p => p.DocumentTemplate)
                            .Include(p => p.Acceptors)
                            .SingleOrDefaultAsync(
                                m => m.Id == MedianPriceId.From(req.MdpId) &&
                                     m.IsActive &&
                                     m.ProcurementId == ProcurementId.From(req.ProcurementId),
                                ct);

        if (mdp is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลราคากลาง");
        }

        if (mdp.Status is not MedianPriceStatus.Approved)
        {
            this.ThrowError("ราคากลางไม่สามารถทำการขอยกเลิก/ขอแก้ไข ณ สถาะนี้ได้", StatusCodes.Status400BadRequest);
        }

        var newMdp = mdp.Clone(req.IsCancel);

        var match = Regex.Match(
            mdp.DocumentTemplate.Code,
            @"BoKor(\d{2})",
            RegexOptions.None,
            TimeSpan.FromMilliseconds(500));

        if (!match.Success)
        {
            this.ThrowError("ไม่พบเอกสารราคากลางที่เข้ากับเงื่อนไข", StatusCodes.Status400BadRequest);
        }

        var newDoc = await this.dbContext.SuDocumentTemplates
                               .Where(w => EF.Functions.ILike(w.Code, $"%{match.Groups[1].Value}%") &&
                                           w.IsActive && w.SupplyMethodCode == mdp.Procurement.SupplyMethodCode && w.Group == "Mdp")
                               .WhereIfTrue(
                                   mdp.Procurement.HasMd,
                                   x =>
                                       EF.Functions.JsonExists(x.AdditionalInfo!, nameof(SuDocumentTemplate.IsJorPorComment)) &&
                                       x.AdditionalInfo!.RootElement
                                        .GetProperty(nameof(SuDocumentTemplate.IsJorPorComment))
                                        .GetBoolean() == true)
                               .WhereIfTrue(
                                   !mdp.Procurement.HasMd,
                                   x => x.AdditionalInfo == null)
                               .WhereIfTrue(
                                   req.IsCancel,
                                   x => x.IsCancel == true)
                               .WhereIfTrue(
                                   !req.IsCancel,
                                   x => x.IsChange == true)
                               .SingleOrDefaultAsync(ct);

        if (newDoc is null)
        {
            this.ThrowError("ไม่พบเอกสารราคากลางที่เข้ากับเงื่อนไข", StatusCodes.Status400BadRequest);
        }

        newMdp.SetDocumentTemplate(newDoc.Id);

        _ = req.IsCancel ? newMdp.SetCancelReason(req.Reason) : newMdp.SetChangeReason(req.Reason);

        await this.SetDefaultDocumentTemplate(
            newMdp,
            newDoc.Code,
            ct);

        mdp.Procurement.SetProcessType(ProcessType.MedianPrice);

        this.dbContext.PpMedianPrices.Update(mdp);
        this.dbContext.PpMedianPrices.Add(newMdp);
        await this.dbContext.SaveChangesAsync(ct);

        var committeeMembers = mdp.Acceptors
            .Where(a => a.Type == AcceptorType.MedianPriceCommittee && a.IsActive && !a.IsUnableToPerformDuties)
            .Select(a => a.UserId)
            .ToList();

        foreach (var userId in committeeMembers)
        {
            _ = req.IsCancel
                ? SendNotificationRequestCancelAsync(newMdp, mdp.Procurement.Id.Value, userId, ct)
                : SendNotificationRequestChangeAsync(newMdp, mdp.Procurement.Id.Value, userId, ct);
        }

        return TypedResults.Created(string.Empty, newMdp.Id.Value);
    }

    private static async Task SendNotificationRequestCancelAsync(PpMedianPrice mdp, Guid procurementId, UserId userId, CancellationToken ct)
    {
        await Notification
              .Crate(
                  userId,
                  NotificationConstant.RequestCancelPlan.Title,
                  string.Format(NotificationConstant.RequestCancelPlan.Message, ProgramConstant.PreProcurementMedianPrice.Name, mdp.ReferenceNumber),
                  NotificationProgram.Procurement)
              .SetReferenceId(mdp.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PreProcurementMedianPrice.Url, procurementId), ProgramConstant.PreProcurementMedianPrice.Button)
              .PublishAsync(ct);
    }

    private static async Task SendNotificationRequestChangeAsync(PpMedianPrice mdp, Guid procurementId, UserId userId, CancellationToken ct)
    {
        await Notification
              .Crate(
                  userId,
                  NotificationConstant.RequestChangePlan.Title,
                  string.Format(NotificationConstant.RequestChangePlan.Message, ProgramConstant.PreProcurementMedianPrice.Name, mdp.ReferenceNumber),
                  NotificationProgram.Procurement)
              .SetReferenceId(mdp.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PreProcurementMedianPrice.Url, procurementId), ProgramConstant.PreProcurementMedianPrice.Button)
              .PublishAsync(ct);
    }
}