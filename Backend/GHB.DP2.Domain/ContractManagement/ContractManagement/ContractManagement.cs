namespace GHB.DP2.Domain.ContractManagement.ContractManagement;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractManagementId
{
    public static ContractManagementId New => From(Guid.CreateVersion7());
}

public enum ContractManagementStep
{
    Step1,

    Step2,

    Step3,
}

public enum ContractManagementStatus
{
    Draft,

    InProgress,

    Done,

    Cancelled,
}

public partial class ContractManagement : AuditableEntity<ContractManagementId>, IHasActivityInfo
{
    public override ContractManagementId Id { get; init; }

    public ContractDraftVendorId CaContractDraftVendorId { get; private set; }

    public ProcurementId ProcurementId { get; private set; }

    public ContractManagementStep Step { get; private set; }

    public ContractManagementStatus Status { get; private set; }

    public string ContractName { get; private set; }

    public BusinessUnitId DepartmentId { get; private set; }

    public ParameterCode SupplyMethodCode { get; private set; }

    public ParameterCode? SupplyMethodTypeCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    public int BudgetYear { get; private set; }

    public virtual CaContractDraftVendor CaContractDraftVendor { get; init; }

    public virtual Procurement Procurement { get; init; }

    public virtual RawBusinessUnit Department { get; init; }

    public virtual SuParameter SupplyMethod { get; init; }

    public virtual SuParameter? SupplyMethodType { get; init; }

    public virtual SuParameter? SupplyMethodSpecialType { get; init; }

    public ContractManagement SetProcurementId(ProcurementId procurementId)
    {
        this.ProcurementId = procurementId;

        return this;
    }

    public ContractManagement SetStep(ContractManagementStep step)
    {
        this.Step = step;

        return this;
    }

    public ContractManagement SetStatus(ContractManagementStatus status)
    {
        this.Status = status;

        return this;
    }

    public ContractManagement SetContractName(string contractName)
    {
        if (string.IsNullOrWhiteSpace(contractName))
        {
            throw new ArgumentException("Contract name cannot be null or empty.", nameof(contractName));
        }

        this.ContractName = contractName;

        return this;
    }

    public ContractManagement SetDepartmentId(BusinessUnitId departmentId)
    {
        this.DepartmentId = departmentId;

        return this;
    }

    public ContractManagement SetBudgetYear(int budgetYear)
    {
        this.BudgetYear = budgetYear;

        return this;
    }

    public ContractManagement SetSupplyMethod(
        ParameterCode supplyMethodCode,
        ParameterCode? supplyMethodTypeCode,
        ParameterCode? supplyMethodSpecialTypeCode)
    {
        this.SupplyMethodCode = supplyMethodCode;
        this.SupplyMethodTypeCode = supplyMethodTypeCode;
        this.SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode;

        return this;
    }

    public static ContractManagement Create(
        ContractDraftVendorId caContractDraftVendorId,
        ProcurementId procurementId,
        string contractName,
        BusinessUnitId departmentId,
        ParameterCode supplyMethodCode,
        ParameterCode? supplyMethodTypeCode,
        ParameterCode? supplyMethodSpecialTypeCode,
        int budgetYear)
    {
        if (string.IsNullOrWhiteSpace(contractName))
        {
            throw new ArgumentException("Contract name cannot be null or empty.", nameof(contractName));
        }

        var entity = new ContractManagement
        {
            Id = ContractManagementId.New,
            CaContractDraftVendorId = caContractDraftVendorId,
            ProcurementId = procurementId,
            Step = ContractManagementStep.Step1,
            Status = ContractManagementStatus.Draft,
            ContractName = contractName,
            DepartmentId = departmentId,
            SupplyMethodCode = supplyMethodCode,
            SupplyMethodTypeCode = supplyMethodTypeCode,
            SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode,
            BudgetYear = budgetYear,
        };

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลใหม่ {nameof(ContractManagement)}",
            entity.Status.ToString()));

        return entity;
    }
}
