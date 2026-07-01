namespace GHB.DP2.Application.Features.Procurement.Invite;

using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public record UpdateInviteRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId,
    bool IsInvite,
    DateTimeOffset? SubmitProposalStartDate,
    DateTimeOffset? SubmitProposalEndDate,
    DateTimeOffset? SubmitProposalStartTime,
    DateTimeOffset? SubmitProposalEndTime,
    DateTimeOffset? NeedToKnowWithinDate,
    DateTimeOffset? ClarifyDetailViaDate,
    string? PhoneNumber,
    PInviteStatus Status,
    bool IsDocumentReplace,
    UpdateInviteAcceptorRequest[]? Acceptors,
    UpdateInviteEntrepreneursRequest[]? InvitedEntrepreneurs,
    Guid? InviteDocumentId,
    bool? IsInviteDocumentIdReplace = false,
    DateTimeOffset? DocumentDate = null);

public record UpdateInviteAcceptorRequest(
    Guid Id,
    AcceptorType AcceptorType,
    Guid UserId,
    string EmployeeCode,
    string FullName,
    string PositionName,
    string DepartmentName,
    int Sequence,
    Guid? DelegateeId,
    AcceptorStatus Status,
    DateTimeOffset? ActionAt,
    string? Remark,
    bool IsActive,
    bool? IsCurrent,
    string? CommitteePositionsCode,
    string? CommitteePositionName,
    bool? IsUnableToPerformDuties);

public record UpdateSequenceInviteEntrepreneursRequest(
    Guid Id,
    int Sequence);

public record UpdateInviteResponse(Guid? NewDocumentFileId);

public class UpdateInviteEndpoint : InviteEndpointBase<UpdateInviteRequest, Results<Ok<UpdateInviteResponse>, NotFound<string>>>
{
    public class Validator : FastEndpoints.Validator<UpdateInviteRequest>
    {
        public Validator()
        {
            this.RuleFor(x => x.ProcurementId)
                .NotEmpty()
                .WithMessage("กรุณาระบุรหัสโครงการ");
            this.RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .When(x => x.IsInvite)
                .WithMessage("กรุณาระบุเบอร์โทรศัพท์");
            this.RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("สถานะไม่ถูกต้อง");

            this.When(x => x.Status == PInviteStatus.WaitingApproval && x.IsInvite, () =>
            {
                this.RuleFor(x => x.SubmitProposalStartDate)
                    .NotNull()
                    .WithMessage("กรุณาระบุวันเริ่มต้นยื่นข้อเสนอ");
                this.RuleFor(x => x.SubmitProposalEndDate)
                    .NotNull()
                    .WithMessage("กรุณาระบุวันสิ้นสุดยื่นข้อเสนอ");
                this.RuleFor(x => x.SubmitProposalStartTime)
                    .NotNull()
                    .WithMessage("กรุณาระบุเวลาเริ่มต้นยื่นข้อเสนอ");
                this.RuleFor(x => x.SubmitProposalEndTime)
                    .NotNull()
                    .WithMessage("กรุณาระบุเวลาสิ้นสุดยื่นข้อเสนอ");
            });
        }
    }

    private readonly Dp2DbContext dbContext;

