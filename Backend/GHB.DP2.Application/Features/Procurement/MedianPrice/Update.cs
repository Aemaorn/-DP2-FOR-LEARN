namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateMedianPriceRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid MedianPriceId,
    string Object,
    string Reason,
    string? Telephone,
    string? SpecialDescription,
    string? JobDescription,
    string PriceReasonablenessInfo,
    string MedianPriceDocumentTemplateCode,
    MedianPriceStatus Status,
    Guid? MedianPriceDocumentId,
    bool? IsMedianPriceDocumentIdReplaced,
    DateTimeOffset? DocumentDate,
    UpdateBudgetAllocations BudgetAllocations,
    UpdateStaff? Staff,
    MedianPriceExpenseDescriptionInfo? ExpenseDescription,
    MedianPriceAcceptorInfo[]? Acceptors,
    MedianPriceAssigneeInfo[]? Assignees,
    bool HasMd,
    string? CancelReason,
    string? ChangeReason)
{
    public class Validator : Validator<UpdateMedianPriceRequest>
    {
        public Validator()
        {
            this.RuleFor(x => x.ProcurementId)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่าง Procurement ID ได้");

            this.RuleFor(x => x.MedianPriceId)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่าง Median Price ID ได้");

            this.RuleFor(x => x.Object)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่างวัตถุประสงค์ได้");

            this.RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่างเหตุผลได้");

            this.RuleFor(x => x.PriceReasonablenessInfo)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่างข้อมูลความสมเหตุสมผลของราคาได้");

            this.RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("สถานะราคากลางไม่ถูกต้อง")
                .Must(s =>
                    s is MedianPriceStatus.Draft or
                        MedianPriceStatus.Rejected or
                        MedianPriceStatus.WaitingCommitteeApproval or
                        MedianPriceStatus.Edit or
                        MedianPriceStatus.WaitingAssign or
                        MedianPriceStatus.WaitingComment or
                        MedianPriceStatus.RejectToAssignee or
                        MedianPriceStatus.WaitingApproval)
                .WithMessage("สถานะราคากลางไม่รองรับการแก้ไข");

            this.RuleFor(x => x.BudgetAllocations)
                .NotNull()
                .WithMessage("จำเป็นต้องระบุการจัดสรรงบประมาณ")
                .SetValidator(new UpdateBudgetAllocations.Validator());

            this.RuleFor(x => x.Acceptors)
                .NotNull()
                .When(x => x.Status is MedianPriceStatus.WaitingCommitteeApproval)
                .WithMessage("ต้องมีบุคคล/คณะกรรมการกำหนดราคากลางอย่างน้อย 1 คน");

            this.RuleFor(x => x.Acceptors)
                .Must(x => x != null && x.Any(m => m.AcceptorType == AcceptorType.MedianPriceCommittee))
                .When(w => w.Status is MedianPriceStatus.WaitingCommitteeApproval && !w.HasMd)
                .WithMessage("ต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");

            this.RuleFor(x => x.Acceptors)
                .Must(x => x != null && x.Any(m => m.AcceptorType == AcceptorType.DepartmentDirectorAgree))
                .When(w => w.Status is MedianPriceStatus.WaitingCommitteeApproval && w.HasMd)
                .WithMessage("ต้องมีสายงานเห็นชอบอย่างน้อย 1 คน");
        }
    }
}

public record UpdateBudgetAllocations(
    BudgetAllocationsId? Id,
    decimal Budget,
    decimal ReferenceMedianPrice,
    UpdateBudgetAllocationsDetail[] Details)
{
    public PpMedianPriceBudgetAllocations CreateToEntity()
    {
        if (this.Id is not null)
        {
            throw new InvalidOperationException("Cannot create a new budget allocations with an existing Id.");
        }

        var budgetAllocations = PpMedianPriceBudgetAllocations.Create(
            this.Budget,
            this.ReferenceMedianPrice);

        if (this.Details.Any(d => d.Id is not null))
        {
            throw new InvalidOperationException("Cannot create a new budget allocation with existing detail Ids.");
        }

        foreach (var detail in this.Details)
        {
            budgetAllocations.AddDetail(detail.ToEntity());
        }

        return budgetAllocations;
    }

    public class Validator : Validator<UpdateBudgetAllocations>
    {
        public Validator()
        {
            this.RuleFor(x => x.Budget)
                .GreaterThan(0)
                .WithMessage("งบประมาณต้องมากกว่า 0");

            this.RuleFor(x => x.ReferenceMedianPrice)
                .GreaterThan(0)
                .WithMessage("ราคากลางอ้างอิงต้องมากกว่า 0");

            this.RuleFor(x => x.Details)
                .NotEmpty()
                .WithMessage("ต้องมีรายละเอียดการจัดสรรงบประมาณอย่างน้อยหนึ่งรายการ");
        }
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UpdateBudgetAllocationsWithDetail), nameof(BudgetAllocationsDetailType.With))]
[JsonDerivedType(typeof(UpdateBudgetAllocationsWithoutDetail), nameof(BudgetAllocationsDetailType.Without))]
public abstract record UpdateBudgetAllocationsDetail(
    BudgetAllocationsDetailId? Id,
    int Sequence,
    string Source)
{
    public abstract PpMedianPriceBudgetAllocationsDetail ToEntity();
}

