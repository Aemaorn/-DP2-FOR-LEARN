namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.Abstract;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.DTO;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreatePlanAnnouncementRequest(
    string? GroupEgpNumber,
    int Year,
    string SupplyMethodCode,
    string? Remark,
    string? AnnouncementTitle,
    string? Telephone,
    DateTimeOffset? AnnouncementDate,
    DateTimeOffset? DocumentDate,
    PlanSelectedRequest[] PlanSelected,
    AttachmentsDto[] Attachments,
    AssigneeRequest[] Assignees,
    PlanAnnouncementStatus Status = PlanAnnouncementStatus.Draft);

public class CreatePlanAnnouncementRequestValidator : Validator<CreatePlanAnnouncementRequest>
{
    public CreatePlanAnnouncementRequestValidator()
    {
        this.RuleFor(r => r.Year)
            .GreaterThan(0)
            .WithMessage("ปีต้องมากกว่าศูนย์");

        this.RuleFor(r => r.SupplyMethodCode)
            .NotEmpty()
            .WithMessage("กรุณาระบุวิธีการจัดหา");

        this.RuleFor(p => p.Telephone)
            .MaximumLength(20)
            .WithMessage("รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง")
            .When(p => !string.IsNullOrEmpty(p.Telephone));

        this.RuleForEach(r => r.PlanSelected)
            .ChildRules(pp =>
            {
                pp.RuleFor(r => r.PlanId)
                  .NotEmpty()
                  .WithMessage("Selected PlanId is required");
            });

        this.RuleForEach(x => x.Attachments)
            .ChildRules(attachment =>
            {
                attachment.RuleFor(a => a.DocumentTypeCode)
                          .NotEmpty()
                          .WithMessage("Document type code is required.");

                attachment.RuleForEach(a => a.FileAttachments)
                          .ChildRules(file =>
                          {
                              file.RuleFor(a => a.FileId)
                                  .NotEmpty()
                                  .WithMessage("File ID is required.");

                              file.RuleFor(a => a.FileName)
                                  .NotEmpty()
                                  .WithMessage("File name is required.");

                              file.RuleFor(a => a.IsPublic)
                                  .NotNull()
                                  .WithMessage("IsPublic must be specified.");
                          });
            });

        this.RuleFor(r => r.Status)
            .IsInEnum()
            .WithMessage("สถานะไม่ถูกต้อง");
    }
}

