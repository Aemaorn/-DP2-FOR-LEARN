namespace GHB.DP2.Application.JopService;

public class TimeZoneHelper
{
    public TimeZoneInfo AsiaBangkok { get; }

    public TimeZoneHelper()
    {
        this.AsiaBangkok =
            Try("Asia/Bangkok") ?? Try("SE Asia Standard Time") ?? TimeZoneInfo.Utc;

        static TimeZoneInfo? Try(string id)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch
            {
                return null;
            }
        }
    }
}