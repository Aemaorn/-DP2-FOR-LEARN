namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public class Payment
{
    public ParameterCode? TypeCode { get; init; }

    public int? DueDays { get; init; }

    public ParameterCode? RedeliveryDateCode { get; init; }

    public virtual SuParameter? Type { get; init; }

    public virtual SuParameter? RedeliveryDate { get; init; }

    public Payment(
        ParameterCode? typeCode,
        int? dueDays,
        ParameterCode? redeliveryDateCode)
    {
        this.TypeCode = typeCode;
        this.DueDays = dueDays;
        this.RedeliveryDateCode = redeliveryDateCode;
    }

    public static Payment Default()
    {
        return new Payment(
            typeCode: null,
            dueDays: null,
            redeliveryDateCode: null);
    }
}