namespace GHB.DP2.Application.Extensions.Document;

public record SectionApprove(string? Name);

public static class DearFormattedPositionApprover
{
    public static IEnumerable<SectionApprove> GetDearFormattedApproverList(
        this IEnumerable<SectionApprove> sectionApprovers,
        string? dear = null)
    {
        var approvers = sectionApprovers.Select(sa => sa.Name);
        var formattedApprovers = approvers
                                 .Select((approver, index) => index == 0 && dear != null ? new SectionApprove(approver) : new SectionApprove($"{dear} {approver}"))
                                 .ToList();

        return formattedApprovers;
    }
}