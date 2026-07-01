namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record UpdateContractTerminationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ContractDraftVendorId,
    string TerminateType,
    DateTimeOffset TerminationDate,
    string? TerminateReasonOther,
    string? TerminateReason,
    string? TerminateReasonDetail,
    CmContractTerminationStatus Status,
    Guid? ContractTerminationDocumentId,
    bool IsContractTerminationDocumentIdReplace,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees);

public record UpdateContractTerminationResponse(Guid? NewDocumentFileId);

public class UpdateTerminationValidator : Validator<UpdateContractTerminationRequest>
{
    public UpdateTerminationValidator()
    {
        this.RuleFor(x => x.ContractDraftVendorId)
            .NotEmpty()
            .WithMessage("กรุณาระบุรหัสสัญญา");
        this.RuleFor(x => x.TerminationDate)
            .NotEmpty()
            .WithMessage("กรุณาระบุวันที่ยกเลิกสัญญา");
        this.RuleFor(x => x.TerminateType)
            .NotEmpty()
            .WithMessage("กรุณาระบุเหตุผลการยกเลิกสัญญา");

        this.When(x => x.Status == CmContractTerminationStatus.WaitingCommitteeApproval, () =>
        {
            this.RuleFor(x => x.Acceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");
            this.RuleFor(x => x.Assignees)
                .NotNull().WithMessage("ต้องระบุผู้รับผิดชอบอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้รับผิดชอบอย่างน้อย 1 คน");
        });
    }
}

public class UpdateContractTerminationEndpoint : ContractTerminationEndpoint<UpdateContractTerminationRequest, Results<Ok<UpdateContractTerminationResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateContractTerminationEndpoint(ILogger<UpdateContractTerminationEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("contract/{ContractDraftVendorId:guid}/contract-termination/{id:guid}");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractTermination")
                              .WithName("UpdateContractTermination")
                              .AllowAnonymous()
                              .Produces<Ok>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<UpdateContractTerminationResponse>, NotFound<string>>> HandleRequestAsync(UpdateContractTerminationRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var termination = entity.CmContractTerminations
                                .FirstOrDefault(s => s.Id == CmContractTerminationId.From(req.Id));

        var delivery = entity.Delivery;

        var suVendor = this.MapSuVendorByType(entity.ContractInvitationVendors, entity.ContractDraft.Procurement.Type);

        if (suVendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการ");
        }

        if (termination is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลการบอกเลิกสัญญารหัส {req.Id}");
        }

        if (req.Acceptors != null)
        {
            await this.UpsertAcceptors(termination, req.Acceptors, req.Status);
        }

        if (req.Assignees != null)
        {
            await this.UpsertAssignee(termination, req.Assignees, ct);
        }

        if (termination.Status == req.Status)
        {
            termination.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    string.Empty,
                    termination.Status.ToString()));
        }

        var oldStatus = termination.Status;

        termination.SetValues(
                       ParameterCode.From(req.TerminateType),
                       req.TerminateReason,
                       req.TerminationDate)
                   .SetTerminateReasonDetail(req.TerminateReasonDetail)
                   .SetTerminateReasonOther(req.TerminateReasonOther)
                   .SetStatus(req.Status);

        FileId? newDocumentFileId = null;

        // Committee Recall: WaitingCommitteeApproval → Draft — restore from Draft checkpoint
        if (oldStatus is CmContractTerminationStatus.WaitingCommitteeApproval
            && termination.Status is CmContractTerminationStatus.Draft)
        {
            var draftOrRejectedCheckpoint = termination.LastedDraftOrRejectedDocument;

            if (draftOrRejectedCheckpoint != null)
            {
                await this.CopyCheckpointDocumentAsync(termination, draftOrRejectedCheckpoint, ct);
            }
        }

