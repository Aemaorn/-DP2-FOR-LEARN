namespace GHB.DP2.Application.Extensions.Document;

using Codehard.OdtKit;
using Codehard.OdtKit.Abstractions;

public sealed class PlaceholderSection
{
    private readonly XElementWrapper section;

    private PlaceholderSection(XElementWrapper section)
    {
        this.section = section;
    }

    public static PlaceholderSection? FromDocument(OdtDocument doc, string sectionName)
    {
        var sectionElement = doc.FindNamedSection(sectionName);

        if (sectionElement is null)
        {
            return null;
        }

        var wrapped = sectionElement.Wrap();

        return new PlaceholderSection(wrapped);
    }

    public IReadOnlyList<XElementWrapper> GetChildContent()
    {
        return this.section.Children;
    }

    public IEnumerable<T> GetChildContent<T>()
        where T : XElementWrapper
    {
        return this.section.Descendants<T>();
    }

    public void ReplaceChildContentFrom(PlaceholderSection source)
    {
        this.section.RemoveNodes();

        foreach (var child in source.GetChildContent())
        {
            this.section.Add(child.Raw.Clone());
        }
    }

    public void AppendChildContentFrom(PlaceholderSection source)
    {
        foreach (var child in source.GetChildContent())
        {
            this.section.Add(child.Raw.Clone());
        }
    }
}