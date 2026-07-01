namespace GHB.DP2.Application.Constants;

public static class JorPor
{
    public static class DefaultDirector
    {
        /// <summary>
        /// Condition business unit code for the default JorPor director.
        /// </summary>
        public const string BusinessUnitCode = "88830";

        /// <summary>
        /// Condition position code for the default JorPor director.
        /// </summary>
        public const string PositionInRefCode = "BP008";
    }

    public static class DefaultSectionHead
    {
        /// <summary>
        /// Condition business unit ID for the default JorPor section head.
        /// </summary>
        public const string BusinessUnitId = "50004839";

        /// <summary>
        /// Condition business unit ID for the default JorPor other section head.
        /// </summary>
        public const string JorPorOtherBusinessUnitId = "50004837";

        /// <summary>
        /// Condition business unit ID for the default JorPor IT section head.
        /// </summary>
        public const string JorPorITBusinessUnitId = "50004838";

        /// <summary>
        /// Condition business unit ID for the default Accounting section head.
        /// </summary>
        public const string JorPorAccountingBusinessUnitId = "50003741";

        /// <summary>
        /// Condition business unit ID for the default JorPor general section head.
        /// </summary>
        public const string GeneralBusinessUnitId = "50004837";

        /// <summary>
        /// Condition position name for the default JorPor section head.
        /// </summary>
        public const string PositionName = "หัวหน้าส่วน";

        /// <summary>
        /// PettyCase department code that triggers auto-assigning the General Section Head as Director
        /// (other PettyCase departments select the assignee from the frontend).
        /// </summary>
        public const string JorPortDepartmentCode = "50004690";
    }
}