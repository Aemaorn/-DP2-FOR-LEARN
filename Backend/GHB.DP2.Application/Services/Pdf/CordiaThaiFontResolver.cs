namespace GHB.DP2.Application.Services.Pdf;

using PdfSharpCore.Fonts;

/// <summary>
/// Custom font resolver for Thai fonts (Cordia New, TH SarabunNew).
/// </summary>
public class CordiaThaiFontResolver : IFontResolver
{
    private const string CordiaNewFace = "Cordia New";
    private const string THSarabunNewFace = "TH SarabunNew";

    private static readonly Lazy<byte[]> CordiaTTFLazy = new(() => LoadFontData("Cordia New.ttf"), LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<byte[]> SarabunTTFLazy = new(() => LoadFontData("THSarabunNew.ttf"), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Gets the default font name.
    /// </summary>
    public string DefaultFontName => CordiaNewFace;

    private static byte[] LoadFontData(string fileName)
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "fonts", fileName);

        if (!File.Exists(fontPath))
        {
            throw new FileNotFoundException(
                $"Thai font file not found. Please ensure '{fileName}' exists at: {fontPath}",
                fontPath);
        }

        try
        {
            return File.ReadAllBytes(fontPath);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Failed to load Thai font file from: {fontPath}. Error: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Gets the font data for the specified face name.
    /// </summary>
    public byte[] GetFont(string faceName)
    {
        return faceName == THSarabunNewFace
            ? SarabunTTFLazy.Value
            : CordiaTTFLazy.Value;
    }

    /// <summary>
    /// Resolves the typeface for the specified font family and style.
    /// </summary>
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (familyName.Equals(THSarabunNewFace, StringComparison.OrdinalIgnoreCase))
        {
            return new FontResolverInfo(THSarabunNewFace);
        }

        return new FontResolverInfo(CordiaNewFace);
    }
}
