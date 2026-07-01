namespace GHB.DP2.Application.Features.Operations;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.Operations.Dto;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

public interface IOperationService
{
    /// <summary>
    /// Retrieves the default acceptor based on the user's primary business unit and section approvers.
    /// </summary>
    /// <param name="processType">The type of process for which approvers are needed.</param>
    /// <param name="userId">The ID of the user initiating the process.</param>
    /// <param name="budget">The budget amount for the operation.</param>
    /// <param name="supplyMethodCode">The supply method code for filtering relevant approvers.</param>
    /// <param name="supplyMethodSpecialTypeCode">Optional special type code for the supply method.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <param name="skipCurrentEmployee"></param>
    /// <returns>A collection of operation information objects containing details about eligible approvers.</returns>
    Task<IEnumerable<OperationInfo>> GetDefaultAcceptorAsync(
        SectionProcessType processType,
        Guid userId,
        decimal budget,
        string supplyMethodCode,
        string? supplyMethodSpecialTypeCode = null,
        CancellationToken ct = default,
        bool skipCurrentEmployee = true);

    /// <summary>
    /// Retrieves the default position information for acceptor based on the user's primary attributes and supply method.
    /// </summary>
    /// <param name="processType">The specific section process type for which the position information is required.</param>
    /// <param name="userId">The unique identifier of the user initiating the process.</param>
    /// <param name="budget">The budget amount for the operation to determine applicable positions.</param>
    /// <param name="supplyMethodCode">The code representing the supply method for filtering positions.</param>
    /// <param name="supplyMethodSpecialTypeCode">An optional special type code for the supply method to further refine the results.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <param name="skipCurrentEmployee"></param>
    /// <returns>A collection of operation position information objects containing details of eligible positions.</returns>
    Task<IEnumerable<OperationPositionInfo>> GetDefaultAcceptorPositionAsync(
        SectionProcessType processType,
        Guid userId,
        decimal budget,
        string supplyMethodCode,
        string? supplyMethodSpecialTypeCode = null,
        CancellationToken ct = default,
        bool skipCurrentEmployee = true);

    /// <summary>
    /// Retrieves the default position information for acceptor based on the user's primary attributes and supply method.
    /// </summary>
    /// <param name="processType">The specific section process type for which the position information is required.</param>
    /// <param name="userId">The unique identifier of the user initiating the process.</param>
    /// <param name="budget">The budget amount for the operation to determine applicable positions.</param>
    /// <param name="supplyMethodCode">The code representing the supply method for filtering positions.</param>
    /// <param name="supplyMethodSpecialTypeCode">An optional special type code for the supply method to further refine the results.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A collection of operation position information objects containing details of eligible positions.</returns>
    Task<IEnumerable<OperationPositionInfo>> GetDefaultAcceptorPositionIgnorePrefixAsync(
        SectionProcessType processType,
        Guid userId,
        decimal budget,
        string supplyMethodCode,
        string? supplyMethodSpecialTypeCode = null,
        CancellationToken ct = default,
        bool skipCurrentEmployee = true);

    IEnumerable<OperationPositionInfo> AddPositionNamePrefix(
        IEnumerable<OperationPositionInfo> managers);

    IEnumerable<OperationPositionInfo> AddPositionNamePrefixPassThroughJorPorDirector(
        IEnumerable<OperationPositionInfo> managers);

    Task<OperationInfo?> GetDefaultJorPorDirectorAsync(CancellationToken ct = default);

    Task<OperationInfo?> GetDefaultExpenseDisbursementDirectorAsync(CancellationToken ct = default);

    Task<OperationInfo?> GetSegmentContractManagerAsync(CancellationToken ct = default);

    Task<OperationInfo?> GetSegmentOtherManagerAsync(CancellationToken ct = default);

    Task<OperationInfo?> GetSegmentITManagerAsync(CancellationToken ct = default);

    Task<OperationInfo?> GetSegmentAccountingManagerAsync(CancellationToken ct = default);

    Task<IEnumerable<OperationInfo>> GetSegmentAccountingMembersAsync(CancellationToken ct = default);

    Task<OperationInfo?> GetDefaultDepartmentDirectorByBusinessUnitIdAsync(string businessUnitId, CancellationToken ct = default);
}

public record SuSectionEmployeeDto(
    int Sequence,
    EmployeeCode EmployeeCode,
    SectionProcessType? ProcessType,
    string InRefCode,
    string ApproverPositionName,
    string ApproverShortPosition,
    string ApproverBusinessUnitName,
    decimal Budget,
    string CommandText,
    string? RefBankOrder,
    decimal? CommandBudget);

[RegisterService<IOperationService>(LifeTime.Scoped)]
public class OperationService : IOperationService
{
    private readonly Dp2DbContext dbContext;
    private readonly ILogger logger;

