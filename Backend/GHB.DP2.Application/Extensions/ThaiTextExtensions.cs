namespace GHB.DP2.Application.Extensions;

using System;
using System.Text;

public static class ThaiTextExtensions
{
    // Text constants
    private const string ThaiMillion = "ล้าน";
    private const string ThaiOne = "หนึ่ง";
    private const string ThaiEt = "เอ็ด";
    private const string ThaiTen = "สิบ";
    private const string ThaiPeriod = "จุด";
    private const string ThaiTwenty = "ยี่สิบ";

    // This array may looks strange. Let's see example:
    // if value is "512", its length is 3
    //              ^-----so at hundreds' place will uses thaiPlaces[3] that is Roi.
    //               ^----at tens' place will be thaiPlaces[2] that is Sib.
    private static readonly string[] ThaiPlaces =
    {
        string.Empty, string.Empty, "สิบ", "ร้อย", "พัน", "หมื่น", "แสน", "ล้าน",
    };

    // Simply the number reading in Thai.
    private static readonly string[] ThaiNumbers =
    {
        "ศูนย์", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า",
    };

    /// <summary>
    /// รูปแบบการใช้คำว่า 'เอ็ด' เมื่อมีค่าหนึ่งที่หลักหน่วย
    /// </summary>
    public enum UsesEt
    {
        /// <summary>
        /// ใช้เอ็ดกับหลักสิบเท่านั้น (ยี่สิบเอ็ด-เก้าสิบเอ็ด)
        /// </summary>
        TensOnly = 0,

        /// <summary>
        /// ใช้เอ็ดเสมอ (รวมถึงร้อยเอ็ด พันเอ็ด ล้านเอ็ด เป็นต้น)
        /// เป็นรูปแบบที่ราชบัณฑิตยสภาแนะนำ
        /// </summary>
        Always = 1,
    }

    public static string ToThaiNumberText(this int value, UsesEt mode = UsesEt.Always)
    {
        if (value == 0)
        {
            return "ศูนย์";
        }

        var result = new StringBuilder();

        if (value < 0)
        {
            result.Append("ลบ");
            value = -value;
        }

        string text = value.ToString();
        string[] parts = Decompose(text);

        if (parts[0].Length > 0)
        {
            SpeakTo(result, parts[0], mode);
            result.Append(ThaiMillion);
        }

        if (parts[1].Length > 0)
        {
            SpeakTo(result, parts[1], mode);
            result.Append(ThaiMillion);
        }

        if (parts[2].Length > 0)
        {
            SpeakTo(result, parts[2], mode);
        }

        if (parts[3].Length > 0)
        {
            SpeakTo(result, parts[3], mode);
        }

        return result.ToString();
    }

    public static string ToThaiNumberText(this decimal? value, UsesEt mode = UsesEt.Always)
    {
        return (value ?? 0m).ToThaiNumberText(mode);
    }

    public static string ToThaiNumberText(this decimal? value, bool isPercent, UsesEt mode = UsesEt.Always)
    {
        return (value ?? 0m).ToThaiNumberText(isPercent, mode);
    }

    /// <summary>
    /// แปลงตัวเลขเป็นคำอ่านภาษาไทย โดยคงเลข "ศูนย์" นำหน้าเมื่อส่วนจำนวนเต็มเป็น 0
    /// และตัด trailing zeros ออกจากทศนิยม เช่น 0.50 → "ศูนย์จุดห้า"
    /// </summary>
    public static string ToThaiNumberText(this decimal value, bool isPercent, UsesEt mode = UsesEt.Always)
    {
        if (!isPercent)
        {
            return value.ToThaiNumberText(mode);
        }

        if (value == 0m)
        {
            return "ศูนย์";
        }

        var result = new StringBuilder();

        if (value < 0)
        {
            result.Append("ลบ");
            value = -value;
        }

        string text = value.ToString("0.000");
        string[] parts = Decompose(text);

        if (parts[0].Length > 0)
        {
            SpeakTo(result, parts[0], mode);
            result.Append(ThaiMillion);
        }

        if (parts[1].Length > 0)
        {
            SpeakTo(result, parts[1], mode);
            result.Append(ThaiMillion);
        }

        if (parts[2].Length > 0)
        {
            SpeakTo(result, parts[2], mode);
        }
        else if (parts[0].Length == 0 && parts[1].Length == 0)
        {
            result.Append("ศูนย์");
        }

        if (parts[3].Length > 0)
        {
            var trimmed = parts[3].TrimEnd('0');
            if (trimmed.Length > 0)
            {
                SpeakDotTo(result, trimmed);
            }
        }

        return result.ToString();
    }

