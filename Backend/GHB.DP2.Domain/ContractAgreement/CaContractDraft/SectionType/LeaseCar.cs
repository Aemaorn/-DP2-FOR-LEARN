namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public class LeaseCar
{
    public decimal? RentPerVehicle { get; init; }

    public ParameterCode? UnitCode { get; init; }

    public virtual SuParameter? Unit { get; init; }

    public LeaseCar()
    {
        // Ef Core constructor
    }

    public LeaseCar(decimal rentPerVehicle, ParameterCode? unitCode)
    {
        this.RentPerVehicle = rentPerVehicle;
        this.UnitCode = unitCode;
    }

    public static LeaseCar Default()
    {
        return new LeaseCar
        {
            RentPerVehicle = null,
            UnitCode = null,
        };
    }
}