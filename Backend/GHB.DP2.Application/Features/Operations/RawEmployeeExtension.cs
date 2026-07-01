namespace GHB.DP2.Application.Features.Operations;

using GHB.DP2.Application.Features.Operations.Dto;
using GHB.DP2.Domain.Raws;

public static class InRefCodeConstant
{
    /// <summary>กรรมการธนาคาร</summary>
    public const string Bp001 = "BP001";

    /// <summary>กรรมการผู้จัดการ</summary>
    public const string Bp002 = "BP002";

    public static readonly List<string> MD = new List<string> { Bp001, Bp002 };

    /// <summary>รองกรรมการผู้จัดการ</summary>
    public const string Bp003 = "BP003";

    public const string Bp004 = "BP004";

    /// <summary>ผู้อำนวยการศูนย์ข้อมูลอสังหาริมทรัพย์</summary>
    public const string Bp005 = "BP005";

    /// <summary>ผู้ช่วยกรรมการผู้จัดการ</summary>
    public const string Bp006 = "BP006";

    /// <summary>ผู้อำนวยการฝ่ายสื่อสารองค์กร</summary>
    public const string Bp007 = "BP007";

    /// <summary>ผู้อำนวยการฝ่ายจัดหาและการพัสดุ</summary>
    public const string Bp008 = "BP008";

    /// <summary>ผู้อำนวยการฝ่าย (อื่นๆ)</summary>
    public const string Bp009 = "BP009";

    /// <summary>ผู้อำนวยการสำนัก</summary>
    public const string Bp010 = "BP010";

    /// <summary>ผู้ช่วยผู้อำนวยการฝ่าย (ฝ่ายจัดหาและการพัสดุ / ฝ่ายการบัญชี)</summary>
    public const string Bp011 = "BP011";

    /// <summary>หัวหน้าส่วนจัดหาทั่วไป (ฝ่ายจัดหาและการพัสดุ)</summary>
    public const string Bp012 = "BP012";

    /// <summary>หัวหน้าส่วนจัดหาระบบเทคโนโลยีสารสนเทศฯ (ฝ่ายจัดหาและการพัสดุ)</summary>
    public const string Bp013 = "BP013";

    /// <summary>ผู้อำนวยการภาค</summary>
    public const string Bp014 = "BP014";

    /// <summary>ผู้อำนวยการศูนย์วิเคราะห์สินเชื่อ</summary>
    public const string Bp015 = "BP015";

    /// <summary>ผู้จัดการเขต</summary>
    public const string Bp016 = "BP016";

    /// <summary>ผู้จัดการสาขา</summary>
    public const string Bp017 = "BP017";

    /// <summary>ผู้จัดการสาขาอาวุโส</summary>
    public const string Bp018 = "BP018";

    /// <summary>ผู้จัดการ DEC (ผู้จัดการศูนย์วิเคราะห์สินเชื่อ)</summary>
    public const string Bp019 = "BP019";

    /// <summary>ผู้อำนวยการฝ่ายบริหารสำนักงานและกิจการสาขา</summary>
    public const string Bp020 = "BP020";

    /// <summary>ผู้อำนวยการฝ่ายบริหาร NPA</summary>
    public const string Bp021 = "BP021";

    public const string Bp022 = "BP022";

    /// <summary>หัวหน้าส่วนบัญชีค่าใช้จ่าย (ฝ่ายการบัญชี)</summary>
    public const string Bp023 = "BP023";

    /// <summary>ผู้ช่วยกรรมการผู้จัดการ สายงานสนับสนุน</summary>
    public const string Bp024 = "BP024";

    /// <summary>ผู้ช่วยกรรมการผู้จัดการ สายงานสื่อสารการตลาดและภาพลักษณ์องค์กร</summary>
    public const string Bp025 = "BP025";

    /// <summary>ผู้อำนวยการฝ่ายบริหารหนี้ภูมิภาค 1</summary>
    public const string Bp026 = "BP026";

    /// <summary>ผู้อำนวยการฝ่ายบริหารหนี้ภูมิภาค 2</summary>
    public const string Bp027 = "BP027";

    /// <summary>ผู้อำนวยการฝ่ายสื่อสารการตลาด</summary>
    public const string Bp028 = "BP028";
}

public static class RawEmployeeExtension
{
    public static OperationInfo CreateOperationInfo(
        this RawEmployee employee,
        ref BusinessUnitId? parentBusinessUnitId,
        string? commandText = null,
        decimal? commandBudget = null,
        string? inRefCode = null)
    {
        var user = employee.Users.FirstOrDefault()!;

        var businessUnitIdFromCommand = parentBusinessUnitId;

        var businessUnit =
            parentBusinessUnitId is null
                ? employee.PrimaryBusinessUnit
                : employee.Positions
                          .FirstOrDefault(p =>
                              p.BusinessUnitId == businessUnitIdFromCommand ||
                              p.BusinessUnit.ParentId == businessUnitIdFromCommand)?
                          .BusinessUnit;

        parentBusinessUnitId = businessUnit?.ParentId;

        return new OperationInfo(
            user.Id,
            employee.View!.EmployeeCode,
            employee.FirstName + " " + employee.LastName,
            employee.View!.FullName,
            employee.View.PositionId,
            employee.View.FullPositionName,
            int.Parse(ResolveOrganizationLevel(employee, inRefCode)),
            businessUnit?.Id ?? employee.PrimaryBusinessUnit!.Id,
            businessUnit?.Name ?? employee.PrimaryBusinessUnit!.Name,
            commandText,
            commandBudget,
            inRefCode);
    }

