namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

public class LeaseCopier
{
    public int? NumberOfMachines { get; init; }

    public int? RentalQuantity { get; init; }

    public int? MonthlyRentalRate { get; init; }

    public int? EstimatedMonthlyCopyVolume { get; init; }

    public int? ActualMonthlyCopyVolume { get; init; }

    public decimal? CopyRatePerPage { get; init; }

    public LeaseCopier()
    {
        // Ef Core constructor
    }

    public LeaseCopier(
        int? numberOfMachines,
        int? rentalQuantity,
        int? monthlyRentalRate,
        int? estimatedMonthlyCopyVolume,
        int? actualMonthlyCopyVolume,
        decimal? copyRatePerPage)
    {
        this.NumberOfMachines = numberOfMachines;
        this.RentalQuantity = rentalQuantity;
        this.MonthlyRentalRate = monthlyRentalRate;
        this.EstimatedMonthlyCopyVolume = estimatedMonthlyCopyVolume;
        this.ActualMonthlyCopyVolume = actualMonthlyCopyVolume;
        this.CopyRatePerPage = copyRatePerPage;
    }

    public static LeaseCopier Default()
    {
        return new LeaseCopier
        {
            NumberOfMachines = null,
            RentalQuantity = null,
            MonthlyRentalRate = null,
            EstimatedMonthlyCopyVolume = null,
            ActualMonthlyCopyVolume = null,
            CopyRatePerPage = null,
        };
    }
}