namespace GHB.DP2.Application.Common;

using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

/// <summary>A text run with an optional ARGB hex color (e.g. "FFEF4444"). Null = default/black.</summary>
public record RichTextRun(string Text, string? HexRgbColor = null);

public class ExportExcel : IDisposable
{
    private SpreadsheetDocument doc;
    private WorkbookPart wbPart;
    private Sheets sheetsRoot;

    private readonly Dictionary<string, SheetCtx> sheets = [];
    private string currentSheet; // key into _sheets

    private record StyleIds(
        uint HeaderRed,
        uint HeaderBold,
        uint TextLeft,
        uint Number0,
        uint Number2,
        uint Date,
        uint TextCenter,
        uint HeaderBoldWrapLightBlue,
        uint HeaderGrayRedText,
        uint HeaderLightBlue,
        uint HeaderDarkBlue,
        uint HeaderYellow,
        uint HeaderBoldWrap,
        uint HeaderRedBg,
        uint HeaderCreamBg);

    private StyleIds Styles { get; set; }

    private ExportExcel()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ExportExcel"/> class and initializes it with a new Excel workbook.
    /// </summary>
    /// <param name="stream">The stream where the Excel document will be created. The stream must be writable.</param>
    /// <returns>A new instance of <see cref="ExportExcel"/> initialized with a workbook.</returns>
    public static ExportExcel Create(Stream stream)
    {
        var document = new ExportExcel();
        document.doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true);
        document.wbPart = document.doc.AddWorkbookPart();
        document.wbPart.Workbook = new Workbook();
        document.sheetsRoot = document.wbPart.Workbook.AppendChild(new Sheets());
        document.SetupDefaultStyles();

