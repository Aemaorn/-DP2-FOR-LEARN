namespace GHB.DP2.Application.Extensions.Document;

using System.Xml.Linq;
using Codehard.OdtKit.Abstractions;
using Codehard.OdtKit.Representations.Texts;

public sealed class PredefinedTextElement : TextElement
{
    private PredefinedTextElement(XElement element, Dictionary<XElement, XElementWrapper> cache)
        : base(element, cache)
    {
    }

    protected override string LocalName => "span";

    private static XElement Render(string value)
    {
        XNamespace textNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

        var span = new XElement(
            textNs + "span",
            new XAttribute(textNs + "style-name", "P18"));

        var lines = value.Split(["\r\n", "\n"], StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                span.Add(new XElement(textNs + "line-break"));
            }

            AddLineWithTabs(span, lines[i], textNs);
        }

        return span;
    }

    private static void AddLineWithTabs(XElement parent, string line, XNamespace textNs)
    {
        var parts = line.Split('\t');

        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                parent.Add(new XElement(textNs + "tab"));
            }

            if (parts[i].Length > 0)
            {
                parent.Add(parts[i]);
            }
        }
    }

    public static TextElement Create(string value)
    {
        return new PredefinedTextElement(Render(value), new());
    }
}