    public OperationService(
        Dp2DbContext dbContext,
        ILogger<OperationService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    protected record SuSectionApproverDto(
        SectionApproverId Id,
        SectionProcessType ProcessType,
        string InRefCode,
        string PositionName,
        string ShortPosition,
        decimal Budget,
        string CommandText,
        string RefBankOrder);

    protected record ResolveApproverCriteria(
        EmployeeCode StartEmployeeCode,
        RawBusinessUnit? BusinessUnit,
        SectionProcessType ProcessType,
        decimal Budget,
        string SupplyMethodCode,
        string? SupplyMethodSpecialTypeCode = null,
        bool SkipCurrentEmployee = true);

    public async Task<IEnumerable<OperationInfo>> GetDefaultAcceptorAsync(
        SectionProcessType processType,
        Guid userId,
        decimal budget,
        string supplyMethodCode,
        string? supplyMethodSpecialTypeCode = null,
        CancellationToken ct = default,
        bool skipCurrentEmployee = true)
    {
        // 1. ดึงข้อมูลผู้ใช้และตรวจสอบความถูกต้อง
        var user = await this.GetUserAsync(userId, ct);

        if (user is null)
        {
            return [];
        }

        var primaryBusinessUnit = user.Employee.PrimaryBusinessUnit;

        if (primaryBusinessUnit is null)
        {
            return [];
        }

        // 2. ค้นหา approver ทั้งสายที่ตรงกับ section
        var sectionApprover =
            await this.ResolveEligibleSectionApproversAsync(
                new ResolveApproverCriteria(
                    user.EmployeeCode,
                    primaryBusinessUnit,
                    processType,
                    budget,
                    supplyMethodCode,
                    supplyMethodSpecialTypeCode,
                    skipCurrentEmployee),
                ct);

        // 3. สร้างเส้นทางผู้อนุมัติตาม budget
        var managers = await this.GetApprovalPathForBudgetAsync(budget, sectionApprover, primaryBusinessUnit.ParentId, ct);
        var allManagers = managers;
        var lastManager = managers.LastOrDefault();

        // 4. กรณี พรบ. 60 ลำดับสุดท้ายจะต้องเป็น จพ.
        if (supplyMethodCode == SupplyMethodConstant.Sixty
            && (lastManager?.InRefCode.IsDepartmentDirectorIgnoreBP009() == true || lastManager?.InRefCode == InRefCodeConstant.Bp009)
            && managers.Any(m => m.CommandNumber is "72/2562"))
        {
            var jorPorManager = await this.GetDefaultJorPorDirectorAsync(ct);

            if (jorPorManager is not null)
            {
                allManagers = managers
                    .UnionBy(new[] { jorPorManager }, m => m.EmployeeCode);
            }
        }

        return allManagers;
    }

    public async Task<IEnumerable<OperationPositionInfo>> GetDefaultAcceptorPositionAsync(
        SectionProcessType processType,
        Guid userId,
        decimal budget,
        string supplyMethodCode,
        string? supplyMethodSpecialTypeCode = null,
        CancellationToken ct = default,
        bool skipCurrentEmployee = true)
    {
        // 1. ดึง user
        var user = await this.GetUserAsync(userId, ct) ?? throw new InvalidOperationException("User not found.");

        var primaryBusinessUnit = user.Employee.PrimaryBusinessUnit;

        if (primaryBusinessUnit is null)
        {
            return [];
        }

        // 2. ค้นหา approver ทั้งสายที่ตรงกับ section
        var sectionApprover = await this.ResolveEligibleSectionApproversAsync(
            new ResolveApproverCriteria(
                user.EmployeeCode,
                primaryBusinessUnit,
                processType,
                budget,
                supplyMethodCode,
                supplyMethodSpecialTypeCode,
                skipCurrentEmployee),
            ct);

        // 3. สร้างเส้นทางผู้อนุมัติตาม budget
        var managers = this.GetPositionApproverPathForBudgetAsync(budget, sectionApprover);
        var allManagers = managers.OrderBy(a => a.InRefCode);

        var lastManager = managers.LastOrDefault();

        // 4. กรณี พรบ. 60 ลำดับสุดท้ายจะต้องเป็น จพ.
        if (supplyMethodCode == SupplyMethodConstant.Sixty
            && (lastManager?.InRefCode.IsDepartmentDirectorIgnoreBP009() == true || lastManager?.InRefCode == InRefCodeConstant.Bp009)
            && managers.Any(m => m.CommandNumber is "72/2562"))
        {
            var jorPorManager = await this.GetDefaultJorPorDirectorAsync(ct);

            if (jorPorManager is not null)
            {
                // เติมข้อมูลตำแหน่ง จพ. จาก SuSectionApprover โดย match ด้วย InRefCode ของ จพ.
                // (jorPorManager.InRefCode เป็น null จึงใช้ค่า constant ที่ใช้ query จพ. โดยตรง)
                var jorPorPositionInfo = await this.GetSectionApproverPositionInfoAsync(
                    processType,
                    budget,
                    supplyMethodCode,
                    supplyMethodSpecialTypeCode,
                    JorPor.DefaultDirector.PositionInRefCode,
                    ct);

                if (jorPorPositionInfo is not null)
                {
                    allManagers = managers
                        .UnionBy(new[] { jorPorPositionInfo }, m => m.PositionName)
                        .OrderBy(a => a.InRefCode);
                }
            }
        }

        // 5. เพิ่ม prefix ให้กับ PositionName ตามเงื่อนไข InRefCode
        return this.AddPositionNamePrefix(allManagers);
    }

    public async Task<IEnumerable<OperationPositionInfo>> GetDefaultAcceptorPositionIgnorePrefixAsync(
        SectionProcessType processType,
        Guid userId,
        decimal budget,
        string supplyMethodCode,
        string? supplyMethodSpecialTypeCode = null,
        CancellationToken ct = default,
        bool skipCurrentEmployee = true)
    {
        var user = await this.GetUserAsync(userId, ct);

        if (user is not null)
        {
            var primaryBusinessUnit = user.Employee.PrimaryBusinessUnit;

            if (primaryBusinessUnit is null)
            {
                return [];
            }

            var sectionApprover = await this.ResolveEligibleSectionApproversAsync(
                new ResolveApproverCriteria(
                    user.EmployeeCode,
                    primaryBusinessUnit,
                    processType,
                    budget,
                    supplyMethodCode,
                    supplyMethodSpecialTypeCode,
                    skipCurrentEmployee),
                ct);

            var managers = this.GetPositionApproverPathForBudgetAsync(budget, sectionApprover);
            var allManagers = managers.OrderBy(a => a.InRefCode);
            var lastManager = managers.LastOrDefault();

            if (supplyMethodCode == SupplyMethodConstant.Sixty
                && (lastManager?.InRefCode.IsDepartmentDirectorIgnoreBP009() == true || lastManager?.InRefCode == InRefCodeConstant.Bp009)
                && managers.Any(m => m.CommandNumber is "72/2562"))
            {
                var jorPorManager = await this.GetDefaultJorPorDirectorAsync(ct);

                if (jorPorManager is not null)
                {
                    // เติมข้อมูลตำแหน่ง จพ. จาก SuSectionApprover โดย match ด้วย InRefCode ของ จพ.
                    // (jorPorManager.InRefCode เป็น null จึงใช้ค่า constant ที่ใช้ query จพ. โดยตรง)
                    var jorPorPositionInfo = await this.GetSectionApproverPositionInfoAsync(
                        processType,
                        budget,
                        supplyMethodCode,
                        supplyMethodSpecialTypeCode,
                        JorPor.DefaultDirector.PositionInRefCode,
                        ct);

                    if (jorPorPositionInfo is not null)
                    {
                        allManagers = managers
                            .UnionBy(new[] { jorPorPositionInfo }, m => m.PositionName)
                            .OrderBy(a => a.InRefCode);
                    }
                }
            }

            return allManagers;
        }

        return [];
    }

    public async Task<OperationInfo?> GetDefaultJorPorDirectorAsync(CancellationToken ct = default)
    {
        return await this.GetDefaultDepartmentDirectorByBusinessUnitCodeAsync(Constants.JorPor.DefaultDirector.PositionInRefCode, Constants.JorPor.DefaultDirector.BusinessUnitCode, ct);
    }

    public async Task<OperationInfo?> GetDefaultDepartmentDirectorByBusinessUnitIdAsync(string businessUnitId, CancellationToken ct = default)
    {
        var inRefCodeDirector =
            new[]
            {
                InRefCodeConstant.Bp005,
                InRefCodeConstant.Bp007,
                InRefCodeConstant.Bp008,
                InRefCodeConstant.Bp009,
                InRefCodeConstant.Bp010,
                InRefCodeConstant.Bp014,
                InRefCodeConstant.Bp015,
                InRefCodeConstant.Bp017,
                InRefCodeConstant.Bp020,
                InRefCodeConstant.Bp021,
                InRefCodeConstant.Bp026,
                InRefCodeConstant.Bp027,
                InRefCodeConstant.Bp028,
            };

        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(u => u.Positions)
                             .Include(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .Where(u => u.Employee.Positions.Any(p =>
                                 p.BusinessUnit.Id == BusinessUnitId.From(businessUnitId) &&
                                 inRefCodeDirector.Contains(p.Position.InRefCode)))
                             .Select(u => new
                             {
                                 u.Id,
                                 u.Employee,
                                 OrganizationLevel =
                                     u.Employee.Positions
                                      .Select(p => p.BusinessUnit.OrganizationLevel)
                                      .FirstOrDefault(),
                                 u.Employee.View,
                             })
                             .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            this.logger.LogWarning("ไม่พบผู้อำนวยการฝ่ายของหน่วยงาน BusinessUnitId: {BusinessUnitId}", businessUnitId);
            return null;
        }

        if (user.View is null)
        {
            this.logger.LogWarning("ไม่พบข้อมูล View ของพนักงาน BusinessUnitId: {BusinessUnitId}", businessUnitId);
            return null;
        }

        if (user.OrganizationLevel is null)
        {
            this.logger.LogWarning("ไม่พบระดับองค์กร (OrganizationLevel) ของพนักงาน BusinessUnitId: {BusinessUnitId}", businessUnitId);
            return null;
        }

        return new OperationInfo(
            user.Id,
            user.View.EmployeeCode,
            user.Employee.FirstName + " " + user.Employee.LastName,
            user.View.FullName,
            user.View.PositionId,
            user.View.FullPositionName,
            int.Parse(user.OrganizationLevel),
            user.View.BusinessUnitId,
            user.View.BusinessUnitName,
            default,
            default,
            default);
    }

    public async Task<OperationInfo?> GetDefaultDepartmentDirectorByBusinessUnitCodeAsync(string inRefCode, string businessUnitCode, CancellationToken ct = default)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(u => u.Positions)
                             .Include(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .Where(u => u.Employee.Positions.Any(p =>
                                 p.Acting == EmployeeConstant.Acting.Primary &&
                                 p.BusinessUnit.BusinessUnitCode == businessUnitCode &&
                                 p.Position.InRefCode == inRefCode))
                             .Select(u => new
                             {
                                 u.Id,
                                 u.Employee,
                                 OrganizationLevel =
                                     u.Employee.Positions
                                      .Select(p => p.BusinessUnit.OrganizationLevel)
                                      .FirstOrDefault(),
                                 u.Employee.View,
                             })
                             .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            this.logger.LogWarning("ไม่พบผู้รับผิดชอบ BusinessUnitCode: {BusinessUnitCode}, InRefCode: {InRefCode}", businessUnitCode, inRefCode);
            return null;
        }

        if (user.View is null)
        {
            this.logger.LogWarning("ไม่พบข้อมูล View ของพนักงาน BusinessUnitCode: {BusinessUnitCode}", businessUnitCode);
            return null;
        }

        if (user.OrganizationLevel is null)
        {
            this.logger.LogWarning("ไม่พบระดับองค์กร (OrganizationLevel) ของพนักงาน BusinessUnitCode: {BusinessUnitCode}", businessUnitCode);
            return null;
        }

        return new OperationInfo(
            user.Id,
            user.View.EmployeeCode,
            user.Employee.FirstName + " " + user.Employee.LastName,
            user.View.FullName,
            user.View.PositionId,
            user.View.FullPositionName,
            int.Parse(user.OrganizationLevel),
            user.View.BusinessUnitId,
            user.View.BusinessUnitName,
            default,
            default,
            default);
    }

