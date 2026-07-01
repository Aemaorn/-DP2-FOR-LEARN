namespace GHB.DP2.Application.Features.Procurement.Jp005;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp005.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateJp005Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid PurchaseRequisitionId,
    PJp005Status Status,
    EvaluationDto Evaluations,
    CommitteeSectionDto ProcurementCommittees,
    CommitteeSectionDto InspectionCommittees,
    CommitteeSectionDto MaintenanceInspectionCommittee,
    CommitteeSectionDto ConstructionSupervisor,
    IEnumerable<AcceptorRequest> Acceptors,
    IEnumerable<ProcurementSuppliesDivisionDto> ProcurementSuppliesDivision,
    DateTimeOffset? DocumentDate,
    string? PrNumber,
    string? Telephone,
    string? Description,
    string? PriceReasonablenessInfo,
    decimal? MedianPriceAmount);

public class CreateJp005RequestValidator : Validator<CreateJp005Request>
{
    public CreateJp005RequestValidator()
    {
        this.RuleFor(x => x.ProcurementId)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูลจัดซื้อจัดจ้างเบื้องต้น");

        this.RuleFor(x => x.PurchaseRequisitionId)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูล จพ.004 เบื้องต้น");

        this.RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("สถานะไม่ถูกต้อง");

        this.RuleFor(x => x.Evaluations)
            .NotNull()
            .WithMessage("ข้อมูลการพิจารณาต้องไม่เป็นค่าว่าง")
            .SetValidator(new EvaluationValidator());

        this.RuleFor(x => x.ProcurementCommittees)
            .NotNull()
            .WithMessage("ข้อมูลคณะกรรมการจัดซื้อจัดจ้างต้องไม่เป็นค่าว่าง")
            .NotEmpty()
            .WithMessage("ข้อมูลคณะกรรมการจัดซื้อจัดจ้างต้องไม่เป็นค่าว่าง")
            .DependentRules(() =>
            {
                this.RuleFor(x => x.ProcurementCommittees)
                    .SetValidator(new CommitteeSectionDtoValidator("จัดซื้อจัดจ้าง"));
            });

        this.RuleFor(x => x.InspectionCommittees)
            .NotNull()
            .WithMessage("ข้อมูลคณะกรรมการตรวจรับต้องไม่เป็นค่าว่าง")
            .NotEmpty()
            .WithMessage("ข้อมูลคณะกรรมการตรวจรับต้องไม่เป็นค่าว่าง")
            .DependentRules(() =>
            {
                this.RuleFor(x => x.InspectionCommittees)
                    .SetValidator(new CommitteeSectionDtoValidator("ตรวจรับ"));
            });
    }
}