        return document;
    }

    /// <summary>
    /// Adds a new sheet to the Excel workbook with the specified name and optional configurations for column widths and frozen rows.
    /// </summary>
    /// <param name="name">The name of the sheet to add. The name must be unique and not empty or whitespace.</param>
    /// <param name="columnWidths">An optional array of column widths to set for the sheet. If null or not provided, default widths are applied.</param>
    /// <param name="freezeTopRows">An optional parameter specifying the number of top rows to freeze. If 0, no rows are frozen.</param>
    /// <returns>The current instance of <see cref="ExportExcel"/>, allowing for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the sheet name is null, empty, or consists only of whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a sheet with the same name already exists.</exception>
    public ExportExcel AddSheet(string name, double[]? columnWidths = null, uint freezeTopRows = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Sheet name required.");
        }

        if (this.sheets.ContainsKey(name))
        {
            throw new InvalidOperationException($"Sheet '{name}' already exists. Use SelectSheet() to switch.");
        }

        var wsPart = this.wbPart.AddNewPart<WorksheetPart>();

        var sheetViews = new SheetViews(new SheetView { WorkbookViewId = 0U });

        if (freezeTopRows > 0)
        {
            sheetViews.RemoveAllChildren<SheetView>();
            sheetViews.Append(new SheetView
            {
                WorkbookViewId = 0U,
                Pane = new Pane
                {
                    State = PaneStateValues.Frozen,
                    VerticalSplit = freezeTopRows,
                    TopLeftCell = $"A{freezeTopRows + 1}",
                    ActivePane = PaneValues.BottomLeft,
                },
            });
        }

        var data = new SheetData();
        var merges = new MergeCells();
        var cols =
            columnWidths != null
                ? new Columns()
                : null;

        if (cols != null && columnWidths is not null && columnWidths.Length > 0)
        {
            foreach (var (width, index) in columnWidths.Select((w, i) => (w, i)))
            {
                cols.Append(new Column { Min = (uint)(index + 1), Max = (uint)(index + 1), Width = width, CustomWidth = true });
            }
        }

        wsPart.Worksheet = cols is null
            ? new Worksheet(sheetViews, data)
            : new Worksheet(sheetViews, cols, data);
        wsPart.Worksheet.Append(merges);

        var relId = this.wbPart.GetIdOfPart(wsPart);
        uint sheetId = (uint)(this.sheetsRoot.Count() + 1);
        this.sheetsRoot.Append(new Sheet { Id = relId, SheetId = sheetId, Name = name });

        this.sheets[name] = new SheetCtx
        {
            WsPart = wsPart,
            Data = data,
            Merges = merges,
            Views = sheetViews,
            Cols = cols,
            RowIndex = 1,
        };

        this.currentSheet = name;

        return this;
    }

    /// <summary>
    /// Selects an existing sheet by its name to make it the current sheet for further operations.
    /// </summary>
    /// <param name="name">The name of the sheet to select. The name must correspond to an existing sheet in the document.</param>
    /// <returns>The current <see cref="ExportExcel"/> instance for method chaining.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the specified sheet name does not exist in the document.</exception>
    public ExportExcel SelectSheet(string name)
    {
        if (!this.sheets.ContainsKey(name))
        {
            throw new KeyNotFoundException($"Sheet '{name}' not found.");
        }

        this.currentSheet = name;

        return this;
    }

    /// <summary>
    /// Gets the name of the currently active sheet in the Excel document.
    /// </summary>
    /// <value>
    /// A string representing the name of the currently selected sheet. Returns null or an empty string if no sheet is currently selected.
    /// </value>
    public string CurrentSheetName => this.currentSheet;

    /// <summary>
    /// Sets the column widths for the current Excel sheet.
    /// </summary>
    /// <param name="widths">An array of double values representing the widths for each column. Each width must be greater than zero.</param>
    /// <returns>An instance of <see cref="ExportExcel"/> for further operations.</returns>
    /// <exception cref="ArgumentException">Thrown when any of the specified column widths is less than or equal to zero.</exception>
    public ExportExcel ColumnWidths(params double[] widths)
    {
        var ctx = this.RequireSheet();

        if (ctx.Cols == null)
        {
            ctx.Cols = new Columns();
            ctx.WsPart.Worksheet.InsertAfter(ctx.Cols, ctx.WsPart.Worksheet.GetFirstChild<SheetViews>());
        }

        ctx.Cols.RemoveAllChildren<Column>();

        foreach (var (index, width) in widths.Select((w, i) => (i, w)))
        {
            if (width <= 0)
            {
                throw new ArgumentException("Column widths must be greater than zero.");
            }

            ctx.Cols.Append(
                new Column
                {
                    Min = (uint)(index + 1),
                    Max = (uint)(index + 1),
                    Width = width,
                    CustomWidth = true,
                });
        }

        return this;
    }

    /// <summary>
    /// Freezes the specified number of rows at the top of the current sheet, ensuring they remain visible during vertical scrolling.
    /// </summary>
    /// <param name="rows">The number of rows to freeze at the top of the sheet.</param>
    /// <returns>The current instance of <see cref="ExportExcel"/>, allowing for method chaining.</returns>
    public ExportExcel HideColumns(params int[] columnIndexes)
    {
        var ctx = this.RequireSheet();

        if (ctx.Cols == null)
        {
            ctx.Cols = new Columns();
            ctx.WsPart.Worksheet.InsertAfter(ctx.Cols, ctx.WsPart.Worksheet.GetFirstChild<SheetViews>());
        }

        foreach (var colIndex in columnIndexes)
        {
            var existing = ctx.Cols.Elements<Column>()
                              .FirstOrDefault(c => c.Min != null && c.Max != null && c.Min <= (uint)colIndex && c.Max >= (uint)colIndex);

            if (existing != null)
            {
                existing.Hidden = true;
            }
            else
            {
                ctx.Cols.Append(new Column
                {
                    Min = (uint)colIndex,
                    Max = (uint)colIndex,
                    Hidden = true,
                    CustomWidth = true,
                    Width = 0,
                });
            }
        }

        return this;
    }

    public ExportExcel FreezeTopRows(uint rows)
    {
        var ctx = this.RequireSheet();
        ctx.Views.RemoveAllChildren<SheetView>();
        ctx.Views.Append(new SheetView
        {
            WorkbookViewId = 0U,
            Pane = new Pane
            {
                State = PaneStateValues.Frozen,
                VerticalSplit = rows,
                TopLeftCell = $"A{rows + 1}",
                ActivePane = PaneValues.BottomLeft,
            },
        });

        return this;
    }

    /// <summary>
    /// Adds a new row to the currently selected sheet in the Excel document. Each value provided corresponds
    /// to a cell in the row, added sequentially.
    /// </summary>
    /// <param name="values">An array of objects representing the values to be added in the row's cells. The order of values determines cell placement.</param>
    /// <returns>The current instance of <see cref="ExportExcel"/>, allowing method chaining.</returns>
    public ExportExcel Row(params object[] values)
    {
        var ctx = this.RequireSheet();
        var row = new Row { RowIndex = ctx.RowIndex };

        for (int i = 0; i < values.Length; i++)
        {
            string a1 = A1(i + 1, (int)ctx.RowIndex);
            row.Append(this.MakeCell(a1, values[i], null));
        }

        ctx.Data.Append(row);
        ctx.RowIndex++;

        return this;
    }

    /// <summary>
    /// Adds a header row to the current sheet with a left-aligned title and a list of header entries.
    /// The headers are styled using the predefined header style.
    /// </summary>
    /// <param name="leftTitle">The title displayed in the first column of the header row, aligned to the left.</param>
    /// <param name="headers">A collection of header entries that will be displayed in subsequent columns of the header row.</param>
    /// <returns>The current instance of <see cref="ExportExcel"/> to allow for method chaining.</returns>
    public ExportExcel HeaderRow(string leftTitle, IEnumerable<string> headers)
    {
        var ctx = this.RequireSheet();
        var row = new Row { RowIndex = ctx.RowIndex };
        row.Append(this.MakeCell(A1(1, (int)ctx.RowIndex), leftTitle, this.Styles.HeaderBold));

        foreach (var item in headers.Select((v, i) => (v, i)))
        {
            string a1 = A1(item.i + 2, (int)ctx.RowIndex);
            row.Append(this.MakeCell(a1, item.v, this.Styles.HeaderBold));
        }

        ctx.Data.Append(row);
        ctx.RowIndex++;

        return this;
    }

    /// <summary>
    /// Adds a two-row group header to the current sheet, with a primary title and grouped top and sub headers.
    /// </summary>
    /// <param name="leftTitle">The title to be displayed in the first column spanning across two rows.</param>
    /// <param name="groups">An array of tuples containing the top and sub header values for each group.</param>
    /// <returns>The current instance of <see cref="ExportExcel"/> with the two-row group header added.</returns>
    public ExportExcel TwoRowGroupHeader(string leftTitle, params (string Top, string Sub)[] groups)
    {
        var ctx = this.RequireSheet();
        uint r = ctx.RowIndex;

        var row1 = new Row { RowIndex = r };
        row1.Append(this.MakeCell(A1(1, (int)r), leftTitle, this.Styles.HeaderRed));

        foreach (var (group, index) in groups.Select((g, i) => (g, i)))
        {
            row1.Append(this.MakeCell(A1(index + 2, (int)r), group.Top, this.Styles.HeaderRed));
        }

        ctx.Data.Append(row1);

        var row2 = new Row { RowIndex = r + 1 };
        row2.Append(this.MakeCell(A1(1, (int)r + 1), string.Empty, this.Styles.HeaderRed));

        foreach (var (group, index) in groups.Select((g, i) => (g, i)))
        {
            row2.Append(this.MakeCell(A1(index + 2, (int)r + 1), group.Sub, this.Styles.HeaderRed));
        }

        ctx.Data.Append(row2);

        ctx.Merges.Append(new MergeCell { Reference = $"A{r}:A{r + 1}" });
        ctx.RowIndex += 2;

        return this;
    }

    /// <summary>
    /// Adds a two-level group header to the current sheet. The header consists of a left title followed by grouped column titles, each with sub-columns.
    /// </summary>
    /// <param name="leftTitle">The title of the leftmost column in the group header.</param>
    /// <param name="groups">An array of tuples where each tuple contains a top-level column title and an array of corresponding sub-column titles.</param>
    /// <returns>The current instance of <see cref="ExportExcel"/> for method chaining.</returns>
    public ExportExcel TwoLevelGroupHeader(string leftTitle, params (string Top, string[] Subs)[] groups)
    {
        var ctx = this.RequireSheet();
        uint r = ctx.RowIndex;

        var row1 = new Row { RowIndex = r };
        row1.Append(this.MakeCell(A1(1, (int)r), leftTitle, this.Styles.HeaderRed));

        int col = 2; // start at column B

        foreach (var g in groups)
        {
            int span = Math.Max(1, g.Subs?.Length ?? 1);
            row1.Append(this.MakeCell(A1(col, (int)r), g.Top, this.Styles.HeaderRed));

            for (int k = 1; k < span; k++)
            {
                row1.Append(new Cell { CellReference = A1(col + k, (int)r), StyleIndex = this.Styles.HeaderRed });
            }

            string start = A1(col, (int)r);
            string end = A1(col + span - 1, (int)r);
            ctx.Merges.Append(new MergeCell { Reference = $"{start}:{end}" });
            col += span;
        }

        ctx.Data.Append(row1);

        var row2 = new Row { RowIndex = r + 1 };
        row2.Append(this.MakeCell(A1(1, (int)r + 1), string.Empty, this.Styles.HeaderRed));

        col = 2;

        foreach (var g in groups)
        {
            var subs = (g.Subs.Length == 0) ? [string.Empty] : g.Subs;

            foreach (var sub in subs)
            {
                row2.Append(this.MakeCell(A1(col, (int)r + 1), sub, this.Styles.HeaderRed));
                col++;
            }
        }

        ctx.Data.Append(row2);
        ctx.Merges.Append(new MergeCell { Reference = $"A{r}:A{r + 1}" });
        ctx.RowIndex += 2;

        return this;
    }

    public ExportExcel RowStyled(params (object Value, uint Style)[] cells)
    {
        var ctx = this.RequireSheet();
        var row = new Row { RowIndex = ctx.RowIndex };

        foreach (var (cell, index) in cells.Select((c, i) => (c, i)))
        {
            string a1 = A1(index + 1, (int)ctx.RowIndex);
            row.Append(this.MakeCell(a1, cell.Value, cell.Style));
        }

        ctx.Data.Append(row);
        ctx.RowIndex++;

        return this;
    }

    public ExportExcel RowStyledWithHeight(double height, params (object Value, uint Style)[] cells)
    {
        var ctx = this.RequireSheet();
        var row = new Row
        {
            RowIndex = ctx.RowIndex,
            Height = height,
            CustomHeight = true,
        };

        foreach (var (cell, index) in cells.Select((c, i) => (c, i)))
        {
            string a1 = A1(index + 1, (int)ctx.RowIndex);
            row.Append(this.MakeCell(a1, cell.Value, cell.Style));
        }

        ctx.Data.Append(row);
        ctx.RowIndex++;

        return this;
    }

    /// <summary>
    /// Adds a new row to the currently selected sheet with the specified values, allowing the use of formulas in the cells.
    /// </summary>
    /// <param name="values">The values to insert into the new row. Can include formulas as strings.</param>
    /// <returns>The current instance of <see cref="ExportExcel"/> to allow method chaining.</returns>
    public ExportExcel RowAllowFormula(params object[] values)
    {
        var ctx = this.RequireSheet();
        var row = new Row { RowIndex = ctx.RowIndex };

        foreach (var (value, index) in values.Select((v, i) => (v, i)))
        {
            string a1 = A1(index + 1, (int)ctx.RowIndex);
            row.Append(this.MakeCell(a1, value, null, allowFormula: true));
        }

        ctx.Data.Append(row);
        ctx.RowIndex++;

        return this;
    }

    /// <summary>
    /// Merges the specified range of cells in the currently active worksheet.
    /// </summary>
    /// <param name="a1Range">The range of cells to merge, specified in A1 notation (e.g., "A1:B2").</param>
    /// <returns>An instance of <see cref="ExportExcel"/> to allow method chaining.</returns>
    public ExportExcel Merge(string a1Range)
    {
        var ctx = this.RequireSheet();
        ctx.Merges.Append(new MergeCell { Reference = a1Range });

        return this;
    }

    /// <summary>
    /// Merges the specified cell ranges in the current worksheet.
    /// </summary>
    /// <param name="ranges">An array of cell range references to merge (e.g., "A1:B1").</param>
    /// <returns>The current instance of <see cref="ExportExcel"/> to allow method chaining.</returns>
    public ExportExcel Merges(params string[] ranges)
    {
        var ctx = this.RequireSheet();

        foreach (var r in ranges)
        {
            ctx.Merges.Append(new MergeCell { Reference = r });
        }

        return this;
    }

    /// <summary>
    /// Saves the current state of the Excel document to the underlying stream.
    /// </summary>
    /// <returns>The current instance of <see cref="ExportExcel"/> for method chaining.</returns>
    public ExportExcel Save()
    {
        this.wbPart.Workbook.Save();

        return this;
    }

    // Call this when writing to a Stream to ensure bytes are flushed.
    public void Finish()
    {
        foreach (var sheet in this.sheets.Values)
        {
            if (!sheet.Merges.HasChildren)
            {
                sheet.WsPart.Worksheet.RemoveChild(sheet.Merges);
            }
        }

        this.wbPart.Workbook.Save();
        this.doc.Clone(); // finalize the package, flush to the underlying stream
    }

    /// <summary>
    /// Closes the Excel document and releases all associated resources.
    /// </summary>
    private void Close() => this.doc?.Dispose();

    private bool disposed = false;

    /// <summary>
    /// Releases all resources used by the <see cref="ExportExcel"/> instance and closes the associated Excel document.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ExportExcel"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.Close();
            }

            this.disposed = true;
        }
    }

    private SheetCtx RequireSheet()
    {
        if (this.currentSheet is null)
        {
            throw new InvalidOperationException("No active sheet. Call AddSheet() first.");
        }

        return this.sheets[this.currentSheet];
    }

    private Cell MakeCell(string a1, object? value, uint? styleOverride, bool allowFormula = false)
    {
        if (value is null)
        {
            return new Cell { CellReference = a1, StyleIndex = styleOverride ?? this.Styles.TextLeft };
        }

        if (allowFormula && value is string s && s.StartsWith("="))
        {
            return new Cell
            {
                CellReference = a1,
                CellFormula = new CellFormula(s.Substring(1)),
                StyleIndex = styleOverride ?? this.Styles.Number2,
            };
        }

        switch (value)
        {
            case RichTextRun[] runs:
                var inlineStr = new InlineString();
                foreach (var run in runs)
                {
                    var rProps = new RunProperties();
                    rProps.Append(new RunFont { Val = "Cordia New" });
                    rProps.Append(new FontSize { Val = 14 });
                    if (run.HexRgbColor != null)
                    {
                        rProps.Append(new Color { Rgb = run.HexRgbColor });
                    }

                    var runElem = new Run();
                    runElem.Append(rProps);
                    runElem.Append(new Text(run.Text) { Space = SpaceProcessingModeValues.Preserve });
                    inlineStr.Append(runElem);
                }

                return new Cell
                {
                    CellReference = a1,
                    DataType = CellValues.InlineString,
                    InlineString = inlineStr,
                    StyleIndex = styleOverride ?? this.Styles.TextLeft,
                };

            case string str:
                return new Cell
                {
                    CellReference = a1,
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(str)),
                    StyleIndex = styleOverride ?? this.Styles.TextLeft,
                };

            case DateTime dt:
                return new Cell
                {
                    CellReference = a1,
                    CellValue = new CellValue(dt.ToOADate().ToString(CultureInfo.InvariantCulture)),
                    StyleIndex = styleOverride ?? this.Styles.Date,
                };

            case int or long or short:
                return new Cell
                {
                    CellReference = a1,
                    CellValue = new CellValue(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty),
                    StyleIndex = styleOverride ?? this.Styles.Number0,
                };

            case decimal or double or float:
                return new Cell
                {
                    CellReference = a1,
                    CellValue = new CellValue(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty),
                    StyleIndex = styleOverride ?? this.Styles.Number2,
                };

            case bool b:
                return new Cell
                {
                    CellReference = a1,
                    DataType = CellValues.Boolean,
                    CellValue = new CellValue(b ? "1" : "0"),
                    StyleIndex = styleOverride ?? this.Styles.TextLeft,
                };

            default:
                return new Cell
                {
                    CellReference = a1,
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(value.ToString() ?? string.Empty)),
                    StyleIndex = styleOverride ?? this.Styles.TextLeft,
                };
        }
    }

    private static string A1(int col, int row)
    {
        int dividend = col;
        string columnName = string.Empty;

        while (dividend > 0)
        {
            int modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName + row;
    }

    private void SetupDefaultStyles()
    {
        var stylesPart = this.wbPart.AddNewPart<WorkbookStylesPart>();

        var nfs = new NumberingFormats();
        uint nfDate = 164, nfNum2 = 165, nfNum0 = 166;
        nfs.Append(new NumberingFormat { NumberFormatId = nfDate, FormatCode = StringValue.FromString("yyyy-mm-dd") });
        nfs.Append(new NumberingFormat { NumberFormatId = nfNum2, FormatCode = StringValue.FromString("#,##0.00") });
        nfs.Append(new NumberingFormat { NumberFormatId = nfNum0, FormatCode = StringValue.FromString("0") });

        var fonts = new Fonts(
            new Font(new FontName { Val = "Cordia New" }, new FontSize { Val = 14 }), // 0 default black
            new Font(new Bold(), new FontName { Val = "Cordia New" }, new FontSize { Val = 14 }), // 1 bold black
            new Font(new Bold(), new Color { Rgb = "FFFF0000" }, new FontName { Val = "Cordia New" }, new FontSize { Val = 14 }), // 2 bold red
            new Font(new Color { Rgb = "FFFF0000" }, new FontName { Val = "Cordia New" }, new FontSize { Val = 14 }), // 3 red text (not bold)
            new Font(new Bold(), new Color { Rgb = "FF0070C0" }, new FontName { Val = "Cordia New" }, new FontSize { Val = 14 })); // 4 bold blue

        var fills = new Fills(
            new Fill([new PatternFill { PatternType = PatternValues.None }]), // 0
            new Fill([new PatternFill { PatternType = PatternValues.Gray125 }]), // 1
            new Fill([new PatternFill // 2 Light blue
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = new ForegroundColor { Rgb = "FFCCFFFF" },
            }
            ]),
            new Fill([new PatternFill // 3 Gray
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = new ForegroundColor { Rgb = "FFD3D3D3" },
            }
            ]),
            new Fill([new PatternFill // 4 Dark Blue
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = new ForegroundColor { Rgb = "FF4472C4" },
            }
            ]),
            new Fill([new PatternFill // 5 Yellow
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = new ForegroundColor { Rgb = "FFFFFF00" },
            }
            ]),
            new Fill([new PatternFill // 6 Red
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = new ForegroundColor { Rgb = "FFFF0000" },
            }
            ]),
            new Fill([new PatternFill // 7 Cream/Peach
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = new ForegroundColor { Rgb = "FFFCE4D6" },
            }
            ]));

        var borders = new Borders(
            new Border(),
            new Border(
                new LeftBorder { Style = BorderStyleValues.Thin },
                new RightBorder { Style = BorderStyleValues.Thin },
                new TopBorder { Style = BorderStyleValues.Thin },
                new BottomBorder { Style = BorderStyleValues.Thin },
                new DiagonalBorder()));

        var cfs = new CellFormats();
        cfs.Append(
        [
            new CellFormat(), // 0 default
            new CellFormat // 1 HeaderRed
            {
                FontId = 2, BorderId = 1,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true,
            },
            new CellFormat // 2 HeaderBold
            {
                FontId = 1, BorderId = 1,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true,
            },
            new CellFormat // 3 TextLeft
            {
                BorderId = 1,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center },
                ApplyBorder = true, ApplyAlignment = true,
            },
            new CellFormat // 4 Number0
            {
                BorderId = 1,
                NumberFormatId = 166,
                ApplyNumberFormat = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Right, Vertical = VerticalAlignmentValues.Center },
                ApplyAlignment = true, ApplyBorder = true,
            },
            new CellFormat // 5 Number2
            {
                BorderId = 1,
                NumberFormatId = 165,
                ApplyNumberFormat = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Right, Vertical = VerticalAlignmentValues.Center },
                ApplyAlignment = true, ApplyBorder = true,
            },
            new CellFormat // Date 6
            {
                BorderId = 1,
                NumberFormatId = 164,
                ApplyNumberFormat = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                ApplyAlignment = true, ApplyBorder = true,
            },
            new CellFormat // 7 TextCenter
            {
                BorderId = 1,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                ApplyBorder = true, ApplyAlignment = true,
            },
            new CellFormat // 8 HeaderBoldWrapLightBlue
            {
                FontId = 1, BorderId = 1, FillId = 2, // Add FillId = 2 for light blue background
                Alignment = new Alignment
                {
                    Horizontal = HorizontalAlignmentValues.Center,
                    Vertical = VerticalAlignmentValues.Center,
                    WrapText = true,
                },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true, ApplyFill = true,
            },
            new CellFormat // 9 HeaderBoldLeft
            {
                FontId = 1, BorderId = 1,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true,
            },
            new CellFormat // 10 HeaderGrayRedText (Gray background with red text)
            {
                FontId = 3, BorderId = 1, FillId = 3,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true, ApplyFill = true,
            },
            new CellFormat // 11 HeaderLightBlue
            {
                FontId = 1, BorderId = 1, FillId = 2,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true, ApplyFill = true,
            },
            new CellFormat // 12 HeaderDarkBlue
            {
                FontId = 1, BorderId = 1, FillId = 4,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true, ApplyFill = true,
            },
            new CellFormat // 13 HeaderYellow
            {
                FontId = 1, BorderId = 1, FillId = 5,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true, ApplyFill = true,
            },
            new CellFormat // 14 HeaderBoldWrap
            {
                FontId = 1, BorderId = 1,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center, WrapText = true },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true,
            },
            new CellFormat // 15 HeaderRedBg (red fill, bold blue text)
            {
                FontId = 4, BorderId = 1, FillId = 6,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center, WrapText = true },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true, ApplyFill = true,
            },
            new CellFormat // 16 HeaderCreamBg (cream fill, bold blue text)
            {
                FontId = 4, BorderId = 1, FillId = 7,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center, WrapText = true },
                ApplyFont = true, ApplyBorder = true, ApplyAlignment = true, ApplyFill = true,
            },
            new CellFormat // 17 TextLeftWrap
            {
                BorderId = 1,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Top, WrapText = true },
                ApplyBorder = true, ApplyAlignment = true,
            },
        ]);

        stylesPart.Stylesheet = new Stylesheet(nfs, fonts, fills, borders, cfs);
        stylesPart.Stylesheet.Save();

        this.Styles =
            new StyleIds(
                HeaderRed: 1,
                HeaderBold: 2,
                TextLeft: 3,
                Number0: 4,
                Number2: 5,
                Date: 6,
                TextCenter: 7,
                HeaderBoldWrapLightBlue: 8,
                HeaderGrayRedText: 10,
                HeaderLightBlue: 11,
                HeaderDarkBlue: 12,
                HeaderYellow: 13,
                HeaderBoldWrap: 14,
                HeaderRedBg: 15,
                HeaderCreamBg: 16);
    }

    private class SheetCtx
    {
        public WorksheetPart WsPart { get; init; }

        public SheetData Data { get; init; }

        public MergeCells Merges { get; init; }

        public SheetViews Views { get; init; }

        public Columns? Cols { get; set; }

        private uint rowIndex = 1;

        public uint RowIndex
        {
            get => this.rowIndex;
            set => this.rowIndex = value;
        }
    }
}
