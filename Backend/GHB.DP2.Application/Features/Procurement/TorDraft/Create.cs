namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record TorDraftObjectDto(
    int Sequence,
    string Description);

public record TorDraftQualificationDto(
    int Sequence,
    string Description);

public record TorDraftTechnicalPeriodDto(
    int? Period,
    string? PeriodTypeCode,
    string? PeriodConditionCode,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? DeliveryConditionCode,
    DateTimeOffset? DeliveryDate,
    TorDraftTechnicalPeriodDetail[]? Details);

public record TorDraftTechnicalPeriodDetail(
    string Branch,
    string PersonalCount,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate
);

public record TorDraftTechnicalSpecificationDto(
    int Sequence,
    string? Name,
    string? Description,
    int? Quantity,
    string? UnitCode);

public record TorDraftBudgetDetailDto(
    int Sequence,
    string Department,
    string BudgetType,
    string? ProjectCode,
    string AccountNo,
    decimal Budget
);

public record TorDraftBudgetDto(
    int Sequence,
    string Description,
    decimal BudgetAmount,
    TorDraftBudgetDetailDto[]? Details
);

public record TorDraftPaymentTermDetailDto(
    int TermNumber,
    decimal Percent,
    int Period,
    string Description
);

public record TorDraftPaymentTermDto(
    string? ProRateTypeCode,
    decimal? PaymentPercent,
    string? Description,
    int? Period,
    string? PeriodTypeCode,
    int? TotalPeriod,
    string? TotalPeriodTypeCode,
    bool? IsMA,
    TorDraftPaymentTermDetailDto[]? Details
);

public record TorDraftWarrantyDto(
    bool HasWarranty,
    int? Period,
    string? PeriodTypeCode,
    string? ConditionOther
);

public record TorDraftFineRateDto(
    int Sequence,
    decimal Rate,
    string Description,
    string PeriodTypeCode,
    string? ConditionCode,
    string? ConditionOther
);

public record TorDraftAcceptorDto(
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
    string? CommitteePositionsCode,
    bool IsUnableToPerformDuties = false);

public record TorTrainingDto(
    int? TrainingCount,
    string? TrainingCountUnit,
    string? TrainingUnitId,
    TorTrainingItemDto[]? TrainingItems);

public record TorCorrectiveMaintenanceDto(
    string? CmProductName,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    int? CmCount,
    string? CmUnit,
    int? CmCompleteCount,
    string? CmCompleteUnit,
    decimal? CmFinePercent,
    decimal? CmDisruptedFinePercent,
    string? DayStart,
    string? DayEnd,
    string? StartTime,
    string? EndTime,
    string? CmFinePercentUnit);

public record TorPreventiveMaintenanceDto(
    string? PmProductName,
    int? PmCount,
    string? PmUnit,
    decimal? PmFinePct,
    decimal? PmFineAmount,
    string? Condition,
    int? DisruptedCount,
    string? DisruptedCountUnit,
    decimal? DisruptedPercent,
    decimal? DisruptedFinePercent,
    decimal? DisruptedFineAmount,
    string? PmFinePctUnit);

public record TorImpedimentDto(
    int? Sequence,
    string? Description,
    decimal? ImpedimentValue);

public record TorTrainingItemDto(
    int? Sequence,
    string? CourseName,
    int? PeriodDay,
    string? Place,
    int? TrainingCount,
    int? TotalPersonPerTime);

public record TorDraftPaymentTermPeriodsDto(
    int Sequence,
    string? Description,
    int? Quantity,
    string? PeriodTypeCode,
    int? TotalQuantity,
    string? TotalPeriodTypeCode);

