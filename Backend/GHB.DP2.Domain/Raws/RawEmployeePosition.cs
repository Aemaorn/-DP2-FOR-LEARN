namespace GHB.DP2.Domain.Raws;

public class RawEmployeePosition
{
    public EmployeeCode EmployeeCode { get; init; }

    public PositionId PositionId { get; init; }

    public BusinessUnitId BusinessUnitId { get; init; }

    public string EmployeeType { get; private set; }

    public string Acting { get; private set; }

    public EmployeeCode? ManagerEmployeeCode { get; private set; }

    public string? Remark { get; init; }

    public virtual RawEmployee Employee { get; init; }

    public virtual RawEmployee Manager { get; init; }

    public virtual RawPosition Position { get; init; }

    public virtual RawBusinessUnit BusinessUnit { get; init; }

    public RawEmployeePosition SetManager(EmployeeCode? managerEmployeeCode)
    {
        this.ManagerEmployeeCode = managerEmployeeCode;

        return this;
    }

    public RawEmployeePosition SetEmployeeType(
        string employeeType)
    {
        this.EmployeeType = employeeType;

        return this;
    }

    public static RawEmployeePosition Create(
        string employeeCode,
        string positionId,
        string businessUnitId,
        string employeeType,
        string acting)
    {
        return new RawEmployeePosition
        {
            EmployeeCode = EmployeeCode.From(employeeCode),
            PositionId = PositionId.From(positionId),
            BusinessUnitId = BusinessUnitId.From(businessUnitId),
            EmployeeType = employeeType,
            Acting = acting,
        };
    }
}