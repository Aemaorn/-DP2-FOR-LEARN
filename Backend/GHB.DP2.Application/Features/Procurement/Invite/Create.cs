namespace GHB.DP2.Application.Features.Procurement.Invite;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

public record CreateInviteRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
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
    DateTimeOffset? DocumentDate);

public record AcceptorFullRequestDto(
    AcceptorType AcceptorType,
    Guid UserId,
    string EmployeeCode,
    string FullName,
    string PositionName,
    string DepartmentName,
    int Sequence,
    Guid? DelegateeId);

public class Validator : Validator<CreateInviteRequest>
{
    public Validator()
    {
        this.RuleFor(x => x.ProcurementId)
            .NotEmpty()
            .WithMessage("กรุณาระบุรหัสโครงการ");
        this.RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("สถานะไม่ถูกต้อง");
    }
}

public class CreateInviteEndpoint : InviteEndpointBase<CreateInviteRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateInviteEndpoint(
        ILogger<CreateInviteEndpoint> logger,
        Dp2DbContext dbContext)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{procurementId:guid}/invite");
        this.Description(b => b
                              .WithTags("Procurement/Invite")
                              .WithName("CreateInvite")
                              .AllowAnonymous()
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreateInviteRequest req, CancellationToken ct)
    {
        var procurement = await this.ValidateRequestAsync(req, ct);

        var invite = PInvite.CreateInvite(
                                procurement,
                                req.IsInvite,
                                req.NeedToKnowWithinDate,
                                req.ClarifyDetailViaDate,
                                req.PhoneNumber,
                                req.Status)
                            .SetSubmitProposal(
                                req.SubmitProposalStartDate,
                                req.SubmitProposalEndDate,
                                req.SubmitProposalStartTime,
                                req.SubmitProposalEndTime);

        if (!(procurement.Budget > 100000 && procurement.SupplyMethodCode == SupplyMethodConstant.Sixty))
        {
            await this.AddAcceptors(invite, req.Status, procurement.DepartmentId, UserId.From(req.UserId), ct);
        }

        if (req.DocumentDate is not null)
        {
            invite.SetDocumentDate(req.DocumentDate);
        }

        invite.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            "สร้างข้อมูลหนังสือเชิญชวน",
            invite.Status.ToString()));

        this.dbContext.PInvites.Add(invite);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        if (req.IsInvite)
        {
            var inviteReloaded = await this.GetPInviteById(invite.Id, invite.ProcurementId, ct);

            foreach (var entrepreneur in inviteReloaded.InvitedEntrepreneurs)
            {
                if (entrepreneur.DocumentHistories == null || !entrepreneur.DocumentHistories.Any())
                {
                    await this.SetDefaultDocumentTemplate(entrepreneur, inviteReloaded.Status, ct);
                }

                await this.UpdateDocumentAsync(inviteReloaded, entrepreneur, req.UserId, req.ProcurementId, true, false, ct);
            }

            await this.dbContext.SaveChangesAsync(ct);
        }

        return TypedResults.Created(string.Empty, invite.Id.Value);
    }

    private async Task<Procurement> ValidateRequestAsync(CreateInviteRequest req, CancellationToken ct)
    {
        var procurement = await
            this.dbContext.Procurements
                .SingleOrDefaultAsync(
                    p => p.Id == ProcurementId.From(req.ProcurementId),
                    ct);

        if (procurement is null)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลโครงการที่มีรหัส {req.ProcurementId}",
                StatusCodes.Status404NotFound);
        }

        return procurement;
    }

    private async Task AddAcceptors(PInvite invite, PInviteStatus inviteStatus, BusinessUnitId workBusinessUnitId, UserId sendToAcceptorId, CancellationToken ct)
    {
        var jp005procurementSuppliesDivisions = await this.dbContext.PJp005S
                                                          .Include(x => x.ProcurementSuppliesDivisions)
                                                          .ThenInclude(x => x.SuUser)
                                                          .ThenInclude(x => x.Employee)
                                                          .Where(w => w.IsActive && w.ProcurementId == invite.ProcurementId)
                                                          .SelectMany(s => s.ProcurementSuppliesDivisions)
                                                          .ToArrayAsync(ct);

        var suppliesDivisions =
            jp005procurementSuppliesDivisions.Map(s =>
                PInviteAcceptors.Create(
                                    new PInviteAcceptors.AcceptorInfoData(
                                        AcceptorType.ProcurementCommittee,
                                        s.SuUserId,
                                        s.SuUser.EmployeeCode,
                                        s.SuUser.Employee.View!.FullName,
                                        s.SuUser.Employee.ConvertPositionName(workBusinessUnitId),
                                        s.SuUser.Employee.View.BusinessUnitName,
                                        s.Sequence),
                                    inviteStatus)
                                .SetCommitteePositionsCode(ParameterCode.From(SuParameterCodeConstant.PosBoard006)));

        var maxSuppliesSequence = suppliesDivisions.Any() ? suppliesDivisions.Max(s => s.Sequence) : 0;

        var jp005committees = await this.dbContext.PJp005S
                                        .Include(x => x.Committees)
                                        .ThenInclude(x => x.User)
                                        .ThenInclude(x => x.Employee)
                                        .Where(w => w.IsActive && w.ProcurementId == invite.ProcurementId)
                                        .SelectMany(s => s.Committees)
                                        .Where(w => w.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                                        .ToArrayAsync(ct);

        var committees = jp005committees
                         .Select((s, index) =>
                             PInviteAcceptors.Create(
                                                 new PInviteAcceptors.AcceptorInfoData(
                                                     AcceptorType.ProcurementCommittee,
                                                     s.SuUserId,
                                                     s.User.EmployeeCode,
                                                     s.User.Employee.View!.FullName,
                                                     s.User.Employee.ConvertPositionName(workBusinessUnitId),
                                                     s.User.Employee.View.BusinessUnitName,
                                                     maxSuppliesSequence == 0 ? s.Sequence : (maxSuppliesSequence + index + 1)),
                                                 inviteStatus)
                                             .SetCommitteePositionsCode(s.CommitteePositionsCode))
                         .ToArray();

        _ = suppliesDivisions.Union(committees).Iter(r =>
        {
            r.SetSendToAcceptorId(sendToAcceptorId);
            invite.AddPInviteAcceptor(r);
        });
    }
}