public record CreateTorDraftRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    TorDraftStatus Status,
    Guid ProcurementId,
    string? TelephoneNumber,
    string TorDocumentTemplateCode,
    bool? BidGuarantee,
    bool? IsStock,
    string? Reason,
    bool? IsMA,
    DateTimeOffset? DocumentDate,
    bool? IsSaveDraft,

    // 7, 10 หลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ
    string? EvaluationCriteria,

    // 2 วัตถุประสงค์
    TorDraftObjectDto[]? Objects,

    // 3 คุณสมบัติผู้ประสงค์จะเสนอราคา
    TorDraftQualificationDto[]? Qualifications,

    // 4 ขอบเขตของงาน/รายละเอียดคุณลักษณะเฉพาะ
    TorDraftTechnicalSpecificationDto[]? TechnicalSpecifications,

    // 5 ระยะเวลาดำเนินการ
    TorDraftTechnicalPeriodDto[]? TechnicalPeriods,

    // 6 วงเงินที่จะจัดซื้อจัดจ้าง
    TorDraftBudgetDto[]? Budgets,

    // 7 เงื่อนไขการชำระเงิน
    TorDraftPaymentTermDto[]? PaymentTerms,

    // 8 การประกันคุณภาพงาน
    TorDraftWarrantyDto[]? Warranties,

    // 9. อัตราค่าปรับ
    TorDraftFineRateDto[]? FineRates,
    TorDraftAcceptorDto[]? Acceptors,
    AssigneeRequest[]? Assignees,
    bool? IsContractGuarantee,
    decimal? PercentageContract,

    // เอกสารประกอบการเสนอราคา
    string? DocumentDescription,

    // ต้องจัดส่งเอกสารคู่มือให้แก่ธนาคารภายในเวลาที่กำหนดดังต่อไปนี้
    string? ManuelDescription,

    // การบำรุงรักษา (Preventive Maintenance) (IT)
    TorPreventiveMaintenanceDto? PreventiveMaintenance,

    // การซ่อมแซมแก้ไข (Corrective Maintenance
    TorCorrectiveMaintenanceDto? CorrectiveMaintenance,

    // การฝึกอบรม (IT)
    TorTrainingDto? Training,

    TorTrainingItemDto[]? TrainingItems,

    // การกำหนดตัวถ่วง (IT)
    TorImpedimentDto[]? Impediments,

    TorDraftPaymentTermPeriodsDto[]? PaymentTermPeriods,
    bool? IsCM,
    bool? IsPM,
    bool? IsTraining,
    bool? IsImpediment
);

public class CreateTorDraftRequestValidator : AbstractValidator<CreateTorDraftRequest>
{
    public CreateTorDraftRequestValidator()
    {
        this.RuleFor(x => x.TelephoneNumber)
            .MaximumLength(20)
            .WithMessage("เบอร์โทรศัพท์ต้องไม่เกิน 20 ตัวอักษร");
        this.RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("กรุณาระบุเหตุผล");
        this.RuleForEach(x => x.Objects)
            .ChildRules(obj =>
            {
                obj.RuleFor(o => o.Description)
                   .NotEmpty()
                   .WithMessage("กรุณาระบุวัตถุประสงค์");
            });
        this.RuleForEach(x => x.Qualifications)
            .ChildRules(q =>
            {
                q.RuleFor(o => o.Description)
                 .NotEmpty()
                 .WithMessage("กรุณาระบุคุณสมบัติผู้ประสงค์จะเสนอราคา");
            });
        this.RuleForEach(x => x.Budgets)
            .ChildRules(b =>
            {
                b.RuleFor(o => o.Description)
                 .NotEmpty()
                 .WithMessage("กรุณาระบุรายละเอียดงบประมาณ");
                b.RuleFor(o => o.BudgetAmount)
                 .GreaterThan(0)
                 .WithMessage("กรุณาระบุจำนวนเงินงบประมาณมากกว่า 0");
            });
        this.RuleForEach(x => x.PaymentTerms)
            .ChildRules(p =>
            {
                p.RuleFor(o => o.PaymentPercent)
                 .GreaterThan(0)
                 .WithMessage("กรุณาระบุเปอร์เซ็นต์การชำระเงินมากกว่า 0");
            });
        this.RuleForEach(x => x.Warranties)
            .ChildRules(w =>
            {
                w.RuleFor(o => o.Period)
                 .GreaterThanOrEqualTo(0)
                 .WithMessage("กรุณาระบุระยะเวลาการรับประกัน")
                 .When(x => x.HasWarranty);

                w.RuleFor(r => r.PeriodTypeCode)
                 .NotEmpty()
                 .WithMessage("กรุณาระบุช่วงเวลา")
                 .When(x => x.HasWarranty);

                w.RuleFor(r => r.ConditionOther)
                 .NotEmpty()
                 .WithMessage("กรุณาระบุเงื่อนไขการรับประกัน")
                 .When(x => x.HasWarranty);
            });
        this.RuleForEach(x => x.FineRates)
            .ChildRules(f =>
            {
                f.RuleFor(o => o.Rate)
                 .GreaterThan(0)
                 .WithMessage("กรุณาระบุอัตราค่าปรับมากกว่า 0");
            });
    }
}

