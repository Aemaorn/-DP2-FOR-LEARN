namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

public class Termination
{
    public DateTimeOffset? StartDate { get; init; }

    public DateTimeOffset? EndDate { get; init; }

    public RentalDurationInfo? Duration { get; init; }

    public RentalDurationInfo? VendorProcessingTime { get; init; }

    public Termination()
    {
        // Parameterless constructor for EF Core
    }

    public Termination(
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        RentalDurationInfo? vendorProcessingTime)
    {
        this.StartDate = startDate;
        this.EndDate = endDate;
        this.VendorProcessingTime = vendorProcessingTime;
    }
}