    public static string ToThaiNumberText(this decimal value, UsesEt mode = UsesEt.Always)
    {
        if (value == 0m)
        {
            return "ศูนย์";
        }

        var result = new StringBuilder();

        if (value < 0)
        {
            result.Append("ลบ");
            value = -value;
        }

        string text = value.ToString("0.00");
        string[] parts = Decompose(text);

        if (parts[0].Length > 0)
        {
            SpeakTo(result, parts[0], mode);
            result.Append(ThaiMillion);
        }

        if (parts[1].Length > 0)
        {
            SpeakTo(result, parts[1], mode);
            result.Append(ThaiMillion);
        }

        if (parts[2].Length > 0)
        {
            SpeakTo(result, parts[2], mode);
        }

        if (parts[3].Length > 0)
        {
            SpeakDotTo(result, parts[3]);
        }

        return result.ToString();
    }

    private static string[] Decompose(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string s1 = string.Empty;
        string s2 = string.Empty;
        string s3;
        string s4;
        int position;

        position = text.IndexOf('.') < 0 ? text.Length : text.IndexOf('.');

        s3 = text.Substring(0, position);
        s4 = text.IndexOf('.') < 0 ? "00" : text.Substring(position + 1);

        if (s4 == "00")
        {
            s4 = string.Empty;
        }

        int length = s3.Length;

        if (length > 6)
        {
            s2 = s3.Substring(0, length - 6);
            s3 = s3.Substring(length - 6);
        }

        length = s2.Length;

        if (length > 6)
        {
            s1 = s2.Substring(0, length - 6);
            s2 = s2.Substring(length - 6);
        }

        if ((s3.Length > 0) && (int.Parse(s3) == 0))
        {
            s3 = string.Empty;
        }

        return new[] { s1, s2, s3, s4 };
    }

    private static void SpeakDotTo(StringBuilder sb, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(sb);

        sb.Append(ThaiPeriod);
        for (int i = 0; i < text.Length; i++)
        {
            int c = int.Parse(text[i].ToString());
            sb.Append(ThaiNumbers[c]);
        }
    }

    private static void SpeakTo(StringBuilder sb, string text, UsesEt mode)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(sb);

        int length = text.Length;
        int c = 0;
        int lastc = -1;
        bool negative = false;

        for (int i = 0; i < length; i++)
        {
            if (text[i] == '-')
            {
                negative = true;
            }
            else
            {
                c = int.Parse(text[i].ToString());

                if ((i == length - 1) && (c == 1))
                {
                    if (length == 1 // 1
                        ||
                        (negative && length == 2) // -1
                        ||
                        (length == 2 && lastc == 0) // 01 (satang)
                       )
                    {
                        sb.Append(ThaiOne);

                        return;
                    }

                    if (mode == UsesEt.Always)
                    {
                        sb.Append(ThaiEt);
                    }
                    else
                    {
                        // if (mode == UsesEt.TensOnly) {
                        if (lastc == 0)
                        {
                            sb.Append(ThaiOne);
                        }
                        else
                        {
                            sb.Append(ThaiEt);
                        }
                    }
                }
                else if ((i == length - 2) && (c == 2))
                {
                    sb.Append(ThaiTwenty);
                }
                else if ((i == length - 2) && (c == 1))
                {
                    sb.Append(ThaiTen);
                }
                else if (c != 0)
                {
                    sb.Append(ThaiNumbers[c] + ThaiPlaces[length - i]);
                }
            }

            lastc = c;
        }
    }
}