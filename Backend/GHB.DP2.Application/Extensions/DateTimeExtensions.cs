namespace GHB.DP2.Application.Extensions;

using System.Globalization;
using System.Text;
using LanguageExt;

public static class DateTimeExtensions
{
    public static readonly ThaiBuddhistCalendar ThaiBuddhistCalendar = new();

    public static string ToThaiDateString(
        this DateTime dateTime,
        string format = "d MMMM yyyy",
        bool thaiNumber = false,
        bool includeBuddhistEra = false)
    {
        // Create Thai culture with Buddhist calendar
        var thaiCulture = new CultureInfo("th-TH");
        thaiCulture.DateTimeFormat.Calendar = new ThaiBuddhistCalendar();

        var dateText = dateTime.ToString(format, thaiCulture);

        if (includeBuddhistEra && format.Contains("yyyy"))
        {
            dateText = dateText.Replace(
                " " + thaiCulture.DateTimeFormat.Calendar.GetYear(dateTime),
                " พ.ศ. " + thaiCulture.DateTimeFormat.Calendar.GetYear(dateTime));
        }

        return thaiNumber ? dateText.ToThaiNumber() : dateText;
    }

    public static string ToThaiDateString(
        this DateTimeOffset dateTimeOffset,
        string format = "d MMMM yyyy",
        bool thaiNumber = false,
        bool includeBuddhistEra = false)
    {
        // Create Thai culture with Buddhist calendar
        var thaiCulture = new CultureInfo("th-TH");
        thaiCulture.DateTimeFormat.Calendar = new ThaiBuddhistCalendar();

        var thaiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var thaiDateTime = TimeZoneInfo.ConvertTime(dateTimeOffset, thaiTimeZone);
        var dateText = thaiDateTime.ToString(format, thaiCulture);

        if (includeBuddhistEra && format.Contains("yyyy"))
        {
            dateText = dateText.Replace(
                " " + thaiCulture.DateTimeFormat.Calendar.GetYear(dateTimeOffset.DateTime),
                " พ.ศ. " + thaiCulture.DateTimeFormat.Calendar.GetYear(dateTimeOffset.DateTime));
        }

        return thaiNumber ? dateText.ToThaiNumber() : dateText;
    }

    public static string ToThaiDateString(
        this DateTime? dateTime,
        string format = "d MMMM yyyy",
        bool thaiNumber = false,
        bool includeBuddhistEra = false)
    {
        if (dateTime.IsNull())
        {
            return string.Empty;
        }

        return dateTime!.Value.ToThaiDateString(format, thaiNumber, includeBuddhistEra);
    }

    public static string ToThaiDateString(
        this DateTimeOffset? dateTimeOffset,
        string format = "d MMMM yyyy",
        bool thaiNumber = false,
        bool includeBuddhistEra = false)
    {
        if (dateTimeOffset.IsNull())
        {
            return string.Empty;
        }

        return dateTimeOffset!.Value.ToThaiDateString(format, thaiNumber, includeBuddhistEra);
    }

    public static DateTimeOffset? NullIfInfinity(this DateTimeOffset? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value == DateTimeOffset.MaxValue || value == DateTimeOffset.MinValue)
        {
            return null;
        }

        return value;
    }

    public static DateTimeOffset? NullIfInfinity(this DateTimeOffset value)
    {
        if (value == DateTimeOffset.MaxValue || value == DateTimeOffset.MinValue)
        {
            return null;
        }

        return value;
    }

    public static string ToThaiDateDiffLabel(
        this DateTimeOffset start,
        DateTimeOffset end)
    {
        if (end < start)
        {
            (start, end) = (end, start);
        }

        DateTime startDate = start.Date;
        DateTime endDate = end.Date;

        var calendar = new GregorianCalendar();

        int years = calendar.GetYear(endDate) - calendar.GetYear(startDate);
        int months = calendar.GetMonth(endDate) - calendar.GetMonth(startDate);
        int days = endDate.Day - startDate.Day;

        if (days < 0)
        {
            months--;
            var prevMonth = calendar.AddMonths(endDate, -1);
            days += calendar.GetDaysInMonth(prevMonth.Year, prevMonth.Month);
        }

        if (months < 0)
        {
            years--;
            months += 12;
        }

        var sb = new StringBuilder();

        if (years > 0)
        {
            sb.Append($"{years} ปี ");
        }

        if (months > 0)
        {
            sb.Append($"{months} เดือน ");
        }

        if (days > 0)
        {
            sb.Append($"{days} วัน");
        }

        return sb.ToString().Trim();
    }
}