    public UpdateInviteEndpoint(
        ILogger<UpdateInviteEndpoint> logger,
        Dp2DbContext dbContext)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/invite/{Id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/Invite")
                              .WithName("UpdateInvite")
                              .Produces<UpdateInviteResponse>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<UpdateInviteResponse>, NotFound<string>>> HandleRequestAsync(UpdateInviteRequest req, CancellationToken ct)
    {
        var invite = await this.GetPInviteById(PInviteId.From(req.Id), ProcurementId.From(req.ProcurementId), ct);

        var oldStatus = invite.Status;

        if (!invite.Procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            this.ValidateDocument(req, invite);
        }

        await this.UpdateAcceptorList(invite, req, UserId.From(req.UserId));
        UpdateEntrepreneurList(invite, req);
        UpdateInviteEntity(invite, req);

        if (req.Status == PInviteStatus.WaitingApproval
            || req.DocumentDate is not null)
        {
            invite.SetDocumentDate(req.DocumentDate);
        }

        if (oldStatus == req.Status)
        {
            invite.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "อัปเดตข้อมูลหนังสือเชิญชวน",
                invite.Status.ToString()));
        }
        else if (req.Status == PInviteStatus.WaitingApproval)
        {
            invite.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                invite.Status.ToString()));
        }

        await this.dbContext.SaveChangesAsync(ct);

        invite = await this.GetPInviteById(PInviteId.From(req.Id), ProcurementId.From(req.ProcurementId), ct);

        EnsureInitialCommitteeCurrents(invite);

        foreach (var entrepreneur in invite.InvitedEntrepreneurs)
        {
            if (entrepreneur.DocumentHistories == null || !entrepreneur.DocumentHistories.Any())
            {
                await this.SetDefaultDocumentTemplate(entrepreneur, invite.Status, ct);
            }

            await this.UpdateDocumentAsync(invite, entrepreneur, req.UserId, req.ProcurementId, req.IsDocumentReplace, false, ct);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateInviteResponse(null));
    }

    private void ValidateDocument(UpdateInviteRequest req, PInvite? invite)
    {
        if (req is { InviteDocumentId: not null, Status: PInviteStatus.WaitingApproval } &&
            (invite != null && !invite.IsMigration.GetValueOrDefault(false) && !invite.InvitedEntrepreneurs.Any(e => e.DocumentHistories.Any())))
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private static void UpdateInviteEntity(PInvite invite, UpdateInviteRequest req)
    {
        invite.Update(
            ProcurementId.From(req.ProcurementId),
            req.IsInvite,
            req.NeedToKnowWithinDate,
            req.ClarifyDetailViaDate,
            req.PhoneNumber,
            req.Status);
        invite.SetSubmitProposal(
            req.SubmitProposalStartDate,
            req.SubmitProposalEndDate,
            req.SubmitProposalStartTime,
            req.SubmitProposalEndTime);
    }

    private async Task UpdateAcceptorList(PInvite invite, UpdateInviteRequest req, UserId sendToAcceptorId)
    {
        if (req.Acceptors == null || req.Acceptors.Length == 0)
        {
            var all = invite.Acceptors.ToList();

            foreach (var acc in all)
            {
                invite.RemovePInviteAcceptor(acc);
            }

            return;
        }

        var acceptorIds = req.Acceptors.Select(a => a.Id).ToList();
        _ = invite.Acceptors.Where(a => !acceptorIds.Contains(a.Id.Value))
                  .Iter(r => invite.RemovePInviteAcceptor(r));

        foreach (var accDto in req.Acceptors)
        {
            var existing = invite.Acceptors.FirstOrDefault(a => a.Id == accDto.Id);

            if (existing == null)
            {
                await this.CreateInviteAcceptor(invite, accDto, req.Status, sendToAcceptorId);
            }
            else
            {
                UpdateInviteAcceptor(invite, accDto, req.Status, existing, sendToAcceptorId);
            }
        }
    }

    private static void UpdateInviteAcceptor(PInvite invite, UpdateInviteAcceptorRequest accDto, PInviteStatus status, PInviteAcceptors existing, UserId sendToAcceptorId)
    {
        var updatedStatus = DetermineAcceptorStatus(status);

        var positionName =
            !existing.User.IsNull() && existing.User.Employee != null
                ? existing.User.Employee.ConvertPositionName(invite.Procurement.DepartmentId)
                : accDto.PositionName;

        var fullName =
            !existing.User.IsNull() && existing.User.Employee != null
                ? existing.User.Employee.FullName
                : accDto.FullName;

        existing.Update(
            new PInviteAcceptors.AcceptorInfoData(
                accDto.AcceptorType,
                UserId.From(accDto.UserId),
                EmployeeCode.From(accDto.EmployeeCode),
                fullName,
                positionName,
                accDto.DepartmentName,
                accDto.Sequence),
            updatedStatus);

        existing.SetIsUnableToPerformDuties(accDto.IsUnableToPerformDuties ?? false);
        existing.SetSendToAcceptorId(sendToAcceptorId);

        if (accDto is { IsUnableToPerformDuties: true, Remark: not null })
        {
            existing.UnableToPerformDuties(accDto.Remark);
        }
        else
        {
            _ = status switch
            {
                PInviteStatus.WaitingApproval => existing.Pending(),
                _ => existing.Draft(),
            };
        }

        invite.UpdatePInviteAcceptor(existing);
    }

    private async Task CreateInviteAcceptor(PInvite invite, UpdateInviteAcceptorRequest accDto, PInviteStatus status, UserId sendToAcceptorId)
    {
        var employee = await this.dbContext.RawEmployees
                                 .Include(e => e.Positions)
                                 .Include(e => e.View)
                                 .SingleOrDefaultAsync(e =>
                                     e.Id == EmployeeCode.From(accDto.EmployeeCode));

        var newAcceptor = PInviteAcceptors.Create(
            new PInviteAcceptors.AcceptorInfoData(
                accDto.AcceptorType,
                UserId.From(accDto.UserId),
                EmployeeCode.From(accDto.EmployeeCode),
                employee?.FullName ?? accDto.FullName,
                employee?.ConvertPositionName(invite.Procurement.DepartmentId) ?? accDto.PositionName,
                accDto.DepartmentName,
                accDto.Sequence),
            status);

        if (accDto.CommitteePositionsCode != null)
        {
            newAcceptor.SetCommitteePositionsCode(ParameterCode.From(accDto.CommitteePositionsCode));
        }

        newAcceptor.SetIsUnableToPerformDuties(accDto.IsUnableToPerformDuties ?? false);
        newAcceptor.SetSendToAcceptorId(sendToAcceptorId);

        if (accDto.IsUnableToPerformDuties == true)
        {
            newAcceptor.UnableToPerformDuties(accDto.Remark);
        }
        else
        {
            _ = status switch
            {
                PInviteStatus.WaitingApproval => newAcceptor.Pending(),
                _ => newAcceptor.Draft(),
            };
        }

        invite.AddPInviteAcceptor(newAcceptor);
    }

    private static void UpdateEntrepreneurList(PInvite invite, UpdateInviteRequest req)
    {
        if (req.InvitedEntrepreneurs == null || req.InvitedEntrepreneurs.Length == 0)
        {
            var all = invite.InvitedEntrepreneurs.ToList();

            foreach (var ent in all)
            {
                invite.RemovePInviteEntrepreneurs(ent.Id);
            }

            return;
        }

        var invitedIds = req.InvitedEntrepreneurs.Select(e => e.Id).ToList();
        var toRemove = invite.InvitedEntrepreneurs.Where(e => !invitedIds.Contains(e.Id.Value)).ToList();

        foreach (var ent in toRemove)
        {
            invite.RemovePInviteEntrepreneurs(ent.Id);
        }

        foreach (var entDto in req.InvitedEntrepreneurs)
        {
            var existing = invite.InvitedEntrepreneurs.FirstOrDefault(e => e.Id == entDto.Id);

            if (existing == null)
            {
                throw new InvalidOperationException($"ไม่พบข้อมูลผู้ประกอบการ");
            }

            var updatedEnt = existing.SetSequence(entDto.Sequence);
            invite.UpdatePInviteEntrepreneurs(updatedEnt);
        }
    }

    private static AcceptorStatus DetermineAcceptorStatus(PInviteStatus requestStatus)
    {
        return requestStatus is PInviteStatus.WaitingApproval
            ? AcceptorStatus.Pending
            : AcceptorStatus.Draft;
    }

    private static void EnsureInitialCommitteeCurrents(PInvite entity)
    {
        if (entity.Status != PInviteStatus.WaitingApproval)
        {
            return;
        }

        if (entity.Procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            var allAcceptors = entity.Acceptors
                                     .Where(a => a is { Type: AcceptorType.Approver, IsActive: true, IsUnableToPerformDuties: false, Status: AcceptorStatus.Pending })
                                     .OrderBy(a => a.Sequence)
                                     .ToArray();

            allAcceptors.Iter(r => r.SetCurrent(false));

            var next = allAcceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                                   .FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

            if (next is null)
            {
                return;
            }

            next.SetCurrent();

            foreach (var accept in allAcceptors)
            {
                _ = SendNotificationAsync(
                    entity,
                    accept.UserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementInvite.Name, entity.InviteNumber));
            }

            return;
        }

        var committee = entity.Acceptors?
                              .Where(a => a is { Type: AcceptorType.ProcurementCommittee, IsActive: true, IsUnableToPerformDuties: false, Status: AcceptorStatus.Pending })
                              .ToList();

        if (committee == null || committee.Count == 0)
        {
            return;
        }

        if (entity.Acceptors!.Any(a => a.Type == AcceptorType.ProcurementCommittee && a.Status == AcceptorStatus.Approved))
        {
            return;
        }

        var chairman = committee.FirstOrDefault(a => a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
                       ?? committee.FirstOrDefault(a => a.IsBoardChairman());

        foreach (var a in committee)
        {
            a.SetCurrent(false);
        }

        var nonChair = committee.Where(a => chairman == null || a.Id != chairman.Id).ToList();

        if (nonChair.Count == 0 && chairman != null)
        {
            chairman.SetCurrent();
            _ = SendNotificationAsync(
                entity,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementInvite.Name, entity.InviteNumber));

            return;
        }

        foreach (var a in nonChair)
        {
            _ = SendNotificationAsync(
                entity,
                a.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementInvite.Name, entity.InviteNumber));
            a.SetCurrent();
        }

        if (chairman != null)
        {
            chairman.SetCurrent(false);
        }
    }

    private static async Task SendNotificationAsync(PInvite entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, entity.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}