    public async Task<OperationInfo?> GetDefaultExpenseDisbursementDirectorAsync(CancellationToken ct = default)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(u => u.Positions)
                             .Include(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .Where(u => u.Employee.Positions.Any(p =>
                                 p.Position.Name == ExpenseDisbursementConstant.DefaultDirector.PositionName &&
                                 p.BusinessUnit.BusinessUnitCode == ExpenseDisbursementConstant.DefaultDirector.BusinessUnitCode &&
                                 p.BusinessUnit.Name == ExpenseDisbursementConstant.DefaultDirector.BusinessUnitName))
                             .Select(u => new
                             {
                                 u.Id,
                                 u.Employee,
                                 OrganizationLevel =
                                     u.Employee.Positions
                                      .Select(p => p.BusinessUnit.OrganizationLevel)
                                      .FirstOrDefault(),
                                 u.Employee.View,
                             })
                             .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            this.logger.LogWarning("ไม่พบผู้รับผิดชอบฝ่ายเบิกจ่ายค่าใช้จ่าย");
            return null;
        }

        if (user.View is null)
        {
            this.logger.LogWarning("ไม่พบข้อมูล View ของผู้รับผิดชอบฝ่ายเบิกจ่ายค่าใช้จ่าย");
            return null;
        }

        if (user.OrganizationLevel is null)
        {
            this.logger.LogWarning("ไม่พบระดับองค์กร (OrganizationLevel) ของผู้รับผิดชอบฝ่ายเบิกจ่ายค่าใช้จ่าย");
            return null;
        }

