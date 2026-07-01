namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateReportContractCompletionByQuarterRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    RpContractCompletionByQuarterStatus Status,
    DateTimeOffset DocumentDate,
    int Year,
    int Quarter,
    DateTimeOffset SignStartDate,
    DateTimeOffset SignEndDate,
    Guid? DocumentId,
    bool? IsDocumentReplace,
    IEnumerable<RpContractCompletionByQuarterDetailDto>? Detail,
    IEnumerable<AcceptorRequest>? Acceptors);

public record UpdateReportContractCompletionByQuarterResponse(Guid? NewDocumentFileId);

public class UpdateReportContractCompletionByQuarterValidator : Validator<UpdateReportContractCompletionByQuarterRequest>
{
    public UpdateReportContractCompletionByQuarterValidator()
    {
        this.RuleFor(x => x.DocumentDate)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลวันที่เอกสาร");

        this.RuleFor(x => x.Year)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลปีงบประมาณ");

        this.RuleFor(x => x.Quarter)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลไตรมาส");

        this.RuleFor(x => x.SignStartDate)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลวันที่ลงนามในสัญญาเริ่มต้น");

        this.RuleFor(x => x.SignEndDate)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลวันที่ลงนามในสัญญาสิ้นสุด");

        this.When(x => x.Status == RpContractCompletionByQuarterStatus.WaitingApproval, () =>
        {
            this.RuleFor(x => x.Acceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");

            this.RuleFor(x => x.Detail)
                .NotNull().WithMessage("ต้องมีข้อมูลรายการสัญญาอย้างน้อย 1 รายการ")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องมีข้อมูลรายการสัญญาอย้างน้อย 1 รายการ");
        });
    }
}

public class UpdateReportContractCompletionByQuarterEndpoint : ContractCompletionByQuarterEndpoint<UpdateReportContractCompletionByQuarterRequest, Results<Ok<UpdateReportContractCompletionByQuarterResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateReportContractCompletionByQuarterEndpoint(ILogger<UpdateReportContractCompletionByQuarterEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("report/contract-completion-by-quarter/{id:guid}");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("UpdateReportContractCompletionByQuarter")
                              .WithSummary("แก้ไขรายงานการสรุปผลสัญญาตามไตรมาส")
                              .AllowAnonymous()
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<UpdateReportContractCompletionByQuarterResponse>, NotFound<string>>> HandleRequestAsync(UpdateReportContractCompletionByQuarterRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpContractCompletionByQuarters
                               .Include(x => x.Details)
                               .Include(x => x.Acceptors)
                               .ThenInclude(x => x.User)
                               .ThenInclude(x => x.Employee)
                               .Include(x => x.DocumentHistories)
                               .FirstOrDefaultAsync(x => x.Id == RpContractCompletionByQuarterId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        var originalStatus = entity.Status;

        entity.SetValues(
            entity.DocumentNumber,
            req.Year,
            req.Quarter,
            req.DocumentDate,
            req.SignStartDate,
            req.SignEndDate);

        // Update Acceptors
        if (req.Acceptors != null)
        {
            this.UpsertAcceptors(entity, req.Acceptors, req.Status, UserId.From(req.UserId));
        }

        // Update Details
        if (req.Detail != null)
        {
            this.UpsertDetails(entity, req.Detail);
        }

        entity.SetStatus(req.Status);

        if (req.Status == RpContractCompletionByQuarterStatus.WaitingApproval)
        {
            entity.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.SendApprove,
                    ActivityLogActionTypeConstant.SendApprove,
                    entity.Status.ToString()));
        }

        await this.dbContext.SaveChangesAsync(ct);

        var isReplaced = req.IsDocumentReplace ?? false;

        await this.ManageDocumentForSaveAsync(
            entity,
            originalStatus,
            req.Status,
            isReplaced,
            UserId.From(req.UserId),
            ct);

        SendNotificationWhenWaitingApproval(entity, originalStatus, req.Status);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateReportContractCompletionByQuarterResponse(null));
    }

    private static void SendNotificationWhenWaitingApproval(RpContractCompletionByQuarter entity, RpContractCompletionByQuarterStatus previousStatus, RpContractCompletionByQuarterStatus newStatus)
    {
        if (previousStatus == RpContractCompletionByQuarterStatus.WaitingApproval || newStatus != RpContractCompletionByQuarterStatus.WaitingApproval)
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
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractCompletionByQuarter.Name, entity.DocumentNumber));
            }
            else
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.ContractCompletionByQuarter.Name, entity.DocumentNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(RpContractCompletionByQuarter entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Report)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractCompletionByQuarter.Url, entity.Id.Value), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}