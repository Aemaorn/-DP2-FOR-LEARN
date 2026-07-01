namespace GHB.DP2.Application.Features.Procurement.Appoint;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Appoint.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApproveAppointRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid AppointId,
    Guid AcceptorId,
    AcceptorType AcceptorType,
    string? Remark);

public class ApproveAppointEndpoint : AppointEndpointBase<ApproveAppointRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveAppointEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<ApproveAppointEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Post("appointments/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveAppointRequest req, CancellationToken ct)
    {
        var (procurement, appoint) = await this.ValidateRequestAsync(PpAppointId.From(req.AppointId), ct);

        var validationResult = ValidateAcceptor(appoint, req.AcceptorId);

        if (validationResult != null)
        {
            return validationResult;
        }

        var acceptors = GetOrderedActiveAcceptors(appoint);
        var currentAcceptor =
            acceptors.FirstOrDefault(a => a.Id == AcceptorId.From(req.AcceptorId));

        if (currentAcceptor == null)
        {
            return TypedResults.NotFound("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        if (!IsPreviousApproved(acceptors, currentAcceptor))
        {
            return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
        }

        var currentAcceptorUser =
            appoint.Acceptors
                   .First(a => a.Id == currentAcceptor.Id);

        currentAcceptorUser
            .SetDelegatee(currentAcceptor.DelegateeId)
            .Approve(remark: req.Remark);

        currentAcceptor.SetCurrent(false);

        await this.HandlePostApprovalAsync(appoint, [.. appoint.Acceptors], currentAcceptor, req.Remark);
        await this.UpdateApprovedStatusAsync(appoint, procurement, [.. appoint.Acceptors], req.Remark);

        this.dbContext.Procurements.Update(procurement);
        this.dbContext.PpAppoints.Update(appoint);

        await this.ReplaceDocumentAsync(appoint, ct);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static Results<Ok, NotFound<string>, BadRequest<string>>? ValidateAcceptor(
        PpAppoint appoint,
        Guid acceptorId)
    {
        if (!appoint.Acceptors
                    .Where(w => w.IsActive)
                    .Any(x => x.Id.Value == acceptorId && x is { Status: AcceptorStatus.Pending, Type: AcceptorType.Approver }))
        {
            return TypedResults.BadRequest("อนุมัติได้เฉพาะผู้มีอำนาจเห็นชอบ/อนุมัติเท่านั้น");
        }

        return null;
    }

    private static List<PpAppointAcceptors> GetOrderedActiveAcceptors(PpAppoint appoint)
    {
        return
        [
            .. appoint.Acceptors
                      .Where(a => a.IsActive)
                      .Map(DelegatorExtensions.DelegatorToAcceptor)
                      .OrderBy(a => a.Sequence)
        ];
    }

    private async Task HandlePostApprovalAsync(
        PpAppoint appoint,
        List<PpAppointAcceptors> acceptors,
        PpAppointAcceptors currentAcceptor,
        string? remark)
    {
        var allApproved = acceptors.All(a => a.Status == AcceptorStatus.Approved);

        if (!allApproved)
        {
            await this.HandleNotAllApprovedAsync(appoint, acceptors, currentAcceptor, remark);
        }
        else
        {
            ClearAllCurrentFlags(acceptors);
        }
    }

    private async Task HandleNotAllApprovedAsync(
        PpAppoint appoint,
        List<PpAppointAcceptors> acceptors,
        PpAppointAcceptors currentAcceptor,
        string? remark)
    {
        var next = acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                            .FirstOrDefault(a => a.Sequence > currentAcceptor.Sequence && a.Status is AcceptorStatus.Pending);

        if (next != null)
        {
            await this.ProcessNextApproverAsync(appoint, acceptors, next);
        }
        else
        {
            await SendNotificationAsync(
                appoint,
                UserId.From(appoint.AuditInfo.CreatedBy),
                NotificationConstant.InformCommittee.Title,
                string.Format(NotificationConstant.InformCommittee.Message, ProgramConstant.PreProcurementAppointment.Name, appoint.AppointNumber));
        }

        appoint.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                string.Empty,
                appoint.Status.ToString(),
                remark));
    }

    private async Task ProcessNextApproverAsync(
        PpAppoint appoint,
        List<PpAppointAcceptors> acceptors,
        PpAppointAcceptors next)
    {
        var pendingCount = acceptors.Count(a => a.Status == AcceptorStatus.Pending);
        var isLastPending = pendingCount == 1;

        if (appoint.Status == AppointStatus.WaitingApproval && next.Status == AcceptorStatus.Pending)
        {
            next.SetCurrent(true);
        }

        if (next.Type == AcceptorType.Approver)
        {
            await SendNextApproverNotificationAsync(appoint, next, isLastPending);
        }
    }

    private static async Task SendNextApproverNotificationAsync(
        PpAppoint appoint,
        PpAppointAcceptors next,
        bool isLastPending)
    {
        var (title, message) = isLastPending
            ? (NotificationConstant.WaitForApprove.Title,
                string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.PreProcurementAppointment.Name, appoint.AppointNumber))
            : (NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementAppointment.Name, appoint.AppointNumber));

        foreach (var targetUserId in next.GetNotificationTargets())
        {
            await SendNotificationAsync(appoint, targetUserId, title, message);
        }
    }

    private static void ClearAllCurrentFlags(List<PpAppointAcceptors> acceptors)
    {
        foreach (var acceptor in acceptors)
        {
            acceptor.SetCurrent(false);
        }
    }

    private async Task UpdateApprovedStatusAsync(
        PpAppoint appoint, Domain.Procurement.Procurement procurement, List<PpAppointAcceptors> acceptorsWithOrder, string? remark)
    {
        var isAllApproved = appoint.Status == AppointStatus.WaitingApproval
                            && acceptorsWithOrder.All(a => a.Status == AcceptorStatus.Approved);

        if (!isAllApproved)
        {
            return;
        }

        if (!appoint.IsCancel)
        {
            await this.HandleFullApprovalAsync(appoint, procurement, remark);
        }
        else
        {
            appoint.SetCancelled();
        }
    }

    private async Task HandleFullApprovalAsync(PpAppoint appoint, Domain.Procurement.Procurement procurement, string? remark)
    {
        appoint.SetApproved(remark);
        await SendNotificationCommitteeAsync(appoint, CancellationToken.None);
        procurement.SetStatus(ProcurementStatus.InProgress);
        procurement.SetProcessType(ProcessType.TorDraft);
        await this.OverrideCommitteeTor(appoint, procurement, CancellationToken.None);

        // await this.OverrideCommitteeMedianPrice(appoint, procurement, CancellationToken.None);
    }

    private async Task OverrideCommitteeTor(PpAppoint appoint, Domain.Procurement.Procurement procurement, CancellationToken ct)
    {
        var torData = await this.dbContext
                                .PpTorDrafts
                                .Include(a => a.PpTorDraftAcceptors)
                                .Where(c => c.IsActive && (c.Status == TorDraftStatus.Draft || c.Status == TorDraftStatus.Edit || c.Status == TorDraftStatus.Rejected))
                                .FirstOrDefaultAsync(c => c.IsActive && c.ProcurementId == appoint.ProcurementId, ct);

        if (torData is null)
        {
            return;
        }

        var committees = appoint.TorDraftCommittees.ToArray();

        var userIds = committees.Select(c => c.SuUserId).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        var existingCommitteeAcceptors = torData.PpTorDraftAcceptors
            .Where(a => a.Type == AcceptorType.TorDraftCommittee)
            .ToList();

        foreach (var acceptor in existingCommitteeAcceptors)
        {
            torData.PpTorDraftAcceptors.Remove(acceptor);
        }

        foreach (var committee in committees)
        {
            var user = users.First(u => u.Id == committee.SuUserId);
            var newAcceptor = PpTorDraftAcceptors.Create(
                new PpTorDraftAcceptors.AcceptorInfoData(
                    AcceptorType.TorDraftCommittee,
                    user.Id,
                    user.EmployeeCode,
                    user.FullName,
                    user.Employee.ConvertPositionName(procurement.DepartmentId),
                    user.Employee.View?.BusinessUnitName ?? string.Empty,
                    committee.Sequence),
                torData.Status);

            newAcceptor.SetCommitteePositionsCode(committee.CommitteePositionsCode);
            torData.PpTorDraftAcceptors.Add(newAcceptor);
        }
    }

    private async Task OverrideCommitteeMedianPrice(PpAppoint appoint, Domain.Procurement.Procurement procurement, CancellationToken ct)
    {
        var medianPriceData = await this.dbContext
                                        .PpMedianPrices
                                        .Include(a => a.Acceptors)
                                        .Where(c => c.IsActive && (c.Status == MedianPriceStatus.Draft || c.Status == MedianPriceStatus.Edit || c.Status == MedianPriceStatus.Rejected))
                                        .FirstOrDefaultAsync(c => c.IsActive && c.ProcurementId == appoint.ProcurementId, ct);

        if (medianPriceData is null)
        {
            return;
        }

        var committees = appoint.MedianPriceCommittees.ToArray();

        var userIds = committees.Select(c => c.SuUserId).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        var existingCommitteeAcceptors = medianPriceData.Acceptors
            .Where(a => a.Type == AcceptorType.MedianPriceCommittee)
            .ToList();

        foreach (var acceptor in existingCommitteeAcceptors)
        {
            medianPriceData.RemoveAcceptor(acceptor);
        }

        foreach (var committee in committees)
        {
            var user = users.First(u => u.Id == committee.SuUserId);
            var newAcceptor = PpMedianPriceAcceptor.Create(
                AcceptorType.MedianPriceCommittee,
                user,
                committee.Sequence,
                procurement.DepartmentId);

            newAcceptor.SetCommitteePositionsCode(committee.CommitteePositionsCode);
            medianPriceData.AddAcceptor(newAcceptor);
        }
    }

    private async ValueTask ReplaceDocumentAsync(
        PpAppoint appoint,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var replaceTemplate = appoint.LastedNotReplacedDocument;

        if (replaceTemplate is not null)
        {
            var replaceDto = await this.MapToReplaceDto(appoint, ct);

            var replaceDocumentAsync =
                documentService.CopyDocumentTemplateAsync(
                    replaceTemplate.FileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.Ap}/{appoint.AppointNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct);

            var finalFileId = await replaceDocumentAsync;

            if (finalFileId is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารที่ส่งเห็นชอบ");
            }

            appoint.AddDocumentHistory(finalFileId.Value, true);
        }
    }

    private static async Task SendNotificationAsync(PpAppoint appoint, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(appoint.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, appoint.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationCommitteeAsync(PpAppoint appoint, CancellationToken ct)
    {
        _ = await appoint.TorDraftCommittees.Map(pa =>
                             Notification
                                 .Crate(
                                     pa.SuUserId,
                                     NotificationConstant.InformTorCommittee.Title,
                                     string.Format(NotificationConstant.InformTorCommittee.Message, ProgramConstant.PreProcurementAppointment.Name, appoint.AppointNumber),
                                     NotificationProgram.Procurement)
                                 .SetReferenceId(appoint.Id.Value)
                                 .SetLinkUrl(
                                     string.Format(ProgramConstant.Procurement.Url, appoint.Procurement.Id),
                                     "ดูรายละเอียด"))
                         .Map(n => n.PublishAsync(ct).ToUnit())
                         .SequenceSerial();

        _ = await appoint.MedianPriceCommittees.Map(pa =>
                             Notification
                                 .Crate(
                                     pa.SuUserId,
                                     NotificationConstant.InformPriceCommittee.Title,
                                     string.Format(NotificationConstant.InformPriceCommittee.Message, ProgramConstant.PreProcurementAppointment.Name, appoint.AppointNumber),
                                     NotificationProgram.Procurement)
                                 .SetReferenceId(appoint.Id.Value)
                                 .SetLinkUrl(
                                     string.Format(ProgramConstant.Procurement.Url, appoint.Procurement.Id),
                                     "ดูรายละเอียด"))
                         .Map(n => n.PublishAsync(ct).ToUnit())
                         .SequenceSerial();
    }
}