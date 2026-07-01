namespace GHB.DP2.Domain.Raws.Constants;

public static class EmployeeConstant
{
    public static class Acting
    {
        /// <summary>
        /// ตำแหน่งหลัก (Primary Position)
        /// </summary>
        public const string Primary = "R";

        /// <summary>
        /// รักษาการแทน (Acting Position)
        /// </summary>
        public const string ActingPosition = "X";

        /// <summary>
        /// ปฏิบัติหน้าที่แทน (Temporary Replacement)
        /// </summary>
        public const string Temporary = "A";
    }

    public static class OrganizationLevel
    {
        /// <summary>
        /// Head Office (สำนักงานใหญ่)
        /// </summary>
        public const string Head = "100";

        /// <summary>
        /// Group (กลุ่ม)
        /// </summary>
        public const string Group = "200";

        /// <summary>
        /// Line/Center (สายงาน/ศูนย์)
        /// </summary>
        public const string Line = "300";

        /// <summary>
        /// Department (ฝ่าย)
        /// </summary>
        public const string Department = "400";

        /// <summary>
        /// Segment (ส่วน)
        /// </summary>
        public const string Segment = "600";

        /// <summary>
        /// Center (ศูนย์)
        /// </summary>
        public const string Center = "401";

        /// <summary>
        /// Zone (โซน)
        /// </summary>
        public const string Zone = "500";

        /// <summary>
        /// Branch (สาขา)
        /// </summary>
        public const string Branch = "601";

        /// <summary>
        /// ลาออก หรือย้าย หรือไม่ใช้งาน
        /// </summary>
        public const string None = "000";

        public static readonly string[] BranchLevels = { Branch, Zone, Segment };
    }

    public static class Position
    {
        /// <summary>
        /// ลาออก หรือย้าย หรือไม่ใช้งาน
        /// </summary>
        public const string Resign = "00000000";
    }

    public static class DefaultGroupCode
    {
        /// <summary>
        /// กลุ่มงานปฏิบัติการ (Operations Group)
        /// </summary>
        public const string Operations = "50000342"; // Replace it with your actual code for Operations Group
    }

    public static class DivisionCode
    {
        /// <summary>
        /// ส่วนบัญชีค่าใช้จ่าย
        /// </summary>
        public const string Accounting = "50003737";
    }

    public static class InReferenceCode
    {
        /// <summary>
        /// รหัสอ้างอิงสำหรับการเข้าร่วมงาน (In Reference Code)
        /// </summary>
        public const string ManagingDirector = "BP002"; // Replace it with your actual code for In Reference
    }

    public static class DefaultJorPor
    {
        /// <summary>
        /// Condition business unit code for the default JorPor director.
        /// </summary>
        public const string BusinessUnitCode = "88830";
    }
}