        // Draft/Rejected: document is editable in frontend
        else if (termination.Status is CmContractTerminationStatus.Draft
                 or CmContractTerminationStatus.Rejected
                 or CmContractTerminationStatus.RejectToAssignee)
        {
            if (req is { ContractTerminationDocumentId: not null, IsContractTerminationDocumentIdReplace: true })
            {
                // IsReplace=true: Major version from template (reset)
                await this.ResetDocumentFromTemplateAsync(entity, termination, ct);
            }
            else if (req.ContractTerminationDocumentId.HasValue)
            {
                // IsReplace=false: Minor version from latest (save edit)
                newDocumentFileId = await this.UpdateDocumentHistoryAsync(
                    termination,
                    FileId.From(req.ContractTerminationDocumentId!.Value),
                    false,
                    ct);
            }
        }

        // WaitingCommitteeApproval: replace all EXCEPT Acceptors/AcceptorDate, mark as replaced
        if (termination.Status == CmContractTerminationStatus.WaitingCommitteeApproval)
        {
            var options = new TerminationDocumentOptions(true, true, false, MarkAsReplaced: true);
            await this.UpdateDocumentAsync(entity, termination, delivery, suVendor, req.UserId, options, ct);

            var committeeMembers = termination.Acceptors
                    .Where(w => w is { Type: AcceptorType.AcceptanceCommittee, IsUnableToPerformDuties: false, IsActive: true })
                    .Where(a => a.Status == AcceptorStatus.Pending)
                    .ToList();

            foreach (var member in committeeMembers)
            {
                _ = SendNotificationAsync(
                    termination,
                    member.UserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(
                        NotificationConstant.WaitForLike.Message,
                        ProgramConstant.ContractTermination.Name,
                        termination.CaContractDraftVendor.ContractDraftNumber));
            }
        }

        if (oldStatus != CmContractTerminationStatus.WaitingComment
            && termination.Status == CmContractTerminationStatus.WaitingComment)
        {
            var lastedWaitingAssignDocument = termination.LastedWaitingAssignDocument;

            if (lastedWaitingAssignDocument is not null)
            {
                await this.CopyCheckpointDocumentAsync(termination, lastedWaitingAssignDocument, ct, isReplace: true, incrementMajor: true);
            }
        }

        // WaitingApproval: checkpoint document (Acceptor placeholders preserved)
        if (oldStatus != CmContractTerminationStatus.WaitingApproval
            && termination.Status == CmContractTerminationStatus.WaitingApproval)
        {
            // Checkpoint latest editor content (document may have been edited in WaitingComment)
            if (req.ContractTerminationDocumentId.HasValue)
            {
                await this.UpdateDocumentHistoryAsync(
                    termination,
                    FileId.From(req.ContractTerminationDocumentId.Value),
                    false,
                    ct);
            }

            // Use latest WaitingComment document (with edits) instead of Draft/Rejected
            var sourceDoc = termination.LastedWaitingCommentDocument
                            ?? termination.LastedDraftOrRejectedDocument;

            var options = new TerminationDocumentOptions(true, false, false, MarkAsReplaced: true);
            await this.UpdateDocumentAsync(entity, termination, delivery, suVendor, req.UserId, options, ct, sourceDoc);
        }

        // Same-status edit: checkpoint for WaitingAssign/WaitingComment/WaitingApproval
        if (oldStatus == req.Status
            && termination.Status is CmContractTerminationStatus.WaitingAssign
                                     or CmContractTerminationStatus.WaitingComment
                                     or CmContractTerminationStatus.WaitingApproval)
        {
            if (req.ContractTerminationDocumentId.HasValue)
            {
                newDocumentFileId = await this.UpdateDocumentHistoryAsync(
                    termination,
                    FileId.From(req.ContractTerminationDocumentId.Value),
                    false,
                    ct);
            }
        }

        this.dbContext.CmContractTerminations.Update(termination);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok(new UpdateContractTerminationResponse(newDocumentFileId?.Value));
    }
}