namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public class Delivery
{
    public string? Address { get; private set; }

    public DateTimeOffset? Date { get; private set; }

    public int? LeadTime { get; private set; }

    public ParameterCode? LeadTimeTypeCode { get; private set; }

    public int? LeadOtherTime { get; private set; }

    public ParameterCode? PeriodTypeCode { get; private set; }

    public ParameterCode? LeadOtherTimeTypeCode { get; private set; }

    public ParameterCode? CountingConditionCode { get; private set; }

    public virtual SuParameter? LeadTimeType { get; init; }

    public virtual SuParameter? LeadOtherTimeType { get; init; }

    public virtual SuParameter? CountingCondition { get; init; }

    public Delivery SetAddress(string? address)
    {
        this.Address = address;

        return this;
    }

    public Delivery SetDate(DateTimeOffset? date)
    {
        this.Date = date;

        return this;
    }

    public Delivery SetLeadTime(
        int? leadTime,
        string? leadTimeTypeCode,
        string? periodTypeCode)
    {
        this.LeadTime = leadTime;
        this.LeadTimeTypeCode = !string.IsNullOrWhiteSpace(leadTimeTypeCode) ? ParameterCode.From(leadTimeTypeCode) : null;
        this.PeriodTypeCode = !string.IsNullOrWhiteSpace(periodTypeCode) ? ParameterCode.From(periodTypeCode) : null;

        return this;
    }

    public Delivery SetLeadOtherTime(
        int? leadOtherTime,
        string? leadOtherTimeTypeCode)
    {
        this.LeadOtherTime = leadOtherTime;
        this.LeadOtherTimeTypeCode = !string.IsNullOrWhiteSpace(leadOtherTimeTypeCode) ? ParameterCode.From(leadOtherTimeTypeCode) : null;

        return this;
    }

    public Delivery SetCountingCondition(string? countingConditionCode)
    {
        this.CountingConditionCode = !string.IsNullOrWhiteSpace(countingConditionCode) ? ParameterCode.From(countingConditionCode) : null;

        return this;
    }
}