namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using System;
using System.Threading;
using System.Threading.Tasks;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RequestEditTorDraftRequest(
    Guid ProcurementId,
    Guid TorDraftId,
    bool IsChange,
    bool IsCancel,
    string Reason
);

public class RequestActionTorDraftEndpoint : TorDraftEndpointBase<RequestEditTorDraftRequest, Results<Created<Guid>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RequestActionTorDraftEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<RequestActionTorDraftEndpoint> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/TorDraft")
             .WithName("RequestActionTorDraft")
             .AllowAnonymous()
             .Accepts<RequestEditTorDraftRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/tordraft/{TorDraftId:guid}/request-action");
    }

    protected override async ValueTask<Results<Created<Guid>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RequestEditTorDraftRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PpTorDrafts
                               .Include(x => x.PpTorDraftObjects)
                               .Include(x => x.PpTorDraftQualifications)
                               .Include(x => x.PpTorDraftTechnicalSpecifications)
                               .Include(x => x.PpTorDraftTechnicalPeriods).ThenInclude(ppTorDraftTechnicalPeriod => ppTorDraftTechnicalPeriod.PpTorDraftTechnicalPeriodDetails)
                               .Include(x => x.PpTorDraftBudgets)
                               .ThenInclude(b => b.PpTorDraftBudgetDetails)
                               .Include(x => x.PpTorDraftPaymentTerms)
                               .ThenInclude(pt => pt.PpTorDraftPaymentTermDetails)
                               .Include(x => x.PpTorDraftWarranties)
                               .Include(x => x.PpTorDraftFineRates)
                               .Include(x => x.PpTorDraftAcceptors)
                               .Include(ppTorDraft => ppTorDraft.DocumentTemplate)
                               .Include(ppTorDraft => ppTorDraft.Procurement)
                               .AsSingleQuery()
                               .SingleOrDefaultAsync(x => x.Id == PpTorDraftId.From(req.TorDraftId) && x.IsActive && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        if (entity.Status != TorDraftStatus.Approved)
        {
            return TypedResults.BadRequest("สถานะร่างขอบเขตงานไม่สามารถขอแก้ไขได้");
        }

        var torDraft = entity.Clone(req.IsChange, req.IsCancel);

        if (req.IsCancel)
        {
            torDraft.SetCancelReason(req.Reason);
        }

        if (req.IsChange)
        {
            torDraft.SetChangeReason(req.Reason);
        }

        entity.Procurement.SetProcessType(ProcessType.TorDraft);

        entity.IsActive = false;

        await this.SetDefaultDocumentTemplate(
            torDraft,
            entity.DocumentTemplate?.Code,
            entity.Procurement.SupplyMethodCode,
            entity.Procurement.HasMd,
            ct);

        this.dbContext.PpTorDrafts.Add(torDraft);
        this.dbContext.PpTorDrafts.Update(entity);

        await this.dbContext.SaveChangesAsync(ct);

        var committeeMembers = entity.PpTorDraftAcceptors
            .Where(a => a.Type == AcceptorType.TorDraftCommittee && a.IsActive && !a.IsUnableToPerformDuties)
            .Select(a => a.UserId)
            .ToList();

        foreach (var userId in committeeMembers)
        {
            _ = req.IsChange
                ? SendNotificationRequestChangeAsync(torDraft, entity.Procurement.Id.Value, userId, ct)
                : SendNotificationRequestCancelAsync(torDraft, entity.Procurement.Id.Value, userId, ct);
        }

        return TypedResults.Created(string.Empty, torDraft.Id.Value);
    }

    private static async Task SendNotificationRequestCancelAsync(PpTorDraft tor, Guid procurementId, UserId user, CancellationToken ct)
    {
        await Notification
              .Crate(
                  user,
                  NotificationConstant.RequestCancelPlan.Title,
                  string.Format(NotificationConstant.RequestCancelPlan.Message, ProgramConstant.PreProcurementTorDraft.Name, tor.ReferenceNumber.Value),
                  NotificationProgram.Procurement)
              .SetReferenceId(tor.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PreProcurementTorDraft.Url, procurementId), ProgramConstant.PreProcurementTorDraft.Button)
              .PublishAsync(ct);
    }

    private static async Task SendNotificationRequestChangeAsync(PpTorDraft tor, Guid procurementId, UserId user, CancellationToken ct)
    {
        await Notification
              .Crate(
                  user,
                  NotificationConstant.RequestChangePlan.Title,
                  string.Format(NotificationConstant.RequestChangePlan.Message, ProgramConstant.PreProcurementTorDraft.Name, tor.ReferenceNumber.Value),
                  NotificationProgram.Procurement)
              .SetReferenceId(tor.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PreProcurementTorDraft.Url, procurementId), ProgramConstant.PreProcurementTorDraft.Button)
              .PublishAsync(ct);
    }
}