public class CreateJp005Endpoint
    : Jp005EndpointBase<CreateJp005Request, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateJp005Endpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<CreateJp005Endpoint> logger)
        : base(dbContext, operationService, commandTextService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("Procurement/JorPor005"));
        this.Post("procurement/{ProcurementId:guid}/jp005");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreateJp005Request req,
        CancellationToken ct)
    {
        var procurementExisting = await this.ValidateRequestAsync(req, ct);

        var newJp005 =
            PJp005.Create(
                      procurementExisting,
                      PpPurchaseRequisitionId.From(req.PurchaseRequisitionId),
                      req.Evaluations.EvaluationDueDate,
                      req.Evaluations.EvaluationPeriodTypeCode,
                      req.Evaluations.EvaluationPeriodConditionCode)
                  .SetEgpProjectNumber(req.Evaluations.EgpProjectNumber);

        this.dbContext.PJp005S.Add(newJp005);

        var procurementCommittees =
            await this.CreateJp005CommitteesAsync(
                newJp005.Id,
                PJp005CommitteeGroupType.ProcurementCommittee,
                [.. req.ProcurementCommittees.Committees],
                ct);

        var inspectionCommittees =
            await this.CreateJp005CommitteesAsync(
                newJp005.Id,
                PJp005CommitteeGroupType.InspectionCommittee,
                [.. req.InspectionCommittees.Committees],
                ct);

        var maintenanceInspectionCommittees =
            await this.CreateJp005CommitteesAsync(
                newJp005.Id,
                PJp005CommitteeGroupType.MaintenanceInspectionCommittee,
                [.. req.MaintenanceInspectionCommittee.Committees],
                ct);

        var constructionSupervisors =
            await this.CreateJp005CommitteesAsync(
                newJp005.Id,
                PJp005CommitteeGroupType.ConstructionSupervisor,
                [.. req.ConstructionSupervisor.Committees],
                ct);

        _ = procurementCommittees
            .Concat(inspectionCommittees)
            .Concat(maintenanceInspectionCommittees)
            .Concat(constructionSupervisors)
            .Map(newJp005.AddCommittee)
            .ToArray();

        var procurementCommitteeDuties =
            CreateJp005CommitteeDuties(
                newJp005.Id,
                PJp005CommitteeGroupType.ProcurementCommittee,
                [.. req.ProcurementCommittees.Duties]);

        var inspectionCommitteeDuties =
            CreateJp005CommitteeDuties(
                newJp005.Id,
                PJp005CommitteeGroupType.InspectionCommittee,
                [.. req.InspectionCommittees.Duties]);

        var maintenanceInspectionCommitteeDuties =
            CreateJp005CommitteeDuties(
                newJp005.Id,
                PJp005CommitteeGroupType.MaintenanceInspectionCommittee,
                [.. req.MaintenanceInspectionCommittee.Duties]);

        var constructionSupervisorDuties =
            CreateJp005CommitteeDuties(
                newJp005.Id,
                PJp005CommitteeGroupType.ConstructionSupervisor,
                [.. req.ConstructionSupervisor.Duties]);

        _ =
            procurementCommitteeDuties
                .Concat(inspectionCommitteeDuties)
                .Concat(maintenanceInspectionCommitteeDuties)
                .Concat(constructionSupervisorDuties)
                .Map(newJp005.AddCommitteeDuties)
                .ToArray();

        var jp005Acceptors =
            await this.CreateJp005AcceptorAsync(
                newJp005.Id,
                newJp005.Status,
                [.. req.Acceptors],
                procurementExisting.DepartmentId);

        jp005Acceptors.Iter(a =>
        {
            a.SetSendToAcceptorId(UserId.From(req.UserId));
            newJp005.AddAcceptor(a);
        });

        var procurementSuppliesDivisions = await this.CreateJp005PProcurementSuppliesDivision(
            newJp005.Id,
            [.. req.ProcurementSuppliesDivision]);

        procurementSuppliesDivisions.Iter(x => newJp005.AddProcurementSuppliesDivision(x));

        if (req.Status != PJp005Status.Draft)
        {
            newJp005.UpdateStatus(req.Status);
        }

        if (req.DocumentDate is not null)
        {
            newJp005.SetDocumentDate(req.DocumentDate);
        }

        var isSixtyAndMoreThanOneHundredThousand = procurementExisting.SupplyMethodCode == SupplyMethodConstant.Sixty && procurementExisting.Budget > 100000;

        if (!isSixtyAndMoreThanOneHundredThousand)
        {
            await this.SetDefaultDocumentTemplate(
                newJp005,
                procurementExisting.SupplyMethodCode,
                ct);
        }

        newJp005.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            "สร้างข้อมูลรายงานผลการพิจารณา(จพ.005)",
            newJp005.Status.ToString()));

        var purchaseRequisition = await this.dbContext.PpPurchaseRequisitions
                                            .FirstOrDefaultAsync(
                                                x => x.Id == PpPurchaseRequisitionId.From(req.PurchaseRequisitionId),
                                                ct);

        purchaseRequisition?.UpdatePriceConsiderationInfo(
            req.PrNumber,
            req.Description,
            req.Telephone,
            req.PriceReasonablenessInfo,
            req.MedianPriceAmount);

        await this.dbContext.SaveChangesAsync(ct);

        if (!isSixtyAndMoreThanOneHundredThousand)
        {
            // Reload entity with includes needed by UpdateDocumentAsync
            var jp005Reloaded = await this.dbContext.PJp005S
                .Include(j => j.DocumentHistories)
                .Include(j => j.Acceptors)
                .Include(j => j.ProcurementSuppliesDivisions)
                .Include(j => j.Procurement)
                .FirstOrDefaultAsync(j => j.Id == newJp005.Id, ct);

            if (jp005Reloaded is not null)
            {
                await this.UpdateDocumentAsync(
                    jp005Reloaded,
                    req.UserId,
                    jp005Reloaded.Procurement,
                    isReplace: true,
                    hasCreator: false,
                    hasAcceptor: false,
                    hasPublisher: false,
                    ct);

                await this.dbContext.SaveChangesAsync(ct);
            }
        }

        return TypedResults.Created(string.Empty, newJp005.Id.Value);
    }

    private async Task<Procurement> ValidateRequestAsync(
        CreateJp005Request req,
        CancellationToken ct)
    {
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

        var jp004 =
            await this.dbContext.PpPurchaseRequisitions
                      .AsNoTracking()
                      .FirstOrDefaultAsync(
                          p =>
                              p.Id == PpPurchaseRequisitionId.From(req.PurchaseRequisitionId) &&
                              !p.IsDeleted,
                          ct);

        if (jp004 is null)
        {
            this.ThrowError(
                r =>
                    r.PurchaseRequisitionId,
                $"ไม่พบ จพ.004 ในระบบ",
                StatusCodes.Status404NotFound);
        }

        if (jp004.Status != PurchaseRequisitionStatus.Approved)
        {
            this.ThrowError(
                r => r.ProcurementId,
                $"สถานะของ จพ.004 ต้องเป็น {PurchaseRequisitionStatus.Approved} เท่านั้น",
                StatusCodes.Status400BadRequest);
        }

        var jp005Existing =
            await this.dbContext.PJp005S
                      .AsNoTracking()
                      .FirstOrDefaultAsync(
                          p =>
                              p.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                              p.IsActive &&
                              !p.IsDeleted,
                          ct);

        if (jp005Existing is not null)
        {
            this.ThrowError(
                r => r.ProcurementId,
                $"มีข้อมูล จพ.005 อยู่แล้วสำหรับการจัดซื้อจัดจ้างนี้",
                StatusCodes.Status409Conflict);
        }

        var evaluationPeriodTypeCode =
            await this.dbContext.SuParameters
                      .AsNoTracking()
                      .FirstOrDefaultAsync(
                          p => p.Code == ParameterCode.From(req.Evaluations.EvaluationPeriodTypeCode),
                          ct);

        if (evaluationPeriodTypeCode is null)
        {
            this.ThrowError(
                r =>
                    r.Evaluations.EvaluationPeriodTypeCode,
                $"ไม่พบ evaluationPeriodTypeCode ในระบบ",
                StatusCodes.Status404NotFound);
        }

        var evaluationPeriodConditionCode =
            await this.dbContext.SuParameters
                      .AsNoTracking()
                      .FirstOrDefaultAsync(
                          p => p.Code == ParameterCode.From(req.Evaluations.EvaluationPeriodConditionCode),
                          ct);

        if (evaluationPeriodConditionCode is null)
        {
            this.ThrowError(
                r =>
                    r.Evaluations.EvaluationPeriodTypeCode,
                $"ไม่พบ evaluationPeriodConditionCode ในระบบ",
                StatusCodes.Status404NotFound);
        }

        return procurementExisting;
    }

    private async ValueTask<IEnumerable<PJp005ProcurementSuppliesDivision>> CreateJp005PProcurementSuppliesDivision(
        PJp005Id jp005Id,
        ProcurementSuppliesDivisionDto[] procurementSuppliesDivision)
    {
        var userIds = procurementSuppliesDivision.Select(a => UserId.From(a.UserId));

        var users = await this.dbContext.SuUsers
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(CancellationToken.None);

        this.ValidateUsers(
            users,
            [.. userIds]);

        var procurementSuppliesDivisionResult = procurementSuppliesDivision.Select(
                                                                               c =>
                                                                                   PJp005ProcurementSuppliesDivision.CreatePJp005ProcurementSuppliesDivision(
                                                                                       jp005Id,
                                                                                       UserId.From(c.UserId),
                                                                                       c.FullName,
                                                                                       c.FullPositionName,
                                                                                       c.Sequence))
                                                                           .ToList();

        return procurementSuppliesDivisionResult;
    }

    private async ValueTask<IEnumerable<PJp005Committee>> CreateJp005CommitteesAsync(
        PJp005Id jp005Id,
        PJp005CommitteeGroupType committeeGroupType,
        CommitteeDto[] committees,
        CancellationToken ct)
    {
        var committeeUserIds =
            committees.Select(a => UserId.From(a.UserId));

        var users =
            await this.dbContext.SuUsers
                      .Where(u => committeeUserIds.Contains(u.Id))
                      .ToArrayAsync(CancellationToken.None);

        this.ValidateUsers(
            users,
            [.. committeeUserIds]);

        var committeesResult = new List<PJp005Committee>();

        var pobParameters = await this.dbContext
                                      .SuParameters
                                      .Where(w => w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoard) ||
                                                  w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoardProcurement) ||
                                                  w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoardMA) ||
                                                  w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoardSupervisor) ||
                                                  (w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoardInSpection) &&
                                                   w.ParentId == null))
                                      .ToArrayAsync(ct);

        if (committeeGroupType == PJp005CommitteeGroupType.ProcurementCommittee)
        {
            committeesResult =
            [
                .. committees
                    .Select(c =>
                        PJp005Committee.CreateProcurementCommittee(
                            jp005Id,
                            UserId.From(c.UserId),
                            c.FullName,
                            c.FullPositionName,
                            ParameterCode.From(c.CommitteePositionsCode),
                            this.FindCommitteeNameByCode(pobParameters, c.CommitteePositionsCode),
                            c.Sequence))
            ];
        }

        if (committeeGroupType == PJp005CommitteeGroupType.InspectionCommittee)
        {
            committeesResult =
            [
                .. committees
                    .Select(c =>
                        PJp005Committee.CreateInspectionCommittee(
                            jp005Id,
                            UserId.From(c.UserId),
                            c.FullName,
                            c.FullPositionName,
                            ParameterCode.From(c.CommitteePositionsCode),
                            this.FindCommitteeNameByCode(pobParameters, c.CommitteePositionsCode),
                            c.Sequence))
            ];
        }

        if (committeeGroupType == PJp005CommitteeGroupType.MaintenanceInspectionCommittee)
        {
            committeesResult =
            [
                .. committees
                    .Select(c =>
                        PJp005Committee.CreateMaintenanceInspectionCommittee(
                            jp005Id,
                            UserId.From(c.UserId),
                            c.FullName,
                            c.FullPositionName,
                            ParameterCode.From(c.CommitteePositionsCode),
                            this.FindCommitteeNameByCode(pobParameters, c.CommitteePositionsCode),
                            c.Sequence))
            ];
        }

        if (committeeGroupType == PJp005CommitteeGroupType.ConstructionSupervisor)
        {
            committeesResult =
            [
                .. committees
                    .Select(c =>
                        PJp005Committee.CreateConstructionSupervisor(
                            jp005Id,
                            UserId.From(c.UserId),
                            c.FullName,
                            c.FullPositionName,
                            ParameterCode.From(c.CommitteePositionsCode),
                            this.FindCommitteeNameByCode(pobParameters, c.CommitteePositionsCode),
                            c.Sequence))
            ];
        }

        return committeesResult;
    }

    private static IEnumerable<PJp005CommitteeDuties> CreateJp005CommitteeDuties(
        PJp005Id jp005Id,
        PJp005CommitteeGroupType committeeGroupType,
        DutyDto[] duties)
    {
        var dutiesResult =
            duties
                .Select(duty => PJp005CommitteeDuties.CreateCommitteeDuty(
                    jp005Id,
                    duty.Description,
                    duty.Sequence,
                    committeeGroupType))
                .ToList();

        return dutiesResult;
    }

    private async ValueTask<IEnumerable<PJp005Acceptors>> CreateJp005AcceptorAsync(
        PJp005Id jp005Id,
        PJp005Status status,
        AcceptorRequest[] acceptors,
        BusinessUnitId workBusinessUnitId)
    {
        var acceptorUserIds =
            acceptors.Select(a => UserId.From(a.UserId));

        var users =
            await this.dbContext.SuUsers
                      .Include(e => e.Employee)
                      .ThenInclude(e => e.View)
                      .Where(u => acceptorUserIds.Contains(u.Id))
                      .ToArrayAsync(CancellationToken.None);

        this.ValidateUsers(
            users,
            [.. acceptorUserIds]);

        var acceptorUsers = acceptors.Join(
            users,
            a => a.UserId,
            u => u.Id.Value,
            (a, u) => PJp005Acceptors.Create(
                jp005Id,
                a.AcceptorType,
                u,
                a.Sequence,
                status,
                workBusinessUnitId));

        return acceptorUsers;
    }
}