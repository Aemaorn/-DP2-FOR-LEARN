namespace GHB.DP2.Domain.Raws;

using GHB.DP2.Domain.Raws.Constants;

public class RawEmployeeView
{
    public EmployeeCode EmployeeCode { get; init; }

    public string FullName { get; init; }

    public PositionId PositionId { get; init; }

    public string FullPositionName { get; init; }

    public BusinessUnitId BusinessUnitId { get; init; }

    public string BusinessUnitName { get; init; }
}

public static class EmployeeViewExtensions
{
    public static string SetEmployeeView(this string fullPositionName, BusinessUnitId workBusinessUnitId)
    {
        string pos = fullPositionName.Trim();

        if (workBusinessUnitId.Value == "50003311")
        {
            bool startsWithTarget =
                pos.StartsWith("กรรมการผู้จัดการ", StringComparison.Ordinal) ||
                pos.StartsWith("รองกรรมการผู้จัดการ", StringComparison.Ordinal);

            bool endsWithTarget =
                pos.EndsWith("ศูนย์ข้อมูลอสังหาริมทรัพย์", StringComparison.Ordinal);

            if (startsWithTarget && endsWithTarget)
            {
                return pos.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0] + "รัษาการ";
            }
        }

        return pos;
    }

    // Implement from requirement By.P.Bic
    public static string ConvertPositionName(this RawEmployee employee, BusinessUnitId workBusinessUnitId)
    {
        if (employee.View is null)
        {
            return string.Empty;
        }

        var fullPositionName = employee.View.FullPositionName.Trim();
        var isActing =
            employee.Positions.Any(p =>
                p.Acting == EmployeeConstant.Acting.ActingPosition);

        if (workBusinessUnitId.Value == "50003311" && isActing && fullPositionName.StartsWith("กรรมการผู้จัดการ"))
        {
            return $"{fullPositionName} รักษาการผู้อำนวยการศูนย์ข้อมูลอสังหาริมทรัพย์";
        }

        return fullPositionName;
    }
}