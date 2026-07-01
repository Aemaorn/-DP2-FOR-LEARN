namespace GHB.DP2.Infrastructure.Services.ErmEmployee;

using System.Text.Json.Serialization;

public class ErmEmployeeResult
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; }

    [JsonPropertyName("emp_code")]
    public string EmployeeCode { get; init; }

    [JsonPropertyName("fname_t")]
    public string FirstName { get; init; }

    [JsonPropertyName("lname_t")]
    public string LastName { get; init; }

    [JsonPropertyName("idcard")]
    public string CitizenCardId { get; init; }

    [JsonPropertyName("birthdate")]
    public string? BirthDate { get; init; }

    [JsonPropertyName("grade")]
    public string Grade { get; init; }

    [JsonPropertyName("emp_type")]
    public string EmployeeType { get; init; }

    [JsonPropertyName("position_id")]
    public string PositionId { get; init; }

    [JsonPropertyName("position_name")]
    public string PositionName { get; init; }

    [JsonPropertyName("acting_position")]
    public string ActingPosition { get; init; }

    [JsonPropertyName("manager_emp_id")]
    public string ManagerEmployeeId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; }

    [JsonPropertyName("orglevel")]
    public string OrganizationLevel { get; init; }

    // Organization Level 1
    [JsonPropertyName("objid1")]
    public string OrganizationObjectId1 { get; init; }

    [JsonPropertyName("org_unit_solid1")]
    public string OrganizationUnitSolidId1 { get; init; }

    [JsonPropertyName("short1")]
    public string OrganizationShortName1 { get; init; }

    [JsonPropertyName("org_unit_name1")]
    public string OrganizationUnitName1 { get; init; }

    // Organization Level 2
    [JsonPropertyName("objid2")]
    public string OrganizationObjectId2 { get; init; }

    [JsonPropertyName("org_unit_solid2")]
    public string OrganizationUnitSolidId2 { get; init; }

    [JsonPropertyName("short2")]
    public string OrganizationShortName2 { get; init; }

    [JsonPropertyName("org_unit_name2")]
    public string OrganizationUnitName2 { get; init; }

    // Organization Level 3
    [JsonPropertyName("objid3")]
    public string OrganizationObjectId3 { get; init; }

    [JsonPropertyName("org_unit_solid3")]
    public string OrganizationUnitSolidId3 { get; init; }

    [JsonPropertyName("short3")]
    public string OrganizationShortName3 { get; init; }

    [JsonPropertyName("org_unit_name3")]
    public string OrganizationUnitName3 { get; init; }

    // Organization Level 4
    [JsonPropertyName("objid4")]
    public string OrganizationObjectId4 { get; init; }

    [JsonPropertyName("org_unit_solid4")]
    public string OrganizationUnitSolidId4 { get; init; }

    [JsonPropertyName("short4")]
    public string OrganizationShortName4 { get; init; }

    [JsonPropertyName("org_unit_name4")]
    public string OrganizationUnitName4 { get; init; }

    // Organization Level 5
    [JsonPropertyName("objid5")]
    public string OrganizationObjectId5 { get; init; }

    [JsonPropertyName("org_unit_solid5")]
    public string OrganizationUnitSolidId5 { get; init; }

    [JsonPropertyName("short5")]
    public string OrganizationShortName5 { get; init; }

    [JsonPropertyName("org_unit_name5")]
    public string OrganizationUnitName5 { get; init; }

    // Organization Level 6
    [JsonPropertyName("objid6")]
    public string OrganizationObjectId6 { get; init; }

    [JsonPropertyName("org_unit_solid6")]
    public string OrganizationUnitSolidId6 { get; init; }

    [JsonPropertyName("short6")]
    public string OrganizationShortName6 { get; init; }

    [JsonPropertyName("org_unit_name6")]
    public string OrganizationUnitName6 { get; init; }

    // Organization Level 7
    [JsonPropertyName("objid7")]
    public string OrganizationObjectId7 { get; init; }

    [JsonPropertyName("org_unit_solid7")]
    public string OrganizationUnitSolidId7 { get; init; }

    [JsonPropertyName("short7")]
    public string OrganizationShortName7 { get; init; }

    [JsonPropertyName("org_unit_name7")]
    public string OrganizationUnitName7 { get; init; }

    // Organization Level 8
    [JsonPropertyName("objid8")]
    public string OrganizationObjectId8 { get; init; }

    [JsonPropertyName("org_unit_solid8")]
    public string OrganizationUnitSolidId8 { get; init; }

    [JsonPropertyName("short8")]
    public string OrganizationShortName8 { get; init; }

    [JsonPropertyName("org_unit_name8")]
    public string OrganizationUnitName8 { get; init; }

    // Organization Level 9
    [JsonPropertyName("objid9")]
    public string OrganizationObjectId9 { get; init; }

    [JsonPropertyName("org_unit_solid9")]
    public string OrganizationUnitSolidId9 { get; init; }

    [JsonPropertyName("short9")]
    public string OrganizationShortName9 { get; init; }

    [JsonPropertyName("org_unit_name9")]
    public string OrganizationUnitName9 { get; init; }

    // Date fields
    [JsonPropertyName("start_date")]
    public DateTimeOffset? StartDate { get; init; }

    [JsonPropertyName("stop_date")]
    public DateTimeOffset? StopDate { get; init; }

    [JsonPropertyName("last_action")]
    public DateTimeOffset? LastActionDate { get; init; }

    [JsonPropertyName("data_date")]
    public DateTimeOffset? DataDate { get; init; }
}