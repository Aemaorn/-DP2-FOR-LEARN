namespace GHB.DP2.Application.Extensions.Document;

using System.Reflection;
using System.Xml.Linq;
using Codehard.OdtKit;
using Codehard.OdtKit.Abstractions;
using Codehard.OdtKit.Models;
using Codehard.OdtKit.Representations.Tables;
using Codehard.OdtKit.Representations.Texts;

public sealed class PlaceholderTable : TableElement
{
    public PlaceholderTable(TableElement table)
        : base(table.Raw, GetInternalCache(table))
    {
    }

    public void InsertValues(object obj)
    {
        var values =
            obj.ExtractPlaceholders().ToList();
        var lastRowWithPlaceholder = this.Descendants<TableRowElement>()
                                         .FirstOrDefault(tr => tr.Descendants<PlaceholderElement>().Any());

        if (lastRowWithPlaceholder is null)
        {
            return;
        }

        var columnCount = this.Descendants<TableColumnElement>().Count();
        var allCells = lastRowWithPlaceholder.Descendants<TableCellElement>().ToList();

        var cellWithPlaceholder = FindCellWithMatchingPlaceholders(allCells, values);

        if (cellWithPlaceholder is null)
        {
            return;
        }

        var indexOfCell = allCells.IndexOf(cellWithPlaceholder);

        if (indexOfCell == columnCount - 1)
        {
            AddNewRowWithPlaceholders(lastRowWithPlaceholder, cellWithPlaceholder, columnCount);
        }
        else
        {
            ExtendCurrentRowWithPlaceholders(lastRowWithPlaceholder, cellWithPlaceholder, indexOfCell, columnCount);
        }

        ReplacePlaceholdersWithValues(cellWithPlaceholder, values);

        static TableCellElement? FindCellWithMatchingPlaceholders(
            IList<TableCellElement> cells,
            IList<PlaceholderReplacementValue> values)
        {
            return cells.SingleOrDefault(cell =>
                cell.Descendants<PlaceholderElement>()
                    .Select(p => p.Key)
                    .Intersect(values.Select(v => v.Key))
                    .Count() == values.Count);
        }

        static void AddNewRowWithPlaceholders(TableRowElement currentRow, TableCellElement placeholderCell, int columnCount)
        {
            var templateRow = currentRow.Clone();
            templateRow.RemoveNodes();
            templateRow.Add(placeholderCell.Clone());

            var emptyCell = CreateEmptyCell();

            for (var i = 1; i < columnCount; i++)
            {
                templateRow.Add(emptyCell);
            }

            currentRow.AddAfterSelf(templateRow);
        }

        static void ExtendCurrentRowWithPlaceholders(
            TableRowElement currentRow,
            TableCellElement placeholderCell,
            int startIndex,
            int columnCount)
        {
            var nextCell = placeholderCell.AddAfterSelf(placeholderCell.Clone());
            var emptyCell = CreateEmptyCell();

            for (var i = startIndex + 1; i < columnCount - 1; i++)
            {
                nextCell.AddAfterSelf(emptyCell);
            }

            var excessCells =
                currentRow.Descendants<TableCellElement>()
                          .Where((_, idx) => idx >= columnCount);

            foreach (var cell in excessCells)
            {
                cell.Remove();
            }
        }

        static void ReplacePlaceholdersWithValues(
            TableCellElement cellWithPlaceholder,
            IList<PlaceholderReplacementValue> values)
        {
            foreach (var (key, value) in values)
            {
                var placeholder = cellWithPlaceholder.Descendants<PlaceholderElement>()
                                                     .Single(p => p.Key == key);

                var attributes = placeholder.Raw.Attributes()
                                            .Where(a => !a.Name.LocalName.Contains("placeholder"));

                var span = XElement.Parse(
                    $"""
                     <root xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">
                        <text:span {string.Join(" ", attributes)}>{value}</text:span>
                     </root>
                     """);

                placeholder.ReplaceWith(span.Descendants().Single());
            }
        }

        static XElement CreateEmptyCell()
        {
            return XElement.Parse(
                """
                <table-cell xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0">
                </table-cell>
                """);
        }
    }

    public void RemovePlaceholders()
    {
        var placeholders = this.Descendants<PlaceholderElement>();

        foreach (var placeholder in placeholders)
        {
            placeholder.Remove();
        }
    }

    private static IDictionary<XElement, XElementWrapper> GetInternalCache(TableElement table)
    {
        return (IDictionary<XElement, XElementWrapper>)typeof(XElementWrapper).GetField("cache", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(table)!;
    }
}