        return new OperationInfo(
            user.Id,
            user.View.EmployeeCode,
            user.Employee.FirstName + " " + user.Employee.LastName,
            user.View.FullName,
            user.View.PositionId,
            user.View.FullPositionName,
            int.Parse(user.OrganizationLevel),
            user.View.BusinessUnitId,
            user.View.BusinessUnitName,
            default,
            default,
            default);
    }

    public async Task<OperationInfo?> GetSegmentContractManagerAsync(CancellationToken ct = default)
    {
        return await this.GetSegmentManagerAsync(JorPor.DefaultSectionHead.BusinessUnitId, ct);
    }

    public async Task<OperationInfo?> GetSegmentOtherManagerAsync(CancellationToken ct = default)
    {
        return await this.GetSegmentManagerAsync(JorPor.DefaultSectionHead.JorPorOtherBusinessUnitId, ct);
    }

    public async Task<OperationInfo?> GetSegmentITManagerAsync(CancellationToken ct = default)
    {
        return await this.GetSegmentManagerAsync(JorPor.DefaultSectionHead.JorPorITBusinessUnitId, ct);
    }

    public async Task<OperationInfo?> GetSegmentAccountingManagerAsync(CancellationToken ct = default)
    {
        return await this.GetSegmentManagerAsync(JorPor.DefaultSectionHead.JorPorAccountingBusinessUnitId, ct);
    }

    public async Task<IEnumerable<OperationInfo>> GetSegmentAccountingMembersAsync(CancellationToken ct = default)
    {
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(u => u.Positions)
                              .Include(u => u.Employee)
                              .ThenInclude(u => u.View)
                              .Where(u => u.Employee.Positions.Any(p =>
                                  p.Acting == EmployeeConstant.Acting.Primary &&
                                  !p.Position.Name.StartsWith(JorPor.DefaultSectionHead.PositionName) &&
                                  p.BusinessUnit.Id == BusinessUnitId.From(JorPor.DefaultSectionHead.JorPorAccountingBusinessUnitId)))
                              .Select(u => new
                              {
                                  u.Id,
                                  u.Employee,
                                  OrganizationLevel =
                                      u.Employee.Positions
                                       .Select(p => p.BusinessUnit.OrganizationLevel)
                                       .FirstOrDefault(),
                                  u.Employee.View,
                              })
                              .ToListAsync(ct);

        return users
            .Where(u => u.View is not null && u.OrganizationLevel is not null)
            .Select(u => new OperationInfo(
                u.Id,
                u.View!.EmployeeCode,
                u.Employee.FirstName + " " + u.Employee.LastName,
                u.View.FullName,
                u.View.PositionId,
                u.View.FullPositionName,
                int.Parse(u.OrganizationLevel!),
                u.View.BusinessUnitId,
                u.View.BusinessUnitName,
                default,
                default,
                default));
    }

    private async Task<OperationInfo?> GetSegmentManagerAsync(string businessUnitId, CancellationToken ct)
    {
        return await this.GetDirectorByBusinessUnitIdAsync(businessUnitId, JorPor.DefaultSectionHead.PositionName, ct);
    }

    private async Task<OperationInfo?> GetDirectorByBusinessUnitIdAsync(string businessUnitId, string positionName, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(u => u.Positions)
                             .Include(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .Where(u => u.Employee.Positions.Any(p =>
                                 p.Acting == EmployeeConstant.Acting.Primary &&
                                 p.BusinessUnit.Id == BusinessUnitId.From(businessUnitId) &&
                                 p.Position.Name == positionName))
                             .Select(u => new
                             {
                                 u.Id,
                                 u.Employee,
                                 OrganizationLevel =
                                     u.Employee.Positions
                                      .Select(p => p.BusinessUnit.OrganizationLevel)
                                      .FirstOrDefault(),
                                 u.Employee.View,
                             })
                             .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            this.logger.LogWarning("ไม่พบผู้รับผิดชอบของหน่วยงาน BusinessUnitId: {BusinessUnitId}, ตำแหน่ง: {PositionName}", businessUnitId, positionName);
            return null;
        }

        if (user.View is null)
        {
            this.logger.LogWarning("ไม่พบข้อมูล View ของพนักงาน BusinessUnitId: {BusinessUnitId}", businessUnitId);
            return null;
        }

        if (user.OrganizationLevel is null)
        {
            this.logger.LogWarning("ไม่พบระดับองค์กร (OrganizationLevel) ของพนักงาน BusinessUnitId: {BusinessUnitId}", businessUnitId);
            return null;
        }

        return new OperationInfo(
            user.Id,
            user.View.EmployeeCode,
            user.Employee.FirstName + " " + user.Employee.LastName,
            user.View.FullName,
            user.View.PositionId,
            user.View.FullPositionName,
            int.Parse(user.OrganizationLevel),
            user.View.BusinessUnitId,
            user.View.BusinessUnitName,
            default,
            default,
            default);
    }

    /// <summary>
    /// Adds prefix to position names based on InRefCode conditions
    /// </summary>
    /// <param name="managers">Collection of operation position info</param>
    /// <returns>Collection with prefixed position names</returns>
    /// <summary>
    /// Adds prefix to position names based on InRefCode conditions
    /// </summary>
    /// <returns>Collection with prefixed position names</returns>
    /// <summary>
    /// Adds prefix to position names based on InRefCode conditions
    /// </summary>
    /// <returns>Collection with prefixed position names</returns>
    /// <summary>
    /// Adds prefix to position names based on InRefCode conditions
    /// </summary>
    /// <returns>Collection with prefixed position names</returns>
    /// <summary>
    /// Adds prefix to position names based on InRefCode conditions
    /// </summary>
    /// <returns>Collection with prefixed position names</returns>
    public IEnumerable<OperationPositionInfo> AddPositionNamePrefix(
           IEnumerable<OperationPositionInfo> managers)
    {
        var inRefCodeManager = new[]
        {
            InRefCodeConstant.Bp001,
            InRefCodeConstant.Bp002,
            InRefCodeConstant.Bp003,
            InRefCodeConstant.Bp005,
            InRefCodeConstant.Bp006,
            InRefCodeConstant.Bp024,
            InRefCodeConstant.Bp025,
        };

        var managersByInRefCode =
            managers
                .Where(m =>
                    inRefCodeManager.Contains(m.InRefCode))
                .OrderBy(o => o.InRefCode)
                .Map((index, operation) => operation with
                {
                    PositionName =
                    index == 0 ? operation.PositionName : $"ผ่าน {operation.PositionName}",
                })
                .ToList();

        return
            managersByInRefCode.Any()
                ? managersByInRefCode
                :
                [
                    .. managers
                       .OrderByDescending(o => o.Budget)
                       .Take(1)
                ];
    }

    public IEnumerable<OperationPositionInfo> AddPositionNamePrefixPassThroughJorPorDirector(
           IEnumerable<OperationPositionInfo> managers)
    {
        var headInRefCodes = new[]
        {
            InRefCodeConstant.Bp001,
            InRefCodeConstant.Bp002,
            InRefCodeConstant.Bp003,
            InRefCodeConstant.Bp024,
        };

        var head = managers
            .Where(m => headInRefCodes.Contains(m.InRefCode))
            .OrderBy(o => o.InRefCode)
            .FirstOrDefault();

        var passThrough = managers
            .FirstOrDefault(m => m.InRefCode == InRefCodeConstant.Bp008);

        if (head != null && passThrough != null)
        {
            return
            [
                head,
                passThrough with { PositionName = $"ผ่าน {passThrough.PositionName}" },
            ];
        }

        if (head != null)
        {
            return [head];
        }

        if (passThrough != null)
        {
            return [passThrough];
        }

        return managers
            .OrderBy(o => o.InRefCode)
            .Take(1);
    }

    private Task<SuUser?> GetUserAsync(Guid userId, CancellationToken ct)
    {
        return this.dbContext.SuUsers
                   .Include(user => user.Employee)
                   .ThenInclude(employee => employee.View)
                   .Include(user => user.Employee)
                   .ThenInclude(employee => employee.Positions)
                   .ThenInclude(position => position.Position)
                   .SingleOrDefaultAsync(user => user.Id == UserId.From(userId), ct);
    }

    private IEnumerable<OperationPositionInfo> GetPositionApproverPathForBudgetAsync(
        decimal budget,
        IEnumerable<SuSectionEmployeeDto> sectionApprovers)
    {
        var budgetApprovers = sectionApprovers
                              .Where(s => s.Budget >= budget)
                              .ToList();

        if (!budgetApprovers.Any())
        {
            return Enumerable.Empty<OperationPositionInfo>();
        }

        var commandText = budgetApprovers.FirstOrDefault()?.CommandText;

        var finalApprover = ResolveFinalApprover(budgetApprovers);

        var ordered = sectionApprovers.OrderBy(s => s.Sequence).ToList();
        var result = new List<OperationPositionInfo>();

        for (var i = 0; i < ordered.Count; i++)
        {
            var sectionApprover = ordered[i];
            var isFinalApprover = finalApprover is not null
                && sectionApprover.EmployeeCode == finalApprover.EmployeeCode
                && sectionApprover.InRefCode == finalApprover.InRefCode;

            // Skip consecutive same EmployeeCode unless it's the finalApprover
            if (!isFinalApprover
                && i + 1 < ordered.Count
                && sectionApprover.EmployeeCode == ordered[i + 1].EmployeeCode)
            {
                continue;
            }

            var operationInfo = sectionApprover.CreateOperationPositionInfo(commandNumber: commandText);

            if (operationInfo != null)
            {
                result.Add(operationInfo);
            }

            if (isFinalApprover)
            {
                break;
            }
        }

        return result;
    }

    private async Task<IEnumerable<OperationInfo>> GetApprovalPathForBudgetAsync(
        decimal budget,
        IEnumerable<SuSectionEmployeeDto> sectionApprovers,
        BusinessUnitId? businessUnitParentId = null,
        CancellationToken ct = default)
    {
        var budgetApprovers = sectionApprovers
                              .Where(s => s.Budget >= budget)
                              .ToList();

        if (!budgetApprovers.Any())
        {
            return Enumerable.Empty<OperationInfo>();
        }

        var finalApprover = ResolveFinalApprover(budgetApprovers);

        var ordered = sectionApprovers.OrderBy(s => s.Sequence).ToList();
        var result = new List<OperationInfo>();

        for (var i = 0; i < ordered.Count; i++)
        {
            var approver = ordered[i];
            var isFinalApprover = finalApprover is not null
                && approver.EmployeeCode == finalApprover.EmployeeCode
                && approver.InRefCode == finalApprover.InRefCode;

            // Skip consecutive same EmployeeCode unless it's the finalApprover
            if (!isFinalApprover
                && i + 1 < ordered.Count
                && approver.EmployeeCode == ordered[i + 1].EmployeeCode)
            {
                continue;
            }

            var user = await this.dbContext.SuUsers
                                 .AsNoTracking()
                                 .Include(suUser => suUser.Employee)
                                 .FirstOrDefaultAsync(u => u.EmployeeCode == approver.EmployeeCode, ct);

            var operationInfo = user?.Employee?.CreateOperationInfo(
                ref businessUnitParentId,
                approver.CommandText,
                approver.CommandBudget,
                approver.InRefCode);

            if (operationInfo != null)
            {
                result.Add(operationInfo);
            }

            if (isFinalApprover)
            {
                break;
            }
        }

        return result;
    }

    private static SuSectionEmployeeDto? ResolveFinalApprover(IEnumerable<SuSectionEmployeeDto> sectionApprovers)
    {
        var approverList = sectionApprovers.ToList();

        if (!approverList.Any())
        {
            return null;
        }

        var minBudget = approverList.Min(s => s.Budget);

        return approverList
            .Where(s => s.Budget == minBudget)
            .OrderByDescending(s => s.Sequence)
            .FirstOrDefault();
    }

    private async Task<IEnumerable<SuSectionEmployeeDto>> ResolveEligibleSectionApproversAsync(
        ResolveApproverCriteria criteria,
        CancellationToken ct = default)
    {
        var section = await this.dbContext.SuSections
                                .Include(suSection => suSection.Approvers)
                                .AsNoTracking()
                                .Where(section =>
                                    section.MaximumBudget >= criteria.Budget &&
                                    section.SupplyMethodCode == ParameterCode.From(criteria.SupplyMethodCode) &&
                                    (
                                        string.IsNullOrEmpty(criteria.SupplyMethodSpecialTypeCode)
                                            ? section.SupplyMethodSpecialTypeCode == null
                                            : section.SupplyMethodSpecialTypeCode == ParameterCode.From(criteria.SupplyMethodSpecialTypeCode)
                                    ))
                                .FirstOrDefaultAsync(ct);

        // เลือก approver ที่มี budget ต่ำสุด (ใกล้เคียง requested budget ที่สุด) สำหรับแต่ละ InRefCode
        var sectionApprovers = section?.Approvers
                                      .Where(a => a.ProcessType == criteria.ProcessType && a.Budget >= criteria.Budget)
                                      .GroupBy(a => a.InRefCode)
                                      .Select(g => g.OrderBy(a => a.Budget).First())
                                      ?? [];

        var sectionHeads = new List<SuSectionEmployeeDto>();
        await this.TraverseManagerHierarchyAsync(criteria.StartEmployeeCode, criteria.BusinessUnit?.Id, sectionHeads, sectionApprovers, ct, skipFirst: criteria.SkipCurrentEmployee);

        return sectionHeads;
    }

    /// <summary>
    /// ดึงข้อมูลตำแหน่งผู้อนุมัติจาก SuSectionApprover โดย match ด้วย InRefCode ภายใน section ที่ตรงเงื่อนไข
    /// (supply method / special type / budget / process type) แล้ว map เป็น OperationPositionInfo
    /// </summary>
    private async Task<OperationPositionInfo?> GetSectionApproverPositionInfoAsync(
        SectionProcessType processType,
        decimal budget,
        string supplyMethodCode,
        string? supplyMethodSpecialTypeCode,
        string? inRefCode,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(inRefCode))
        {
            return null;
        }

        var section = await this.dbContext.SuSections
                                .Include(suSection => suSection.Approvers)
                                .AsNoTracking()
                                .Where(section =>
                                    section.MaximumBudget >= budget &&
                                    section.SupplyMethodCode == ParameterCode.From(supplyMethodCode) &&
                                    (
                                        string.IsNullOrEmpty(supplyMethodSpecialTypeCode)
                                            ? section.SupplyMethodSpecialTypeCode == null
                                            : section.SupplyMethodSpecialTypeCode == ParameterCode.From(supplyMethodSpecialTypeCode)
                                    ))
                                .FirstOrDefaultAsync(ct);

        var approver = section?.Approvers
                              .FirstOrDefault(a => a.ProcessType == processType && a.InRefCode == inRefCode);

        if (section is null || approver is null)
        {
            this.logger.LogWarning(
                "ไม่พบ SuSectionApprover สำหรับ InRefCode: {InRefCode}, ProcessType: {ProcessType}",
                inRefCode,
                processType);
            return null;
        }

        return new OperationPositionInfo(
            approver.PositionName,
            section.RefBankOrder,
            approver.ShortPosition,
            approver.Budget,
            approver.InRefCode,
            section.Id,
            approver.CommandText,
            approver.CommandBudget);
    }

    private async Task<IEnumerable<SuSectionEmployeeDto>> ResolveEligibleJorPortSectionApproversAsync(
        SectionProcessType processType,
        decimal budget,
        string supplyMethodCode,
        string? supplyMethodSpecialTypeCode,
        CancellationToken ct)
    {
        var section = await this.dbContext.SuSections
                                .Include(suSection => suSection.Approvers)
                                .AsNoTracking()
                                .Where(section =>
                                    section.MaximumBudget >= budget &&
                                    section.SupplyMethodCode == ParameterCode.From(supplyMethodCode) &&
                                    (
                                        string.IsNullOrEmpty(supplyMethodSpecialTypeCode)
                                            ? section.SupplyMethodSpecialTypeCode == null
                                            : section.SupplyMethodSpecialTypeCode == ParameterCode.From(supplyMethodSpecialTypeCode)
                                    ))
                                .FirstOrDefaultAsync(ct);

        var sectionApprovers = section?.Approvers
                                      .Where(a => a.ProcessType == processType);

        var sectionJorPorHeads = await this.TraverseJorPorDirectorHierarchyAsync(ct);

        var approvers = MapSectionApprovers(
            sectionJorPorHeads,
            sectionApprovers,
            section,
            processType);

        return approvers;
    }

    private static List<SuSectionEmployeeDto> MapSectionApprovers(
        List<RawEmployee> sectionHeads,
        IEnumerable<SuSectionApprover>? sectionApprovers,
        SuSection? section,
        SectionProcessType processType,
        BusinessUnitId? businessUnitParentId = null)
    {
        return
        [
            .. sectionHeads.Map((index, employee) =>
            {
                var employeePosition =
                    businessUnitParentId is null
                        ? employee.PrimaryEmployeePosition
                        : GetPosition(employee, ref businessUnitParentId);

                var position = employeePosition?.Position;
                var businessUnit = employeePosition?.BusinessUnit;

                var sectionApprover = sectionApprovers?
                    .FirstOrDefault(a => a.InRefCode == position?.InRefCode);

                return new SuSectionEmployeeDto(
                    index,
                    employee.Id,
                    processType,
                    position?.InRefCode ?? string.Empty,
                    position?.Name ?? string.Empty,
                    sectionApprover?.ShortPosition ?? string.Empty,
                    businessUnit?.Name ?? string.Empty,
                    sectionApprover?.Budget ?? 0,
                    sectionApprover?.CommandText ?? string.Empty,
                    section?.RefBankOrder ?? string.Empty,
                    sectionApprover?.CommandBudget ?? 0);
            })
        ];

        static RawEmployeePosition? GetPosition(
            RawEmployee employee, ref BusinessUnitId? businessUnitParentId)
        {
            var parentBusinessUnit = businessUnitParentId;
            var rawPosition =
                employee.Positions
                        .OrderBy(p => p.EmployeeType)
                        .FirstOrDefault(p =>
                            p.BusinessUnitId == parentBusinessUnit || p.BusinessUnit.ParentId == parentBusinessUnit);

            businessUnitParentId = rawPosition?.BusinessUnit?.ParentId;

            return rawPosition;
        }
    }

    private async Task<List<RawEmployee>> TraverseJorPorDirectorHierarchyAsync(CancellationToken ct)
    {
        var sectionJorPorHeads = new List<RawEmployee>();

        var userSegment = await this.GetDefaultJorPorDirectorAsync(ct);

        if (userSegment is null)
        {
            this.logger.LogWarning("ไม่พบผู้อำนวยการฝ่ายจัดหาและการพัสดุ (จพ.)");
            return sectionJorPorHeads;
        }

        var user = await this.GetUserAsync(userSegment.UserId.Value, ct);

        if (user is null)
        {
            this.logger.LogWarning("ไม่พบข้อมูลผู้ใช้งานของผู้อำนวยการฝ่ายจัดหาและการพัสดุ UserId: {UserId}", userSegment.UserId.Value);
            return sectionJorPorHeads;
        }

        var primaryBusinessUnit = user.Employee.PrimaryBusinessUnit;

        if (primaryBusinessUnit is null)
        {
            this.logger.LogWarning("ไม่พบหน่วยงานหลัก (PrimaryBusinessUnit) ของผู้อำนวยการฝ่ายจัดหาและการพัสดุ");
            return sectionJorPorHeads;
        }

        await this.TraverseManagerHierarchyAsync(
            userSegment.EmployeeCode,
            primaryBusinessUnit.Id,
            sectionJorPorHeads,
            ct,
            skipFirst: false);

        return sectionJorPorHeads;
    }

    private async Task TraverseManagerHierarchyAsync(
        EmployeeCode employeeCode,
        BusinessUnitId? businessUnitId,
        List<RawEmployee> sectionHeads,
        CancellationToken ct,
        bool skipFirst = true)
    {
        var currentPosition =
            await this.dbContext.RawEmployeePositions
                      .Include(p => p.Position)
                      .Include(p => p.Employee)
                      .ThenInclude(e => e.Users)
                      .Include(rawEmployeePosition => rawEmployeePosition.Manager)
                      .FirstOrDefaultAsync(p => p.EmployeeCode == employeeCode && p.BusinessUnitId == businessUnitId, ct);

        if (currentPosition == null)
        {
            return;
        }

        var managerEmployees = currentPosition.Employee;

        if (managerEmployees != null &&
            sectionHeads.Any(e => e.Id == employeeCode))
        {
            return;
        }

        if (managerEmployees != null && !skipFirst)
        {
            sectionHeads.AddRange(managerEmployees);
        }

        if (currentPosition.ManagerEmployeeCode == null)
        {
            return;
        }

        if (currentPosition.ManagerEmployeeCode.Value == employeeCode)
        {
            // Self-referencing manager (e.g. Acting = X position)
            // Fallback to primary position (Acting = R) to find real manager
            var primaryPosition = await this.dbContext.RawEmployeePositions
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(
                    p =>
                    p.EmployeeCode == employeeCode &&
                    p.Acting == EmployeeConstant.Acting.Primary &&
                    p.ManagerEmployeeCode != null &&
                    p.ManagerEmployeeCode != employeeCode,
                    ct);

            if (primaryPosition == null)
            {
                return;
            }

            await this.TraverseManagerHierarchyAsync(
                primaryPosition.ManagerEmployeeCode!.Value,
                primaryPosition.Manager.PrimaryBusinessUnit?.Id,
                sectionHeads,
                ct,
                skipFirst: false);

            return;
        }

        await this.TraverseManagerHierarchyAsync(
            currentPosition.ManagerEmployeeCode.Value,
            currentPosition.Manager.PrimaryBusinessUnit?.Id,
            sectionHeads,
            ct,
            skipFirst: false);
    }

    private async Task TraverseManagerHierarchyAsync(
        EmployeeCode employeeCode,
        BusinessUnitId? businessUnitId,
        List<SuSectionEmployeeDto> sectionHeads,
        IEnumerable<SuSectionApprover> sectionApprovers,
        CancellationToken ct,
        bool skipFirst = true)
    {
        var currentPosition =
            await this.dbContext.RawEmployeePositions
                      .Include(p => p.Position)
                      .Include(p => p.BusinessUnit)
                      .Include(p => p.Employee)
                      .ThenInclude(e => e.Users)
                      .Include(p => p.Employee)
                      .ThenInclude(e => e.Positions)
                      .ThenInclude(p => p.Position)
                      .Include(p => p.Employee)
                      .ThenInclude(e => e.Positions)
                      .ThenInclude(p => p.BusinessUnit)
                      .Include(rawEmployeePosition => rawEmployeePosition.Manager)
                      .ThenInclude(rawEmployee => rawEmployee.Positions)
                      .ThenInclude(rawPosition => rawPosition.BusinessUnit)
                      .FirstOrDefaultAsync(p => p.EmployeeCode == employeeCode && p.BusinessUnitId == businessUnitId, ct);

        if (currentPosition == null)
        {
            return;
        }

        var managerEmployees = currentPosition.Employee;

        if (!skipFirst)
        {
            foreach (var pos in managerEmployees.Positions.Where(p => p.BusinessUnitId == businessUnitId))
            {
                var position = pos.Position;
                var businessUnit = pos.BusinessUnit;
                var sectionApprover = sectionApprovers?
                    .FirstOrDefault(a => a.InRefCode == position?.InRefCode);

                sectionHeads.Add(new SuSectionEmployeeDto(
                    sectionHeads.Count + 1,
                    managerEmployees.Id,
                    sectionApprover?.ProcessType,
                    position?.InRefCode ?? string.Empty,
                    position?.Name ?? string.Empty,
                    sectionApprover?.ShortPosition ?? string.Empty,
                    businessUnit?.Name ?? string.Empty,
                    sectionApprover?.Budget ?? 0,
                    sectionApprover?.CommandText ?? string.Empty,
                    sectionApprover?.Section?.RefBankOrder,
                    sectionApprover?.CommandBudget ?? 0));
            }
        }

        if (currentPosition.ManagerEmployeeCode == null)
        {
            return;
        }

        // 1. Self-referencing manager (e.g. Acting = X with ManagerEmployeeCode pointing to self)
        if (currentPosition.ManagerEmployeeCode.Value == employeeCode)
        {
            // Self-referencing manager (e.g. Acting = X position)
            // Fallback to primary position (Acting = R) to find real manager
            var primaryPosition = await this.dbContext.RawEmployeePositions
                .Include(p => p.Position)
                .Include(p => p.BusinessUnit)
                .Include(p => p.Manager)
                .ThenInclude(e => e.Positions)
                .ThenInclude(p => p.BusinessUnit)
                .FirstOrDefaultAsync(
                    p =>
                    p.EmployeeCode == employeeCode &&
                    p.Acting == EmployeeConstant.Acting.Primary &&
                    p.ManagerEmployeeCode != null &&
                    p.ManagerEmployeeCode != employeeCode,
                    ct);

            if (primaryPosition == null)
            {
                return;
            }

            // Add primary position (Acting = R) to sectionHeads
            if (!skipFirst)
            {
                var primaryPosInfo = primaryPosition.Position;
                var primaryBu = primaryPosition.BusinessUnit;
                var primarySectionApprover = sectionApprovers?
                    .FirstOrDefault(a => a.InRefCode == primaryPosInfo?.InRefCode);

                sectionHeads.Add(new SuSectionEmployeeDto(
                    sectionHeads.Count + 1,
                    primaryPosition.EmployeeCode,
                    primarySectionApprover?.ProcessType,
                    primaryPosInfo?.InRefCode ?? string.Empty,
                    primaryPosInfo?.Name ?? string.Empty,
                    primarySectionApprover?.ShortPosition ?? string.Empty,
                    primaryBu?.Name ?? string.Empty,
                    primarySectionApprover?.Budget ?? 0,
                    primarySectionApprover?.CommandText ?? string.Empty,
                    primarySectionApprover?.Section?.RefBankOrder,
                    primarySectionApprover?.CommandBudget ?? 0));
            }

            var primaryManagerBusinessUnit =
                primaryPosition.Manager.Positions
                    .Where(p => p.BusinessUnitId == primaryPosition.BusinessUnit?.Id)
                    .Select(p => new { p.BusinessUnitId })
                    .FirstOrDefault() ??
                primaryPosition.Manager.Positions
                    .Where(p => p.BusinessUnitId == primaryPosition.BusinessUnit?.ParentId)
                    .Select(p => new { p.BusinessUnitId })
                    .FirstOrDefault() ??
                primaryPosition.Manager.Positions
                    .Where(p => p.BusinessUnit.ParentId == primaryPosition.BusinessUnit?.ParentId)
                    .Select(p => new { p.BusinessUnitId })
                    .FirstOrDefault();

            await this.TraverseManagerHierarchyAsync(
                primaryPosition.ManagerEmployeeCode!.Value,
                primaryManagerBusinessUnit?.BusinessUnitId,
                sectionHeads,
                sectionApprovers ?? [],
                ct,
                skipFirst: false);

            return;
        }

        // 2. Circular chain detection (manager already in list)
        if (sectionHeads.Any(s => s.EmployeeCode == currentPosition.ManagerEmployeeCode.Value))
        {
            return;
        }

        foreach (var pos in currentPosition.Manager.Positions)
        {
            this.logger.LogWarning(
                "Manager Position: ManagerEmployeeCode={ManagerEmployeeCode}, BusinessUnitId={BusinessUnitId}, BusinessUnitParentId={BusinessUnitParentId}, Acting={Acting}, PositionName={PositionName}",
                pos.EmployeeCode,
                pos.BusinessUnitId,
                pos.BusinessUnit?.ParentId,
                pos.Acting,
                pos.Position?.Name);
        }

        var managerBusinessUnit =
            currentPosition.Manager.Positions
                           .Where(p =>
                               p.BusinessUnitId == currentPosition.BusinessUnit.Id)
                           .Select(p => new { p.BusinessUnitId, p.EmployeeCode, p.Acting })
                           .FirstOrDefault() ??
            currentPosition.Manager.Positions
                           .Where(p =>
                               p.BusinessUnitId == currentPosition.BusinessUnit.ParentId)
                           .Select(p => new { p.BusinessUnitId, p.EmployeeCode, p.Acting })
                           .FirstOrDefault() ??
            currentPosition.Manager.Positions
                           .Where(p =>
                               p.BusinessUnit.ParentId == currentPosition.BusinessUnit.ParentId)
                           .Select(p => new { p.BusinessUnitId, p.EmployeeCode, p.Acting })
                           .FirstOrDefault();

        this.logger.LogWarning(
            "Traversing manager hierarchy: EmployeeCode={EmployeeCode}, ManagerEmployeeCode={ManagerEmployeeCode}, BusinessUnitId={BusinessUnitId}, ManagerBusinessUnitId={ManagerBusinessUnitId}, Acting={Acting}",
            currentPosition.EmployeeCode,
            currentPosition.ManagerEmployeeCode,
            currentPosition.BusinessUnitId,
            managerBusinessUnit?.BusinessUnitId,
            managerBusinessUnit?.Acting);

        await this.TraverseManagerHierarchyAsync(
            currentPosition.ManagerEmployeeCode.Value,
            managerBusinessUnit?.BusinessUnitId,
            sectionHeads,
            sectionApprovers ?? [],
            ct,
            skipFirst: false);
    }
}