    public static OperationInfo CreateOperationInfo(
        this RawEmployee employee,
        string? commandText = null,
        decimal? commandBudget = null,
        string? inRefCode = null)
    {
        var user = employee.Users.FirstOrDefault()!;

        return new OperationInfo(
            user.Id,
            employee.View!.EmployeeCode,
            employee.FirstName + " " + employee.LastName,
            employee.View!.FullName,
            employee.View.PositionId,
            employee.View.FullPositionName,
            int.Parse(ResolveOrganizationLevel(employee, inRefCode)),
            employee.View.BusinessUnitId,
            employee.View.BusinessUnitName,
            commandText,
            commandBudget,
            inRefCode);
    }

    private static string ResolveOrganizationLevel(RawEmployee employee, string? inRefCode)
    {
        if (string.IsNullOrEmpty(inRefCode))
        {
            return employee.PrimaryBusinessUnit!.OrganizationLevel;
        }

        var matchedLevel = employee.Positions
                                   .FirstOrDefault(p => p.Position?.InRefCode == inRefCode)
                                   ?.BusinessUnit?.OrganizationLevel;

        return matchedLevel ?? employee.PrimaryBusinessUnit!.OrganizationLevel;
    }

    public static OperationPositionInfo CreateOperationPositionInfo(
        this SuSectionEmployeeDto? employee,
        string? commandNumber = null)
    {
        return new OperationPositionInfo(
            MatchingInRefCode(employee?.InRefCode, employee?.ApproverPositionName, employee?.ApproverBusinessUnitName),
            null,
            null,
            employee?.Budget ?? 0,
            employee?.InRefCode,
            null,
            commandNumber,
            employee?.CommandBudget ?? 0);
    }

    /// <summary>
    /// ตรวจสอบว่าเป็นผู้ช่วยกรรมการผู้จัดการหรือไม่ (BP006, BP024, BP025)
    /// </summary>
    /// <param name="inRefCode">รหัสอ้างอิงตำแหน่ง</param>
    /// <returns>true ถ้าเป็นผู้ช่วยกรรมการผู้จัดการ</returns>
    public static bool IsAssistantManagingDirector(this string? inRefCode)
    {
        return inRefCode is InRefCodeConstant.Bp006
                            or InRefCodeConstant.Bp024
                            or InRefCodeConstant.Bp025;
    }

    /// <summary>
    /// ตรวจสอบว่าเป็นผู้อำนวยการฝ่ายหรือไม่ (BP007, BP008, BP010, BP020, BP021, BP026)
    /// </summary>
    /// <param name="inRefCode">รหัสอ้างอิงตำแหน่ง</param>
    /// <returns>true ถ้าเป็นผู้อำนวยการฝ่าย</returns>
    public static bool IsDepartmentDirectorIgnoreBP009(this string? inRefCode)
    {
        return inRefCode is InRefCodeConstant.Bp007
                            or InRefCodeConstant.Bp008
                            or InRefCodeConstant.Bp010
                            or InRefCodeConstant.Bp020
                            or InRefCodeConstant.Bp021
                            or InRefCodeConstant.Bp026
                            or InRefCodeConstant.Bp027
                            or InRefCodeConstant.Bp028;
    }

    private static string? MatchingInRefCode(string? inRefCode, string? positionName, string? buName)
    {
        switch (inRefCode)
        {
            case InRefCodeConstant.Bp001:
            case InRefCodeConstant.Bp002:
                return positionName;

            case InRefCodeConstant.Bp003:
                return string.Concat(positionName, " ", buName);

            case InRefCodeConstant.Bp005:
                return positionName;

            case InRefCodeConstant.Bp006:
            case InRefCodeConstant.Bp024:
            case InRefCodeConstant.Bp025:
                return string.Concat(positionName, " ", buName);

            case InRefCodeConstant.Bp007:
            case InRefCodeConstant.Bp008:
            case InRefCodeConstant.Bp009:
            case InRefCodeConstant.Bp010:
            case InRefCodeConstant.Bp011:
            case InRefCodeConstant.Bp020:
            case InRefCodeConstant.Bp021:
            case InRefCodeConstant.Bp026:
            case InRefCodeConstant.Bp027:
            case InRefCodeConstant.Bp028:
                return string.Concat(positionName?.Replace("ฝ่าย", string.Empty), buName);

            case InRefCodeConstant.Bp012:
            case InRefCodeConstant.Bp013:
            case InRefCodeConstant.Bp023:
                return string.Concat(positionName?.Replace("ส่วน", string.Empty), buName);

            case InRefCodeConstant.Bp014:
                return string.Concat(positionName?.Replace("ภาค", string.Empty), buName);

            case InRefCodeConstant.Bp015:
                return positionName;

            case InRefCodeConstant.Bp016:
                return string.Concat(positionName?.Replace("เขต", string.Empty), buName);

            case InRefCodeConstant.Bp017:
                return string.Concat(positionName?.Replace("สาขา", string.Empty), buName);

            case InRefCodeConstant.Bp018:
            case InRefCodeConstant.Bp019:
            case InRefCodeConstant.Bp022:
                return positionName;

            default:
                return positionName;
        }
    }
}