public class CreateTorDraftEndpoint : TorDraftEndpointBase<CreateTorDraftRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateTorDraftEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<CreateTorDraftEndpoint> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/TorDraft")
             .WithName("CreateTorDraft")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<CreateTorDraftRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/tordraft");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreateTorDraftRequest req,
        CancellationToken ct)
    {
        if (!req.IsSaveDraft.GetValueOrDefault(false))
        {
            var validationResult = await new CreateTorDraftRequestValidator().ValidateAsync(req, ct);

            if (!validationResult.IsValid)
            {
                this.ThrowError(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)), StatusCodes.Status400BadRequest);
            }
        }

        var procurementExisting =
            await this.dbContext.Procurements
                      .AsNoTracking()
                      .FirstOrDefaultAsync(
                          p =>
                              p.Id == ProcurementId.From(req.ProcurementId) &&
                              !p.IsDeleted,
                          ct);

        if (procurementExisting is null)
        {
            this.ThrowError(
                r => r.ProcurementId,
                $"ไม่พบการจัดซื้อจัดจ้างในระบบ",
                StatusCodes.Status404NotFound);
        }

        var torDraft = CreateTorDraftFromRequest(req, procurementExisting);

        var lastAssigneeUserId = req.Assignees?
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        if (req.Acceptors is not null)
        {
            torDraft.PpTorDraftAcceptors = await this.MapTorDraftAcceptors(req.Acceptors, req.Status, procurementExisting.DepartmentId, lastAssigneeUserId ?? UserId.From(req.UserId));
        }

        if (req.Assignees != null)
        {
            await this.UpsertAssigneeAsync(torDraft, req.Assignees, UserId.From(req.UserId), ct);
        }

        await this.AddDocumentTemplate(torDraft, req.TorDocumentTemplateCode, ct);

        if (!req.IsSaveDraft.GetValueOrDefault(false))
        {
            await this.SetDefaultDocumentTemplate(
                torDraft,
                torDraft.DocumentTemplate is not null ? torDraft.DocumentTemplate.Code : req.TorDocumentTemplateCode,
                procurementExisting.SupplyMethodCode,
                procurementExisting.HasMd,
                ct);
        }

        this.dbContext.PpTorDrafts.Add(torDraft);
        await this.dbContext.SaveChangesAsync(ct);

        // Reload entity with includes needed by MapToReplaceDto
        var torDraftReloaded = await this.GetTorDraftById(torDraft.Id, torDraft.ProcurementId, ct);
        var appoint = await this.GetAppointById(torDraft.ProcurementId, ct);

        await this.UpdateAndReplaceDocumentAsync(
            torDraftReloaded,
            torDraftReloaded.Status,
            appoint,
            isTorDraftDocumentIdReplaced: true,
            isTorDraftApprovalDocumentIdReplaced: true,
            ct);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, torDraft.Id.Value);
    }

    private async Task AddDocumentTemplate(PpTorDraft torDraft, string? torDocumentTemplateCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(torDocumentTemplateCode))
        {
            return;
        }

        var documentTemplate = await this.dbContext.SuDocumentTemplates
                                         .FirstOrDefaultAsync(x => x.Code == torDocumentTemplateCode, ct);

        if (documentTemplate is null)
        {
            this.ThrowError("ไม่พบเอกสาร TOR Template ที่ระบุ", StatusCodes.Status404NotFound);
        }

        torDraft.SetDocumentTemplate(documentTemplate.Id);
    }

    private static PpTorDraft CreateTorDraftFromRequest(CreateTorDraftRequest req, Procurement procurement)
    {
        var torDraft =
            PpTorDraft.CreateBasic(
                          req.Status,
                          procurement,
                          req.TelephoneNumber,
                          req.BidGuarantee,
                          (bool)req.IsStock,
                          req.IsMA,
                          req.IsContractGuarantee,
                          req.PercentageContract)
                      .SetReasonAndCriteria(req.Reason, req.EvaluationCriteria)
                      .SetChangeAndCancel(false, false);

        if (req.Objects is not null)
        {
            torDraft.PpTorDraftObjects = MapTorDraftObjects(req.Objects);
        }

        if (req.Qualifications is not null)
        {
            torDraft.PpTorDraftQualifications = MapTorDraftQualifications(req.Qualifications);
        }

        if (req.TechnicalSpecifications is not null)
        {
            torDraft.PpTorDraftTechnicalSpecifications = MapTorDraftTechnicalSpecifications(req.TechnicalSpecifications);
        }

        if (req.TechnicalPeriods is not null)
        {
            torDraft.PpTorDraftTechnicalPeriods = MapTorDraftTechnicalPeriods(req.TechnicalPeriods);
        }

        if (req.Budgets is not null)
        {
            torDraft.PpTorDraftBudgets = MapTorDraftBudgets(req.Budgets);
        }

        if (req.PaymentTerms is not null)
        {
            torDraft.PpTorDraftPaymentTerms = MapTorDraftPaymentTerms(req.PaymentTerms);
        }

        if (req.PaymentTermPeriods is not null && req.PaymentTermPeriods.Any())
        {
            torDraft.PpTorPaymentTermPeriods = MapTorDraftPaymentTermPeriods(req.PaymentTermPeriods);
        }

        if (req.Warranties is not null)
        {
            torDraft.PpTorDraftWarranties = MapTorDraftWarranties(req.Warranties);
        }

        if (req.FineRates is not null)
        {
            torDraft.PpTorDraftFineRates = MapTorDraftFineRates(req.FineRates);
        }

        if (req.DocumentDate is not null)
        {
            torDraft.SetDocumentDate(req.DocumentDate);
        }

        if (req.DocumentDescription is not null
            || req.ManuelDescription is not null
            || req.PreventiveMaintenance is not null
            || req.CorrectiveMaintenance is not null
            || req.Training is not null)
        {
            torDraft.PpTorTemplateComputer =
                 MapTorTemplateComputer(
                    req.DocumentDescription,
                    req.ManuelDescription,
                    req.PreventiveMaintenance,
                    req.CorrectiveMaintenance,
                    req.Training);
        }

        if (req.Impediments is not null)
        {
            torDraft.PpTorImpediments = MaTporImpediment(req.Impediments);
        }

        if (req.TrainingItems is not null)
        {
            torDraft.PpTorTrainingItems = MapTrainingItems(req.TrainingItems);
        }

        torDraft.IsCM = req.IsCM;
        torDraft.IsPM = req.IsPM;
        torDraft.IsTraining = req.IsTraining;
        torDraft.IsImpediment = req.IsImpediment;

        // Initialize IsCurrent flags for committee phase
        if (torDraft.Status == TorDraftStatus.WaitingCommitteeApproval)
        {
            SetInitialCommitteeCurrentFlags(torDraft);
        }

        return torDraft;
    }

    private static void SetInitialCommitteeCurrentFlags(PpTorDraft torDraft)
    {
        var committee = torDraft.PpTorDraftAcceptors
                                ?.Where(a => a.Type == AcceptorType.TorDraftCommittee && a.IsActive && !a.IsUnableToPerformDuties && a.Status == AcceptorStatus.Pending)
                                .ToList();

        if (committee == null || committee.Count == 0)
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
            chairman.SetCurrent(true); // only chairman

            return;
        }

        foreach (var a in nonChair)
        {
            a.SetCurrent(true);
        }

        if (chairman != null)
        {
            chairman.SetCurrent(false);
        }
    }

    private static List<PpTorDraftObject> MapTorDraftObjects(IEnumerable<TorDraftObjectDto> objects)
    {
        return
        [
            .. objects.Select(o => new PpTorDraftObject
            {
                Id = PpTorDraftObjectId.New(),
                Sequence = o.Sequence,
                Description = o.Description,
            })
        ];
    }

    private static List<PpTorDraftQualifications> MapTorDraftQualifications(IEnumerable<TorDraftQualificationDto> qualifications)
    {
        return
        [
            .. qualifications.Select(q => new PpTorDraftQualifications
            {
                Id = PpTorDraftQualificationsId.New(),
                Sequence = q.Sequence,
                Description = q.Description,
            })
        ];
    }

    private static List<PpTorDraftTechnicalSpecifications> MapTorDraftTechnicalSpecifications(IEnumerable<TorDraftTechnicalSpecificationDto> specs)
    {
        return
        [
            .. specs.Select(t => new PpTorDraftTechnicalSpecifications
            {
                Id = PpTorDraftTechnicalSpecificationsId.New(),
                Sequence = t.Sequence,
                Name = t.Name,
                Description = t.Description,
                Quantity = t.Quantity,
                UnitCode = !string.IsNullOrWhiteSpace(t.UnitCode) ? ParameterCode.From(t.UnitCode) : null,
            })
        ];
    }

    private static List<PpTorDraftTechnicalPeriod> MapTorDraftTechnicalPeriods(IEnumerable<TorDraftTechnicalPeriodDto> periods)
    {
        return
        [
            .. periods.Select(t =>
            {
                var periodId = PpTorDraftTechnicalPeriodId.New();

                return new PpTorDraftTechnicalPeriod
                {
                    Id = periodId,
                    Period = t.Period,
                    PeriodTypeCode = !string.IsNullOrWhiteSpace(t.PeriodTypeCode) ? ParameterCode.From(t.PeriodTypeCode) : null,
                    PeriodConditionCode = !string.IsNullOrWhiteSpace(t.PeriodConditionCode) ? ParameterCode.From(t.PeriodConditionCode) : null,
                    DeliveryConditionCode = !string.IsNullOrWhiteSpace(t.DeliveryConditionCode) ? ParameterCode.From(t.DeliveryConditionCode) : null,
                    DeliveryDate = t.DeliveryDate,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    PpTorDraftTechnicalPeriodDetails = t.Details?.Select(d => new PpTorDraftTechnicalPeriodDetail
                    {
                        Id = PpTorDraftTechnicalPeriodDetailId.New(),
                        PpTorDraftTechnicalPeriodId = periodId,
                        Branch = d.Branch,
                        PersonalCount = d.PersonalCount,
                        StartDate = d.StartDate,
                        EndDate = d.EndDate,
                    }).ToList()!,
                };
            })
        ];
    }

    private static List<PpTorDraftBudget> MapTorDraftBudgets(IEnumerable<TorDraftBudgetDto> budgets)
    {
        return
        [
            .. budgets.Select(b =>
            {
                var budgetId = PpTorDraftBudgetId.New();

                return new PpTorDraftBudget
                {
                    Id = budgetId,
                    Sequence = b.Sequence,
                    Description = b.Description,
                    BudgetAmount = b.BudgetAmount,
                    PpTorDraftBudgetDetails = b.Details?.Select(d => new PpTorDraftBudgetDetail
                    {
                        Id = PpTorDraftBudgetDetailId.New(),
                        PpTorDraftBudgetId = budgetId,
                        Sequence = d.Sequence,
                        Department = d.Department,
                        BudgetType = d.BudgetType,
                        ProjectCode = d.ProjectCode,
                        AccountNo = d.AccountNo,
                        Budget = d.Budget,
                    }).ToList(),
                };
            })
        ];
    }

    private static List<PpTorDraftPaymentTerm> MapTorDraftPaymentTerms(IEnumerable<TorDraftPaymentTermDto> paymentTerms)
    {
        return
        [
            .. paymentTerms.Select(pt =>
            {
                var paymentTermId = PpTorDraftPaymentTermId.New();

                return new PpTorDraftPaymentTerm
                {
                    Id = paymentTermId,
                    ProRateTypeCode = !string.IsNullOrWhiteSpace(pt.ProRateTypeCode) ? ParameterCode.From(pt.ProRateTypeCode) : null,
                    PaymentPercent = pt.PaymentPercent,
                    Description = pt.Description ?? string.Empty,
                    Period = pt.Period,
                    PeriodTypeCode = !string.IsNullOrWhiteSpace(pt.PeriodTypeCode) ? ParameterCode.From(pt.PeriodTypeCode) : null,
                    TotalPeriod = pt.TotalPeriod,
                    TotalPeriodTypeCode = !string.IsNullOrWhiteSpace(pt.TotalPeriodTypeCode) ? ParameterCode.From(pt.TotalPeriodTypeCode) : null,
                    IsMA = pt.IsMA,
                    PpTorDraftPaymentTermDetails = MapPaymentTermDetails(pt.Details, paymentTermId),
                };
            })
        ];
    }

    private static List<PpTorPaymentTermPeriod> MapTorDraftPaymentTermPeriods(IEnumerable<TorDraftPaymentTermPeriodsDto> paymentTerms)
    {
        return
        [
            .. paymentTerms.Select(pt =>
            {
                var paymentTermId = PpTorDraftPaymentTermPeriodId.New();

                return new PpTorPaymentTermPeriod
                {
                    Id = paymentTermId,
                    Sequence = pt.Sequence,
                    Description = pt.Description,
                    Quantity = pt.Quantity,
                    PeriodTypeCode = !string.IsNullOrWhiteSpace(pt.PeriodTypeCode) ? ParameterCode.From(pt.PeriodTypeCode) : null,
                    TotalQuantity = pt.TotalQuantity,
                    TotalPeriodTypeCode = !string.IsNullOrWhiteSpace(pt.TotalPeriodTypeCode) ? ParameterCode.From(pt.TotalPeriodTypeCode) : null,
                };
            })
        ];
    }

    private static List<PpTorDraftWarranty> MapTorDraftWarranties(IEnumerable<TorDraftWarrantyDto> warranties)
    {
        return
        [
            .. warranties.Select(w => new PpTorDraftWarranty
            {
                Id = PpTorDraftWarrantyId.New(),
                HasWarranty = w.HasWarranty,
                Period = w.Period,
                PeriodTypeCode = !string.IsNullOrWhiteSpace(w.PeriodTypeCode) ? ParameterCode.From(w.PeriodTypeCode) : null,
                ConditionOther = w.ConditionOther,
            })
        ];
    }

    private static List<PpTorDraftFineRate> MapTorDraftFineRates(IEnumerable<TorDraftFineRateDto> fineRates)
    {
        return
        [
            .. fineRates.Select(f => new PpTorDraftFineRate
            {
                Id = PpTorDraftFineRateId.New(),
                Sequence = f.Sequence,
                Description = f.Description,
                Rate = f.Rate,
                PeriodTypeCode = !string.IsNullOrWhiteSpace(f.PeriodTypeCode) ? ParameterCode.From(f.PeriodTypeCode) : null,
                ConditionCode = !string.IsNullOrWhiteSpace(f.ConditionCode) ? ParameterCode.From(f.ConditionCode) : null,
                ConditionOther = f.ConditionOther,
            })
        ];
    }

    private async Task<List<PpTorDraftAcceptors>> MapTorDraftAcceptors(IEnumerable<TorDraftAcceptorDto> acceptors, TorDraftStatus status, BusinessUnitId workBusinessUnitId, UserId sendToAcceptorId)
    {
        var userIds = acceptors.Select(a => UserId.From(a.UserId)).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync();

        var acceptorList =
            acceptors.Join(
                         users,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => new { AcceptorDto = a, User = u })
                     .Select(a =>
                     {
                         var acceptor = PpTorDraftAcceptors
                             .Create(
                                 new PpTorDraftAcceptors.AcceptorInfoData(
                                     a.AcceptorDto.AcceptorType,
                                     a.User.Id,
                                     a.User.EmployeeCode,
                                     a.User.FullName,
                                     a.User.Employee.ConvertPositionName(workBusinessUnitId),
                                     a.AcceptorDto.DepartmentName,
                                     a.AcceptorDto.Sequence),
                                 status)
                             .SetCommitteePositionsCode(
                                 a.AcceptorDto.CommitteePositionsCode.IsNullOrEmpty()
                                     ? null
                                     : ParameterCode.From(a.AcceptorDto.CommitteePositionsCode!)).SetIsUnableToPerformDuties(a.AcceptorDto.IsUnableToPerformDuties);

                         acceptor.SetSendToAcceptorId(sendToAcceptorId);

                         return acceptor;
                     })
                     .ToList();

        return acceptorList;
    }

    private static List<PpTorDraftPaymentTermDetail>? MapPaymentTermDetails(IEnumerable<TorDraftPaymentTermDetailDto>? details, PpTorDraftPaymentTermId paymentTermId)
    {
        return details?.Select(d => new PpTorDraftPaymentTermDetail
        {
            Id = PpTorDraftPaymentTermDetailId.New(),
            PpTorDraftPaymentTermId = paymentTermId,
            TermNumber = d.TermNumber,
            Percent = d.Percent,
            Period = d.Period,
            Description = d.Description ?? string.Empty,
        }).ToList();
    }

    private static List<PpTorTrainingItem> MapTrainingItems(IEnumerable<TorTrainingItemDto> trainingItems)
    {
        return trainingItems?
            .Select(d => new PpTorTrainingItem
            {
                Id = PpTorTrainingItemId.New(),
                CourseName = d.CourseName ?? string.Empty,
                Sequence = d.Sequence ?? 1,
                PeriodDay = d.PeriodDay,
                Place = d.Place ?? string.Empty,
                TrainingCount = d.TrainingCount,
                TotalPersonPerTime = d.TotalPersonPerTime,
            })
            .ToList()
            ?? new List<PpTorTrainingItem>();
    }

    private static List<PpTorImpediment> MaTporImpediment(IEnumerable<TorImpedimentDto> impediments)
    {
        return impediments?
            .Select(d => new PpTorImpediment
            {
                Id = PpTorImpedimentId.New(),
                Description = d.Description ?? string.Empty,
                Sequence = d.Sequence ?? 1,
                ImpedimentValue = d.ImpedimentValue,
            })
            .ToList()
            ?? new List<PpTorImpediment>();
    }

    private static PpTorTemplateComputer MapTorTemplateComputer(
        string? documentDescription,
        string? manuelDescription,
        TorPreventiveMaintenanceDto? pm,
        TorCorrectiveMaintenanceDto? cm,
        TorTrainingDto? training)
    {
        return new PpTorTemplateComputer
        {
            Id = PpPpTorTemplateComputerId.New(),
            DocumentDescription = documentDescription,
            ManuelDescription = manuelDescription,
            PreventiveMaintenance = new TorPreventiveMaintenance
            {
                PmProductName = pm?.PmProductName ?? string.Empty,
                PmCount = pm?.PmCount,
                PmUnit = !string.IsNullOrWhiteSpace(pm?.PmUnit) ? ParameterCode.From(pm.PmUnit) : null,
                PmFinePct = pm?.PmFinePct != null ? pm.PmFinePct : null,
                PmFineAmount = pm?.PmFineAmount,
                PmFinePctUnit = !string.IsNullOrWhiteSpace(pm?.PmFinePctUnit) ? ParameterCode.From(pm.PmFinePctUnit) : null,
                Condition = pm?.Condition,
                DisruptedCount = pm?.DisruptedCount,
                DisruptedCountUnit = !string.IsNullOrWhiteSpace(pm?.DisruptedCountUnit) ? ParameterCode.From(pm.DisruptedCountUnit) : null,
                DisruptedPercent = pm?.DisruptedPercent,
                DisruptedFinePercent = pm?.DisruptedFinePercent,
                DisruptedFineAmount = pm?.DisruptedFineAmount != null ? pm.DisruptedFineAmount : null,
            },
            CorrectiveMaintenance = new TorCorrectiveMaintenance
            {
                CmProductName = cm?.CmProductName ?? string.Empty,
                StartDate = cm?.StartDate,
                EndDate = cm?.EndDate,
                CmCount = cm?.CmCount,
                CmUnit = !string.IsNullOrWhiteSpace(cm?.CmUnit) ? ParameterCode.From(cm.CmUnit) : null,
                CmCompleteCount = cm?.CmCompleteCount,
                CmCompleteUnit = !string.IsNullOrWhiteSpace(cm?.CmCompleteUnit) ? ParameterCode.From(cm.CmCompleteUnit) : null,
                CmFinePercent = cm?.CmFinePercent,
                CmDisruptedFinePercent = cm?.CmDisruptedFinePercent,
                CmFinePercentUnit = !string.IsNullOrWhiteSpace(cm?.CmFinePercentUnit) ? ParameterCode.From(cm.CmFinePercentUnit) : null,
                DayStart = !string.IsNullOrWhiteSpace(cm?.DayStart) ? ParameterCode.From(cm.DayStart) : null,
                DayEnd = !string.IsNullOrWhiteSpace(cm?.DayEnd) ? ParameterCode.From(cm.DayEnd) : null,
                StartTime = cm?.StartTime != null ? cm.StartTime : string.Empty,
                EndTime = cm?.EndTime != null ? cm.EndTime : string.Empty,
            },
            Training = new TorTraining
            {
                TrainingCount = training?.TrainingCount != null ? training.TrainingCount : null,
                TrainingCountUnit = !string.IsNullOrWhiteSpace(training?.TrainingCountUnit) ? ParameterCode.From(training.TrainingCountUnit) : null,
                TrainingUnitId = !string.IsNullOrWhiteSpace(training?.TrainingUnitId) ? ParameterCode.From(training.TrainingUnitId) : null,
            },
        };
    }
}