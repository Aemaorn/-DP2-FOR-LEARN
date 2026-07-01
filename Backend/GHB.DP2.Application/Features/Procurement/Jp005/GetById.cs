namespace GHB.DP2.Application.Features.Procurement.Jp005;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp005.Abstract;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record Jp005DocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public class GetById
{
    public record GetJp005ByIdRequest(
        [property: FromClaim(JwtRegisteredClaimNames.Sub)]
        Guid UserId,
        Guid ProcurementId,
        Guid? Id = null);

    public record GetJp005ByIdResponse(
        [property: Description("รหัสการจัดซื้อจัดจ้าง")]
        ProcurementId ProcurementId,
        [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
        ProcurementDto Procurement,
        [property: Description("รหัส จพ.005")] PJp005Id? Id,
        [property: Description("เลขที่ จพ.005")]
        PJp005Number? PJp005Number,
        [property: Description("ข้อมูลใบขอซื้อ จพ.004")]
        Jp004Response PurchaseRequisition,
        [property: Description("ข้อมูล จพ.005")]
        Jp005Response? Jp005,
        [property: Description("สถานะ จพ.005")]
        PJp005Status Status,
        string? TorTemplateCode,
        [property: Description("เลขที่คำสั่ง จพ.")]
        string? JorPorNumber,
        [property: Description("มีสิทธิ์แก้ไข")]
        bool HasEditPermission = false,
        [property: Description("วันที่เอกสาร")]
        DateTimeOffset? DocumentDate = null);

    public record Jp005Response(
        [property: Description("ระยะเวลาในการพิจารณาผลการเสนอราคา")]
        int EvaluationDueDate,
        [property: Description("รหัสประเภทระยะเวลา")]
        string EvaluationPeriodTypeCode,
        [property: Description("รหัสเงื่อนไขระยะเวลา")]
        string EvaluationPeriodConditionCode,
        [property: Description("เลขโครงการ eGP")]
        string? EgpProjectNumber,
        [property: Description("รหัสเอกสารอนุมัติ จพ.005")]
        Guid? Jp005ApprovalDocumentId,
        [property: Description("รหัสเอกสารอนุมัติ จพ.005 เปลี่ยนแปลง")]
        bool? IsJp005ApprovalDocumentIdReplaced,
        [property: Description("รหัสเอกสารคำสั่ง จพ.005")]
        Guid? Jp005CommandDocumentId,
        [property: Description("รหัสเอกสารคำสั่ง จพ.005 เปลี่ยนแปลง")]
        bool? IsJp005CommandDocumentIdReplaced,
        [property: Description("คณะกรรมการจัดซื้อจัดจ้าง")]
        CommitteeSectionDto ProcurementCommittees,
        [property: Description("คณะกรรมการตรวจรับ")]
        CommitteeSectionDto InspectionCommittees,
        bool IsHasMaintenanceInspectionCommittee,
        CommitteeSectionDto MaintenanceInspectionCommittee,
        bool IsConstructionSupervisor,
        CommitteeSectionDto ConstructionSupervisor,
        [property: Description("ผู้อนุมัติ")] IEnumerable<AcceptorResponse> Acceptors,
        List<ProcurementSuppliesDivisionDto> ProcurementSuppliesDivision,
        [property: Description("ประวัติเวอร์ชันเอกสารอนุมัติ")]
        Jp005DocumentVersionResponse[] ApprovalDocumentVersions,
        [property: Description("ประวัติเวอร์ชันเอกสารคำสั่ง")]
        Jp005DocumentVersionResponse[] CommandDocumentVersions)
    {
        public static Jp005Response CreateDefault(
            IEnumerable<AcceptorResponse> acceptors,
            CommitteeSectionDto procurementCommitteeSection,
            CommitteeSectionDto inSpectCommitteeSection,
            CommitteeSectionDto maintenanceInspectionCommittee,
            CommitteeSectionDto constructionSupervisor,
            List<ProcurementSuppliesDivisionDto> procurementSuppliesDivision)
        {
            return
                new Jp005Response(
                    0,
                    string.Empty,
                    string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null,
                    procurementCommitteeSection,
                    inSpectCommitteeSection,
                    maintenanceInspectionCommittee.Committees.Any(),
                    maintenanceInspectionCommittee,
                    constructionSupervisor.Committees.Any(),
                    constructionSupervisor,
                    acceptors,
                    procurementSuppliesDivision,
                    [],
                    []);
        }
    }

    public record Jp004Response(
        [property: Description("รหัสใบขอซื้อ")]
        PpPurchaseRequisitionId PurchaseRequisitionId,
        [property: Description("ข้อมูลใบขอซื้อ")]
        GetPurchaseRequisition Requisition,
        [property: Description("ข้อมูลงบประมาณ")]
        IEnumerable<GetPurchaseRequisitionBudget> Budgets,
        [property: Description("ข้อมูลการรับประกัน")]
        IEnumerable<GetPurchaseRequisitionWarranty> Warranties,
        [property: Description("คณะกรรมการ")] IEnumerable<GetPurchaseRequisitionCommittee> Committees,
        [property: Description("ข่ายงาน")] IEnumerable<GetScopeOfWork> ScopeOfWorks,
        [property: Description("ผู้ดำเนินการ")]
        IEnumerable<Jp004Operator> Operators,
        [property: Description("เป็นคณะกรรมการจัดซื้อจัดจ้าง")]
        bool IsProcurementCommittee,
        [property: Description("เป็นคณะกรรมการจัดซื้อจัดจ้าง")]
        bool IsInspectCommittee);

    public record GetPurchaseRequisition(
        [property: Description("เลขที่ใบขอซื้อ")]
        string? PurchaseRequisitionNumber,
        [property: Description("เลข eGP")] string? EgpNumber,
        [property: Description("เลขที่ PR")] string? PrNumber,
        [property: Description("รายละเอียด")] string? Description,
        [property: Description("ข้อมูลความสมเหตุสมผลของราคา")]
        string? PriceReasonablenessInfo,
        [property: Description("ราคากลาง")] decimal? MedianPriceAmount,
        [property: Description("รหัสเกณฑ์การประเมิน")]
        string? EvaluationCriteriaCode,
        [property: Description("ระยะเวลาการส่งมอบ")]
        int? DeliveryPeriod,
        [property: Description("รหัสประเภทระยะเวลาส่งมอบ")]
        string? DeliveryPeriodTypeCode,
        [property: Description("รหัสเงื่อนไขการส่งมอบ")]
        string? DeliveryConditionCode,
        [property: Description("มีอัตราค่าปรับ")]
        bool HasFineRate,
        [property: Description("มีการรับประกัน")]
        bool HasWarranty,
        [property: Description("ระยะเวลารับประกัน")]
        int? WarrantyPeriod,
        [property: Description("รหัสประเภทระยะเวลารับประกัน")]
        string? WarrantyPeriodCode,
        [property: Description("รหัสเงื่อนไขการรับประกัน")]
        string? WarrantyConditionCode,
        [property: Description("มีหลักประกันสัญญา")]
        bool HasContractGuarantee,
        [property: Description("มีคณะกรรมการตรวจรับ")]
        bool HasInspectionCommittee,
        [property: Description("มีผู้ควบคุมงานก่อสร้าง")]
        bool HasConstructionSupervisor,
        string? Telephone);

    public record GetPurchaseRequisitionBudget(
        [property: Description("รหัสงบประมาณ")]
        Guid? Id,
        [property: Description("รายละเอียด")] string Description,
        [property: Description("จำนวนเงินงบประมาณ")]
        decimal BudgetAmount,
        [property: Description("รายละเอียดงบประมาณ")]
        IEnumerable<GetPurchaseRequisitionBudgetDetail> Details,
        [property: Description("ลำดับ")] int Sequence);

    public record GetPurchaseRequisitionBudgetDetail(
        [property: Description("รหัสรายละเอียดงบประมาณ")]
        Guid? Id,
        [property: Description("ลำดับ")] int Sequence,
        [property: Description("หน่วยงาน")] string Department,
        [property: Description("ประเภทงบประมาณ")]
        string BudgetType,
        [property: Description("รหัสโครงการ")] string? ProjectCode,
        [property: Description("เลขที่บัญชี")] string AccountNo,
        [property: Description("จำนวนเงินงบประมาณ")]
        decimal Budget);

    public record GetPurchaseRequisitionWarranty(
        [property: Description("รหัสการรับประกัน")]
        Guid? Id,
        [property: Description("มีการรับประกัน")]
        bool HasWarranty,
        [property: Description("ระยะเวลา")] int Period,
        [property: Description("รหัสประเภทระยะเวลา")]
        string? PeriodTypeCode,
        [property: Description("เงื่อนไขอื่น")]
        string? ConditionOther);

    public record GetPurchaseRequisitionCommittee(
        [property: Description("รหัสคณะกรรมการ")]
        Guid? Id,
        [property: Description("ประเภทกลุ่ม")] GroupType GroupType,
        [property: Description("รหัสผู้ใช้งาน")]
        Guid SuUserId,
        [property: Description("ชื่อ-สกุล")] string FullName,
        [property: Description("ชื่อตำแหน่ง")] string PositionName,
        [property: Description("รหัสตำแหน่งคณะกรรมการ")]
        string CommitteePositionsCode,
        [property: Description("ชื่อตำแหน่งคณะกรรมการ")]
        string CommitteePositionsName,
        [property: Description("ลำดับ")] int Sequence);

    public record GetScopeOfWork(
        [property: Description("รหัสข่ายงาน")] Guid Id,
        [property: Description("ลำดับ")] int Sequence,
        [property: Description("ชื่อข่ายงาน")] string Name,
        [property: Description("รายละเอียด")] string Description,
        [property: Description("จำนวน")] int Quantity,
        [property: Description("รหัสหน่วย")] string UnitCode);

    public enum Jp004OperatorType
    {
        /// <summary>
        /// คณะกรรมการจัดซื้อจัดจ้าง (จพ.004)
        /// </summary>
        ProcurementCommittee,

        /// <summary>
        /// ผู้ที่ได้รับมอบหมาย (จพ.004)
        /// </summary>
        Assignee,
    }

    public record Jp004Operator(
        [property: Description("รหัสผู้ใช้งาน")]
        UserId UserId,
        [property: Description("ประเภทผู้ดำเนินการ")]
        Jp004OperatorType OperatorType,
        int Sequence);

    public class GetJp005ById
        : Jp005EndpointBase<GetJp005ByIdRequest, Results<Ok<GetJp005ByIdResponse>, NotFound<string>>>
    {
        private readonly Dp2DbContext dbContext;

        public GetJp005ById(
            Dp2DbContext dbContext,
            IOperationService operationService,
            ICommandTextService commandTextService,
            ILogger<GetJp005ById> logger)
            : base(dbContext, operationService, commandTextService, logger)
        {
            this.dbContext = dbContext;
        }

        public override void Configure()
        {
            this.Get("procurement/{ProcurementId:guid}/jp005/{Id:guid?}");
            this.Description(b => b
                                  .WithTags("Procurement/JorPor005")
                                  .Produces<GetJp005ByIdResponse>()
                                  .Produces<string>(StatusCodes.Status404NotFound));
        }

        protected override async ValueTask<Results<Ok<GetJp005ByIdResponse>, NotFound<string>>> HandleRequestAsync(
            GetJp005ByIdRequest req,
            CancellationToken ct)
        {
            var jp004Existing =
                await this.dbContext.PpPurchaseRequisitions
                          .Include(r => r.Budgets)
                          .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                          .Include(r => r.Warranties)
                          .Include(r => r.PaymentTerms)
                          .Include(r => r.FineRates)
                          .Include(r => r.Committees)
                          .ThenInclude(ppPurchaseRequisitionCommittee => ppPurchaseRequisitionCommittee.User)
                          .ThenInclude(suUser => suUser.Employee)
                          .ThenInclude(rawEmployee => rawEmployee.View)
                          .Include(r => r.Acceptors)
                          .ThenInclude(p => p.User)
                          .ThenInclude(p => p.Employee)
                          .Include(r => r.Assignees)
                          .ThenInclude(p => p.User)
                          .ThenInclude(p => p.Employee)
                          .Include(ppPurchaseRequisition => ppPurchaseRequisition.TechnicalSpecifications)
                          .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                          .ThenInclude(procurement => procurement.Department)
                          .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                          .ThenInclude(procurement => procurement.SupplyMethod)
                          .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                          .ThenInclude(procurement => procurement.SupplyMethodType)
                          .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                          .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                          .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                          .ThenInclude(procurement => procurement.Plan)
                          .AsNoTracking()
                          .AsSplitQuery()
                          .FirstOrDefaultAsync(
                              r => r.ProcurementId == ProcurementId.From(req.ProcurementId),
                              ct);

            if (jp004Existing == null)
            {
                return TypedResults.NotFound("ไม่พบข้อมูล จพ.004");
            }

            var purchaseRequisition = this.GetPurchaseRequisitionResponse(jp004Existing);

            var torData = await this.dbContext.PpTorDrafts.Include(ppTorDraft => ppTorDraft.DocumentTemplate)
                                    .FirstOrDefaultAsync(t => t.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

            if (req.Id is null)
            {
                var (procurementDuties, inspectDuties, maintenanceInspection, constructionSupervisor) = await this.GetDefaultCommitteeAsync(jp004Existing, [.. purchaseRequisition.Committees], ct);

                var jp005Response = Jp005Response.CreateDefault([], procurementDuties, inspectDuties, maintenanceInspection, constructionSupervisor, []);

                var initJp005 =
                    new GetJp005ByIdResponse(
                        ProcurementId.From(req.ProcurementId),
                        ProcurementDto.Map(jp004Existing.Procurement),
                        null,
                        null,
                        purchaseRequisition,
                        jp005Response,
                        PJp005Status.Draft,
                        torData?.DocumentTemplate?.Code,
                        null,
                        purchaseRequisition.Operators.Any(s => (Guid)s.UserId == req.UserId));

                return TypedResults.Ok(initJp005);
            }

            var procurement = await this.GetProcurementById(
                ProcurementId.From(req.ProcurementId),
                ct);

            var jp005Existing = this.GetJp005ById(procurement.Jp005, PJp005Id.From(req.Id.Value), ProcurementId.From(req.ProcurementId));

            var inProcurementSuppliesDivision = jp005Existing.ProcurementSuppliesDivisions.Any(s => s.SuUserId.Value == req.UserId);

            var hasEditPermission = purchaseRequisition.Operators
                                                       .Any(s => (Guid)s.UserId == req.UserId) || inProcurementSuppliesDivision;

            var result =
                new GetJp005ByIdResponse(
                    ProcurementId.From(req.ProcurementId),
                    ProcurementDto.Map(jp005Existing.Procurement),
                    PJp005Id.From(req.Id.Value),
                    jp005Existing.PJp005Number,
                    purchaseRequisition,
                    this.GetJp005Response(jp005Existing),
                    jp005Existing.Status,
                    torData?.DocumentTemplate?.Code,
                    jp005Existing.JorPorNumber,
                    hasEditPermission,
                    jp005Existing.DocumentDate);

            return TypedResults.Ok(result);
        }

        private async Task<(CommitteeSectionDto ProcurementDuties, CommitteeSectionDto InspectDuties, CommitteeSectionDto maintenanceInspection, CommitteeSectionDto constructionSupervisor)> GetDefaultCommitteeAsync(
            PpPurchaseRequisition jp004,
            GetPurchaseRequisitionCommittee[]? committee,
            CancellationToken ct)
        {
            var (ddpGroup, ddiGroup, maGroup, supGroup) = jp004.Procurement.SupplyMethodCode == SupplyMethodConstant.Sixty
                ? (GroupCode.From("DDP60"), GroupCode.From("DDI60"), GroupCode.From("PosBoardMA"), GroupCode.From("PosBoardSup"))
                : (GroupCode.From("DDP80"), GroupCode.From("DDI80"), GroupCode.From("PosBoardMA"), GroupCode.From("PosBoardSup"));

            var procurementDuties =
                await this.dbContext
                          .SuParameters
                          .Where(w => w.IsActive && w.GroupCode == ddpGroup)
                          .OrderBy(x => x.Sequence)
                          .Select(s => new DutyDto(null, s.Label, s.Sequence))
                          .ToListAsync(ct);

            var inspectDuties =
                await this.dbContext
                          .SuParameters
                          .Where(w => w.IsActive && w.GroupCode == ddiGroup)
                          .OrderBy(x => x.Sequence)
                          .Select(s => new DutyDto(null, s.Label, s.Sequence))
                          .ToListAsync(ct);

            var maintenanceDuties =
                await this.dbContext
                          .SuParameters
                          .Where(w => w.IsActive && w.GroupCode == maGroup)
                          .OrderBy(x => x.Sequence)
                          .Select(s => new DutyDto(null, s.Label, s.Sequence))
                          .ToListAsync(ct);

            var constructionSupervisorDuties =
                await this.dbContext
                          .SuParameters
                          .Where(w => w.IsActive && w.GroupCode == supGroup)
                          .OrderBy(x => x.Sequence)
                          .Select(s => new DutyDto(null, s.Label, s.Sequence))
                          .ToListAsync(ct);

            var procurementCommittee = committee?.Where(c => c.GroupType == GroupType.ProcurementCommittee)
                                                .Select(s => new CommitteeDto(null, s.SuUserId, s.FullName, s.PositionName, s.CommitteePositionsCode, s.Sequence))
                                                .OrderBy(s => s.Sequence)
                                                .ToArray() ?? [];
            var inspectCommittee = committee?.Where(c => c.GroupType == GroupType.InspectionCommittee)
                                            .Select(s => new CommitteeDto(null, s.SuUserId, s.FullName, s.PositionName, s.CommitteePositionsCode, s.Sequence))
                                            .OrderBy(s => s.Sequence)
                                            .ToArray() ?? [];

            var maintenanceInspectionCommittee = committee?.Where(c => c.GroupType == GroupType.MaintenanceInspectionCommittee)
                                                          .Select(s => new CommitteeDto(null, s.SuUserId, s.FullName, s.PositionName, s.CommitteePositionsCode, s.Sequence))
                                                          .OrderBy(s => s.Sequence)
                                                          .ToArray() ?? [];

            var constructionSupervisor = committee?.Where(c => c.GroupType == GroupType.ConstructionSupervisor)
                                                  .Select(s => new CommitteeDto(null, s.SuUserId, s.FullName, s.PositionName, s.CommitteePositionsCode, s.Sequence))
                                                  .OrderBy(s => s.Sequence)
                                                  .ToArray() ?? [];

            return (new CommitteeSectionDto(procurementCommittee, procurementDuties), new CommitteeSectionDto(inspectCommittee, inspectDuties),
                new CommitteeSectionDto(maintenanceInspectionCommittee, []), new CommitteeSectionDto(constructionSupervisor, []));
        }
    }
}