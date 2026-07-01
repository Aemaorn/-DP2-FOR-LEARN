namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateReportAuditAndRevenueRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    Guid Id,
    RpAuditAndRevenueStatus Status,
    DateTimeOffset DocumentDate,
    DateTimeOffset SignStartDate,
    DateTimeOffset SignEndDate,
    DateTimeOffset DeliveryDate,
    IEnumerable<AuditAndRevenueDetailDto>? Details,
    IEnumerable<AcceptorRequest>? ApprovalAcceptors,
    bool? IsAuditReportDocumentIdReplaced,
    bool? IsAuditGeneralReportDocumentIdReplaced,
    bool? IsRevenueReportDocumentIdReplaced);

public class UpdateReportAuditAndRevenueEndpoint : AuditAndRevenueEndpoint<UpdateReportAuditAndRevenueRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateReportAuditAndRevenueEndpoint(ILogger<UpdateReportAuditAndRevenueEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient, IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("report/audit-revenue/{id:guid}");
        this.Description(b => b
                              .WithTags("Report/AuditAndRevenue")
                              .WithName("UpdateReportAuditAndRevenue")
                              .WithSummary("แก้ไขรายงานการตรวจสอบและรายได้")
                              .AllowAnonymous()
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdateReportAuditAndRevenueRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpAuditAndRevenues
            .Include(x => x.Details)
            .ThenInclude(d => d.CaContractDraftVendor)
            .ThenInclude(v => v.Vendor)
            .ThenInclude(v => v.VendorInfo)
            .Include(x => x.Details)
            .ThenInclude(d => d.CaContractDraftVendor)
            .ThenInclude(v => v.ContractType)
            .Include(x => x.Acceptors)
            .ThenInclude(a => a.User)
            .ThenInclude(u => u.Employee)
            .Include(x => x.AuditInfo)
            .Include(x => x.DocumentHistories)
            .FirstOrDefaultAsync(x => x.Id == RpAuditAndRevenueId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        entity.SetValues(
            entity.DocumentNumber,
            req.DocumentDate,
            req.SignStartDate,
            req.SignEndDate,
            req.DeliveryDate);

        // Update Acceptors
        if (req.ApprovalAcceptors != null)
        {
            this.UpsertAcceptors(entity, req.ApprovalAcceptors, req.Status, UserId.From(req.UserId));
        }

        // Update Details
        if (req.Details != null)
        {
            this.UpsertDetails(entity, req.Details);
        }

        var originalStatus = entity.Status;

        if (originalStatus == req.Status)
        {
            entity.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    string.Empty,
                    RpAuditAndRevenueStatus.Approved.ToString()));
        }

        entity.SetStatus(req.Status);

        var documentTypes = new[]
        {
            (Type: RpAuditAndRevenueDocumentType.AuditReport, IsReplaced: req.IsAuditReportDocumentIdReplaced ?? false),
            (Type: RpAuditAndRevenueDocumentType.AuditGeneralReport, IsReplaced: req.IsAuditGeneralReportDocumentIdReplaced ?? false),
            (Type: RpAuditAndRevenueDocumentType.RevenueReport, IsReplaced: req.IsRevenueReportDocumentIdReplaced ?? false),
        };

        foreach (var doc in documentTypes)
        {
            await this.ManageDocumentForSaveAsync(entity, doc.Type, originalStatus, req.Status, doc.IsReplaced, UserId.From(req.UserId), ct);
        }

        SendNotificationWhenWaitingApproval(entity, originalStatus, req.Status);

        this.dbContext.RpAuditAndRevenues.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void SendNotificationWhenWaitingApproval(RpAuditAndRevenue entity, RpAuditAndRevenueStatus previousStatus, RpAuditAndRevenueStatus newStatus)
    {
        if (previousStatus == RpAuditAndRevenueStatus.WaitingApproval || newStatus != RpAuditAndRevenueStatus.WaitingApproval)
        {
            return;
        }

        var approvers = entity.Acceptors
                              .Where(p => p.Type == AcceptorType.Approver)
                              .OrderBy(a => a.Sequence)
                              .ToList();

        var firstPending = approvers.Select(DelegatorExtensions.DelegatorToAcceptor)
                                    .FirstOrDefault(a => a is { Status: AcceptorStatus.Pending });

        if (firstPending is null)
        {
            return;
        }

        var isLastPending = approvers.Count(a => a.Status == AcceptorStatus.Pending) == 1;

        foreach (var targetUserId in firstPending.GetNotificationTargets())
        {
            if (!isLastPending)
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.AuditAndRevenue.Name, entity.DocumentNumber));
            }
            else
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.AuditAndRevenue.Name, entity.DocumentNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(RpAuditAndRevenue entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Report)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.AuditAndRevenue.Url, entity.Id.Value), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}