public record UpdateBudgetAllocationsWithDetail(
    BudgetAllocationsDetailId? Id,
    int Sequence,
    string Source,
    decimal ReferenceBudge) : UpdateBudgetAllocationsDetail(Id, Sequence, Source)
{
    public override PpMedianPriceBudgetAllocationsDetail ToEntity()
    {
        if (this.Id is not null)
        {
            return PpMedianPriceBudgetAllocationsWithDetail.CreateWithDetail(
                this.Id.Value,
                this.Sequence,
                this.Source,
                this.ReferenceBudge);
        }
        else
        {
            return PpMedianPriceBudgetAllocationsWithDetail.CreateWithDetail(
                this.Sequence,
                this.Source,
                this.ReferenceBudge);
        }
    }

    public class Validator : Validator<UpdateBudgetAllocationsWithDetail>
    {
        public Validator()
        {
            this.RuleFor(x => x.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับต้องมากกว่า 0");

            this.RuleFor(x => x.Source)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่างแหล่งที่มาได้");

            this.RuleFor(x => x.ReferenceBudge)
                .GreaterThan(0)
                .WithMessage("งบประมาณอ้างอิงต้องมากกว่า 0");
        }
    }
}

public record UpdateBudgetAllocationsWithoutDetail(
    BudgetAllocationsDetailId? Id,
    int Sequence,
    string Source) : UpdateBudgetAllocationsDetail(Id, Sequence, Source)
{
    public override PpMedianPriceBudgetAllocationsDetail ToEntity()
    {
        if (this.Id is not null)
        {
            return PpMedianPriceBudgetAllocationsWithoutDetail.CreateWithoutDetail(
                this.Id.Value,
                this.Sequence,
                this.Source);
        }
        else
        {
            return PpMedianPriceBudgetAllocationsWithoutDetail.CreateWithoutDetail(
                this.Sequence,
                this.Source);
        }
    }

    public class Validator : Validator<UpdateBudgetAllocationsWithoutDetail>
    {
        public Validator()
        {
            this.RuleFor(x => x.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับต้องมากกว่า 0");

            this.RuleFor(x => x.Source)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่างแหล่งที่มาได้");
        }
    }
}

public record UpdateStaff(
    MedianPriceStaffId? Id,
    decimal PersonnelCompensation,
    int PersonnelCount,
    UpdateStaffDetail[] Details)
{
    public PpMedianPriceStaff CreateToEntity()
    {
        if (this.Id is not null)
        {
            throw new InvalidOperationException("Cannot create a new staff with an existing Id.");
        }

        var staff = PpMedianPriceStaff.Create(
            this.PersonnelCompensation,
            this.PersonnelCount);

        if (this.Details.Any(d => d.Id is not null))
        {
            throw new InvalidOperationException("Cannot create a new staff with existing detail Ids.");
        }

        foreach (var detail in this.Details)
        {
            staff.AddDetail(detail.ToEntity());
        }

        return staff;
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UpdateStaffPersonnelDetail), nameof(MedianPriceStaffType.Personal))]
[JsonDerivedType(typeof(UpdateStaffConsultantTypeDetail), nameof(MedianPriceStaffType.ConsultantTypes))]
[JsonDerivedType(typeof(UpdateStaffConsultantQualificationDetail), nameof(MedianPriceStaffType.ConsultantQualifications))]
public abstract record UpdateStaffDetail(
    MedianPriceStaffDetailId? Id,
    int Sequence,
    string Description)
{
    public abstract PpMedianPriceStaffDetail ToEntity();
}

public record UpdateStaffPersonnelDetail(
    MedianPriceStaffDetailId? Id,
    int Sequence,
    string Description,
    int PersonnelCount) : UpdateStaffDetail(Id, Sequence, Description)
{
    public class Validator : Validator<UpdateStaffPersonnelDetail>
    {
        public Validator()
        {
            this.RuleFor(x => x.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับต้องมากกว่า 0");

            this.RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่างคำอธิบายได้");

            this.RuleFor(x => x.PersonnelCount)
                .GreaterThan(0)
                .WithMessage("จำนวนบุคลากรต้องมากกว่า 0");
        }
    }

    public override PpMedianPriceStaffDetail ToEntity()
    {
        if (this.Id is not null)
        {
            return PpMedianPriceStaffPersonal.Create(
                this.Id.Value,
                this.Sequence,
                this.Description,
                this.PersonnelCount);
        }
        else
        {
            return PpMedianPriceStaffPersonal.Create(
                this.Sequence,
                this.Description,
                this.PersonnelCount);
        }
    }
}

public record UpdateStaffConsultantTypeDetail(
    MedianPriceStaffDetailId? Id,
    int Sequence,
    string Description) : UpdateStaffDetail(Id, Sequence, Description)
{
    public override PpMedianPriceStaffDetail ToEntity()
    {
        if (this.Id is not null)
        {
            return PpMedianPriceStaffConsultantTypes.Create(
                this.Id.Value,
                this.Sequence,
                this.Description);
        }
        else
        {
            return PpMedianPriceStaffConsultantTypes.Create(
                this.Sequence,
                this.Description);
        }
    }

    public class Validator : Validator<UpdateStaffConsultantTypeDetail>
    {
        public Validator()
        {
            this.RuleFor(x => x.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับต้องมากกว่า 0");

            this.RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่างคำอธิบายได้");
        }
    }
}

public record UpdateStaffConsultantQualificationDetail(
    MedianPriceStaffDetailId? Id,
    int Sequence,
    string Description) : UpdateStaffDetail(Id, Sequence, Description)
{
    public override PpMedianPriceStaffDetail ToEntity()
    {
        if (this.Id is not null)
        {
            return PpMedianPriceStaffConsultantQualifications.Create(
                this.Id.Value,
                this.Sequence,
                this.Description);
        }
        else
        {
            return PpMedianPriceStaffConsultantQualifications.Create(
                this.Sequence,
                this.Description);
        }
    }

    public class Validator : Validator<UpdateStaffConsultantQualificationDetail>
    {
        public Validator()
        {
            this.RuleFor(x => x.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับต้องมากกว่า 0");

            this.RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("ไม่สามารถเว้นว่างคำอธิบายได้");
        }
    }
}

public record UpdateMedianPriceResponse(Guid? NewMedianPriceDocumentFileId);

public class UpdateMedianPriceEndpoint : MedianPriceEndpointBase<UpdateMedianPriceRequest, Results<Ok<UpdateMedianPriceResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateMedianPriceEndpoint(
        ILogger<UpdateMedianPriceEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/median-price/{MedianPriceId:guid}");
        this.Description(b => b
                              .WithTags(nameof(MedianPrice))
                              .WithName("UpdateMedianPrice")
                              .Produces<Ok<UpdateMedianPriceResponse>>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<UpdateMedianPriceResponse>, NotFound<string>>> HandleRequestAsync(UpdateMedianPriceRequest req, CancellationToken ct)
    {
        var procurement = await this.GetProcurementById(ProcurementId.From(req.ProcurementId), ct);

        var medianPrice = procurement.MedianPrices.FirstOrDefault(x => x.Id == MedianPriceId.From(req.MedianPriceId));

        this.ValidateDocument(req, medianPrice);

        if (medianPrice is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลการกำหนดราคากลาง รหัส {req.MedianPriceId}");
        }

        var previousStatus = medianPrice.Status;

        // Update basic properties
        _ = medianPrice
            .SetObject(req.Object)
            .SetReason(req.Reason)
            .SetSpecialDescription(req.SpecialDescription ?? string.Empty)
            .SetJobDescription(req.JobDescription)
            .SetPriceReasonablenessInfo(req.PriceReasonablenessInfo)
            .SetTelephone(req.Telephone);

        if (!string.IsNullOrWhiteSpace(req.CancelReason))
        {
            medianPrice.SetCancelReason(req.CancelReason);
        }

        if (!string.IsNullOrWhiteSpace(req.ChangeReason))
        {
            medianPrice.SetChangeReason(req.ChangeReason);
        }

        if (req.Status == MedianPriceStatus.WaitingCommitteeApproval
            || req.DocumentDate is not null)
        {
            medianPrice.SetDocumentDate(req.DocumentDate);
        }

        // Update budget allocations
        UpsertBudgetAllocations(medianPrice, req.BudgetAllocations);

        // Update staff implementation here
        UpsertStaff(medianPrice, req.Staff);

        // Update expense description implementation here
        UpdateExpenseDescription(medianPrice, req.ExpenseDescription);

        var lastAssigneeUserId = req.Assignees?
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        // Update acceptor implementation here
        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(medianPrice, req.Acceptors, procurement.DepartmentId, lastAssigneeUserId ?? UserId.From(req.UserId));
        }

        if (req.Assignees is not null && req.Assignees.Length > 0)
        {
            var newAssignees = req.Assignees.Where(x => x is { AssigneeType: AssigneeType.Assignee, Id: null });

            foreach (var inComing in newAssignees)
            {
                await SendNotificationAsync(
                    medianPrice,
                    UserId.From(inComing.UserId),
                    NotificationConstant.Assignment.Title,
                    string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PreProcurementMedianPrice.Name, medianPrice.ReferenceNumber));
            }

            await this.UpsertAssignee(medianPrice, req.Assignees, UserId.From(req.UserId), CancellationToken.None);
        }

        await this.UpdateDocumentTemplate(medianPrice, req.MedianPriceDocumentTemplateCode, ct);

        var isReplaceDocument = req.IsMedianPriceDocumentIdReplaced ?? false;

        var mustSaveDocument =
            isReplaceDocument &&
            req.MedianPriceDocumentId.HasValue &&
            medianPrice.Status != MedianPriceStatus.WaitingCommitteeApproval;

        FileId? newMedianPriceDocumentFileId = null;

        if (mustSaveDocument)
        {
            newMedianPriceDocumentFileId = await this.UpdateDocumentHistoryAsync(
                medianPrice,
                FileId.From(req.MedianPriceDocumentId!.Value),
                isReplaceDocument,
                ct);
        }

        if (medianPrice.Status == req.Status)
        {
            medianPrice.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูล",
                medianPrice.Status.ToString()));
        }
        else
        {
            UpdateStatus(medianPrice, req.Status);
        }

        await this.UpdateAndReplaceDocumentAsync(
            procurement,
            medianPrice,
            UserId.From(req.UserId),
            isReplaceDocument,
            ct);

        if (medianPrice.Status == MedianPriceStatus.WaitingCommitteeApproval)
        {
            EnsureInitialCommitteeCurrents(medianPrice);
        }

        SendNotificationWhenWaitingApproval(medianPrice, previousStatus, req);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateMedianPriceResponse(newMedianPriceDocumentFileId?.Value));
    }

    private void ValidateDocument(UpdateMedianPriceRequest req, PpMedianPrice? medianPrice)
    {
        if (req is { MedianPriceDocumentId: not null, Status: MedianPriceStatus.WaitingCommitteeApproval } &&
            (medianPrice != null && !medianPrice.IsMigration.GetValueOrDefault(false) && !medianPrice.DocumentHistories.Any()))
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private static void SendNotificationWhenWaitingApproval(PpMedianPrice medianPrice, MedianPriceStatus previousStatus, UpdateMedianPriceRequest req)
    {
        if (previousStatus != MedianPriceStatus.WaitingApproval && req.Status == MedianPriceStatus.WaitingApproval)
        {
            var approvers = medianPrice.Acceptors
                                       .Where(p => p.Type == AcceptorType.Approver)
                                       .OrderBy(a => a.Sequence)
                                       .ToList();

            var firstPending = approvers.Select(DelegatorExtensions.DelegatorToAcceptor)
                                        .FirstOrDefault(a => a is { Status: AcceptorStatus.Pending, IsCurrent: true });

            if (firstPending != null)
            {
                foreach (var targetUserId in firstPending.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        medianPrice,
                        targetUserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementMedianPrice.Name, medianPrice.ReferenceNumber));
                }
            }
        }
    }

    private async Task UpdateDocumentTemplate(
        PpMedianPrice medianPrice,
        string medianPriceDocumentTemplateCode,
        CancellationToken ct)
    {
        var documentTemplate =
            await this.dbContext.SuDocumentTemplates
                      .FirstOrDefaultAsync(
                          sdt => sdt.Code == medianPriceDocumentTemplateCode, ct);

        if (documentTemplate is null)
        {
            this.ThrowError("ไม่พบเอกสาร Median price Template ที่ระบุ", StatusCodes.Status404NotFound);
        }

        if (documentTemplate.Id == medianPrice.DocumentTemplateId)
        {
            return; // No change needed
        }

        medianPrice.SetDocumentTemplate(documentTemplate.Id);

        await this.SetDefaultDocumentTemplate(
            medianPrice,
            medianPriceDocumentTemplateCode,
            ct);
    }

    private static void UpsertBudgetAllocations(PpMedianPrice medianPrice, UpdateBudgetAllocations budgetAllocations)
    {
        _ = medianPrice.UpdateBudgetAllocations(
            budgetAllocations.Id ?? medianPrice.BudgetAllocation.Id,
            ba =>
            {
                // Update allocation properties
                ba.SetBudget(budgetAllocations.Budget)
                  .SetReferenceMedianPrice(budgetAllocations.ReferenceMedianPrice);

                var requestDetails =
                    budgetAllocations.Details
                                     .Select(d => d.ToEntity())
                                     .ToHashSet();

                // Update existing details
                _ = ba.Details
                      .Join(
                          requestDetails,
                          domainDetail => domainDetail.Id,
                          requestDetail => requestDetail.Id,
                          (domainDetail, requestDetail) =>
                          {
                              domainDetail.SetSequence(requestDetail.Sequence)
                                          .SetSource(requestDetail.Source);

                              if (requestDetail is PpMedianPriceBudgetAllocationsWithDetail requestWithDetail &&
                                  domainDetail is PpMedianPriceBudgetAllocationsWithDetail domainWithDetail)
                              {
                                  domainWithDetail.SetReferenceBudge(requestWithDetail.ReferenceBudge);
                              }

                              return domainDetail;
                          })
                      .ToHashSet();

                // Add new details
                _ = requestDetails
                    .Except(ba.Details)
                    .Map(ba.AddDetail)
                    .ToHashSet();

                // Remove details that are not in the request
                _ = ba.Details
                      .Except(requestDetails)
                      .Map(ba.RemoveDetail)
                      .ToHashSet();
            });
    }

    private static void UpsertStaff(PpMedianPrice medianPrice, UpdateStaff? staff)
    {
        if (staff is null)
        {
            return;
        }

        _ = Optional(medianPrice.StaffMember)
            .Map(ps => medianPrice.UpdateStaff(
                staff.Id ?? ps.Id,
                s =>
                {
                    // Update staff properties
                    s.SetPersonnelCompensation(staff.PersonnelCompensation)
                     .SetPersonnelCount(staff.PersonnelCount);

                    var requestDetails =
                        staff.Details
                             .Select(d => d.ToEntity())
                             .ToHashSet();

                    // Update existing details
                    _ = s.Details
                         .Join(
                             requestDetails,
                             domainDetail => domainDetail.Id,
                             requestDetail => requestDetail.Id,
                             (domainDetail, requestDetail) =>
                             {
                                 domainDetail.SetSequence(requestDetail.Sequence)
                                             .SetDescription(requestDetail.Description);

                                 if (domainDetail is PpMedianPriceStaffPersonal domainPersonal &&
                                     requestDetail is PpMedianPriceStaffPersonal requestPersonal)
                                 {
                                     domainPersonal.SetPersonalCount(requestPersonal.PersonalCount);
                                 }

                                 return domainDetail;
                             })
                         .ToHashSet();

                    // Add new details
                    _ = requestDetails
                        .Except(s.Details)
                        .Map(s.AddDetail)
                        .ToHashSet();

                    // Remove details that are not in the request
                    _ = s.Details
                         .Except(requestDetails)
                         .Map(s.RemoveDetail)
                         .ToHashSet();
                }))
            .IfNone(() => medianPrice.AddStaff(staff.CreateToEntity()));
    }

    private static void UpdateExpenseDescription(PpMedianPrice medianPrice, MedianPriceExpenseDescriptionInfo? expenseDescription)
    {
        if (expenseDescription is null)
        {
            return;
        }

        medianPrice.UpdateExpenseDescription(e =>
            e.SetHardwareCost(expenseDescription.HardwareCost)
             .SetMaterialCost(expenseDescription.MaterialCost)
             .SetOverseasTravelCost(expenseDescription.OverseasTravelCost)
             .SetOtherExpenses(expenseDescription.OtherExpenses)
             .SetSoftwareCost(expenseDescription.SoftwareCost)
             .SetSystemDevelopmentCost(expenseDescription.SystemDevelopmentCost));
    }

    private async ValueTask UpdateAndReplaceDocumentAsync(
        Procurement procurement,
        PpMedianPrice medianPrice,
        UserId creatorUserId,
        bool isReplace,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var lastedDraftDocumentHistory = medianPrice.LastedDraftDocument;

        if (lastedDraftDocumentHistory is null)
        {
            this.ThrowError("ไม่พบเอกสารร่างล่าสุดของราคากลาง");
        }

        var copiedFileId = await CopyDocument(lastedDraftDocumentHistory.FileId);
        medianPrice.AddDocumentHistory(copiedFileId);

        var replaceDto = await this.MapToReplaceDtoAsync(
            procurement,
            medianPrice,
            ct,
            creatorUserId);

        var replacedFileId = await ReplaceDocument(lastedDraftDocumentHistory.FileId, replaceDto, isReplace);
        medianPrice.AddDocumentHistory(replacedFileId, true);

        return;

        async Task<FileId> CopyDocument(FileId sourceFileId)
        {
            var fileIdResult = await documentService.CopyDocumentTemplateAsync(
                sourceFileId,
                parentDirectory: $"{DocumentTemplateGroups.Mdp}/{medianPrice.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (fileIdResult is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
            }

            return fileIdResult.Value;
        }

        async Task<FileId> ReplaceDocument(FileId sourceFileId, object replaceDto, bool isReplace)
        {
            // Get original template (with placeholders) instead of LastedDraftDocument
            var templateFileId = await this.GetDocumentTemplateAsync(medianPrice, ct);

            var fileIdResult = await documentService.CopyDocumentTemplateAsync(
                isReplace ? templateFileId : sourceFileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.Mdp}/{medianPrice.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (fileIdResult is null)
            {
                this.ThrowError("ไม่สามารถแทนที่เอกสารได้");
            }

            return fileIdResult.Value;
        }
    }

    private static void EnsureInitialCommitteeCurrents(PpMedianPrice entity)
    {
        if (entity.Status != MedianPriceStatus.WaitingCommitteeApproval)
        {
            return;
        }

        var committee = entity.Acceptors?
                              .Where(a => a.Type == AcceptorType.MedianPriceCommittee && a.IsActive && !a.IsUnableToPerformDuties && a.Status == AcceptorStatus.Pending)
                              .ToList();

        if (committee == null || committee.Count == 0)
        {
            return;
        }

        // if any committee (non-chair) already approved do not reset currents
        if (entity.Acceptors!.Any(a => a.Type == AcceptorType.MedianPriceCommittee && a.Status == AcceptorStatus.Approved))
        {
            return; // progress already started
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
            chairman.SetCurrent(true); // only chairman

            _ = SendNotificationAsync(
                entity,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementMedianPrice.Name, entity.ReferenceNumber));

            return;
        }

        foreach (var a in nonChair)
        {
            _ = SendNotificationAsync(
                entity,
                a.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementMedianPrice.Name, entity.ReferenceNumber));
            a.SetCurrent(true);
        }

        if (chairman != null)
        {
            chairman.SetCurrent(false);
        }
    }

    private static async Task SendNotificationAsync(PpMedianPrice ppMedianPrice, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(ppMedianPrice.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, ppMedianPrice.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(PpMedianPrice ppMedianPrice, CancellationToken ct)
    {
        _ = await ppMedianPrice.Assignees.Where(x => x.Type != AssigneeType.Director).Map(pa =>
                                   Notification
                                       .Crate(
                                           pa.UserId,
                                           NotificationConstant.Assignment.Title,
                                           string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PreProcurementTorDraft.Name, ppMedianPrice.ReferenceNumber),
                                           NotificationProgram.Procurement)
                                       .SetReferenceId(ppMedianPrice.Id.Value)
                                       .SetLinkUrl(
                                           string.Format(ProgramConstant.Procurement.Url, ppMedianPrice.Id),
                                           "ดูรายละเอียด"))
                               .Map(n => n.PublishAsync(ct).ToUnit())
                               .SequenceSerial();
    }
}