public class CreatePlanAnnouncement : PlanAnnouncementEndpointBase<CreatePlanAnnouncementRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreatePlanAnnouncement(
        Dp2DbContext dbContext,
        ILogger<CreatePlanAnnouncement> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("CreatePlanAnnouncement")
             .Accepts<CreatePlanAnnouncementRequest>("application/json"));
        this.Post("plan/announcement");
        this.AuditLog("ขออนุมัติเผยแพร่จัดซื้อจัดจ้าง", "สร้างแผนจัดซื้อจัดจ้าง");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreatePlanAnnouncementRequest req, CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var planAnnouncementNumber = await this.GeneratePlanAnnouncementNumberAsync(req.Year, req.SupplyMethodCode, ct);
        var announcement = CreatePlanAnnouncementInstance(req, planAnnouncementNumber);

        if (req.PlanSelected.Length > 0)
        {
            await this.AddPlanAnnouncementSelected(announcement, req.PlanSelected, ct);
        }

        await this.AddAssigneeAsync(announcement, req.Assignees, ct);
        await this.AddAttachmentsDocument(announcement, req.Attachments, ct);
        await this.SetDefaultDocumentTemplate(announcement, ct);

        this.dbContext.PlanAnnouncements.Add(announcement);
        await this.dbContext.SaveChangesAsync(ct);

        var savedAnnouncement =
            await this.dbContext.PlanAnnouncements
                      .Include(p => p.AnnouncementSelectedInformations)
                      .ThenInclude(s => s.Plan)
                      .Include(p => p.Assignees)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .ThenInclude(e => e.View)
                      .Include(p => p.Assignees)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .ThenInclude(e => e.Positions)
                      .ThenInclude(p => p.BusinessUnit)
                      .AsSplitQuery()
                      .SingleAsync(p => p.Id == announcement.Id, ct);

        await this.UpdateAndReplaceDocumentTemplate(savedAnnouncement, ct);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, announcement.Id.Value);
    }

    private async Task ValidateRequestAsync(CreatePlanAnnouncementRequest req, CancellationToken ct)
    {
        if (req.Status == PlanAnnouncementStatus.WaitingAssign && !req.Assignees.Any(w => w.AssigneeType == AssigneeType.Assignee))
        {
            this.ThrowError(
                r => r.Assignees,
                $"Must be Assignees type in List.",
                StatusCodes.Status409Conflict);
        }

        var supplyMethod = await this.dbContext.SuParameters
                                     .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodCode), ct);

        if (supplyMethod is null)
        {
            this.ThrowError(
                r => r.SupplyMethodCode,
                $"Supply method with code {req.SupplyMethodCode} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    private async Task<PlanAnnouncementNumber> GeneratePlanAnnouncementNumberAsync(int year, string supplyMethodCode, CancellationToken ct)
    {
        var latestAnnouncement = await this.dbContext.PlanAnnouncements
                                           .Where(p => p.Year == year && p.SupplyMethodCode == ParameterCode.From(supplyMethodCode))
                                           .OrderByDescending(p => p.PlanAnnouncementNumber)
                                           .FirstOrDefaultAsync(ct);

        return latestAnnouncement is null
            ? PlanAnnouncementNumber.New(year)
            : latestAnnouncement.PlanAnnouncementNumber.Next();
    }

    private static Domain.Plan.PlanAnnouncement CreatePlanAnnouncementInstance(
        CreatePlanAnnouncementRequest request,
        PlanAnnouncementNumber planAnnouncementNumber)
    {
        var announcement = Domain.Plan.PlanAnnouncement
                                 .Create(
                                     planAnnouncementNumber,
                                     request.GroupEgpNumber,
                                     request.Year,
                                     ParameterCode.From(request.SupplyMethodCode))
                                 .SetAnnouncementTitle(request.AnnouncementTitle)
                                 .SetAnnouncementDate(request.AnnouncementDate)
                                 .SetTelephone(request.Telephone)
                                 .SetStatus(request.Status)
                                 .SetRemark(request.Remark);

        if (request.DocumentDate is not null)
        {
            announcement.SetDocumentDate(request.DocumentDate);
        }

        return announcement;
    }

    private async Task AddPlanAnnouncementSelected(PlanAnnouncement planAnnouncement, PlanSelectedRequest[] selectedList, CancellationToken ct)
    {
        var planWithEgp = selectedList
                          .Select(r =>
                          {
                              var planSelected = Domain.Plan.PlanAnnouncementSelected.Create(
                                  PlanId.From(r.PlanId),
                                  planAnnouncement.Id);

                              planAnnouncement.AddPlanAnnouncementSelected(planSelected);

                              return new { planSelected.PlanId, r.EgpNumber };
                          }).ToList();

        foreach (var item in planWithEgp)
        {
            var data = await this.dbContext.Plans
                                 .SingleOrDefaultAsync(w => w.Id == item.PlanId, ct);

            if (data is not null)
            {
                data.SetEgpNumber(item.EgpNumber);
                this.dbContext.Plans.Update(data);
            }
        }
    }

    private async Task AddAttachmentsDocument(
        PlanAnnouncement planAnnouncement,
        AttachmentsDto[] attachments,
        CancellationToken ct)
    {
        await this.ValidateDocumentTypes(attachments, ct);

        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => (
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic)))
                       .ToArray();

        _ = fileList
            .Map(a => Domain.Plan.PlanAnnouncementAttachments.Create(
                ParameterCode.From(a.DocumentTypeCode),
                FileId.From(a.FileId),
                a.FileName,
                a.Sequence,
                a.IsPublic))
            .Iter(a => planAnnouncement.AddAttachment(a));
    }

    private async Task ValidateDocumentTypes(
        AttachmentsDto[] attachments,
        CancellationToken ct)
    {
        var documentTypeCodes = attachments.Select(a => ParameterCode.From(a.DocumentTypeCode)).ToArray();

        var documentTypes = await this.dbContext.SuParameters
                                      .Where(p => documentTypeCodes.Contains(p.Code))
                                      .ToArrayAsync(ct);

        var foundDocumentTypeCodes = documentTypes.Select(p => p.Code).ToArray();

        var missingDocumentTypeCodes = documentTypeCodes.Except(foundDocumentTypeCodes).ToArray();

        if (missingDocumentTypeCodes.Length > 0)
        {
            this.ThrowError(
                r => r.Attachments,
                $"Document types with codes {string.Join(", ", missingDocumentTypeCodes)} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    private async Task AddAssigneeAsync(
        PlanAnnouncement planAnnouncement,
        AssigneeRequest[] requestsAssignee,
        CancellationToken ct)
    {
        var assigneeIds = requestsAssignee.Select(s => UserId.From(s.UserId))
                                          .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => assigneeIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        this.ValidateUsers(userData, assigneeIds);

        requestsAssignee
            .Join(
                userData,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) => Domain.Plan.PlanAnnouncementAssignee.Create(a.AssigneeType, u, a.Sequence))
            .Iter(s => planAnnouncement.AddAssignee(s));
    }

    private void ValidateUsers(SuUser[] users, UserId[] assignUserIds)
    {
        var foundUserIds = users.Select(u => u.Id).ToArray();

        var missingAssigneeUserIds = assignUserIds.Except(foundUserIds).ToArray();

        if (missingAssigneeUserIds.Any())
        {
            this.ThrowError(
                r => r.Assignees,
                $"Users with IDs {string.Join(", ", missingAssigneeUserIds)} not found.",
                StatusCodes.Status404NotFound);
        }
    }
}