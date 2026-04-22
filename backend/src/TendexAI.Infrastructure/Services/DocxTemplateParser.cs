using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Parses DOCX files from EXPRO (هيئة كفاءة الإنفاق) to extract structured booklet content
/// with color-coded text classification per the official usage guide:
///   - Black: Fixed/mandatory text (لا يجوز التغيير)
///   - Green: Editable text within regulatory bounds (يجوز الاستبدال)
///   - Red: Example text (أمثلة يُستأنس بها)
///   - Blue: Internal guidance notes (يجب إزالتها من النسخة المنشورة)
///   - Brackets []: Placeholder fields requiring user input
/// </summary>
public sealed class DocxTemplateParser
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    /// <summary>
    /// Parse a DOCX file stream and return structured template content.
    /// </summary>
    public static TemplateParseResult Parse(Stream docxStream)
    {
        var result = new TemplateParseResult();

        using var archive = new ZipArchive(docxStream, ZipArchiveMode.Read);
        var documentEntry = archive.GetEntry("word/document.xml");
        if (documentEntry is null)
            throw new InvalidOperationException("Invalid DOCX file: missing word/document.xml");

        // Load styles for default color resolution
        var stylesMap = LoadStyles(archive);

        using var docStream = documentEntry.Open();
        var doc = XDocument.Load(docStream);
        var body = doc.Root?.Element(W + "body");
        if (body is null)
            throw new InvalidOperationException("Invalid DOCX file: missing body element");

        var currentSection = new ParsedSection { Title = "غلاف الكراسة", SortOrder = 0 };
        int sectionOrder = 0;
        int paragraphOrder = 0;

        foreach (var element in body.Elements())
        {
            if (element.Name == W + "p")
            {
                var para = element;
                var paraText = GetParagraphText(para);
                if (string.IsNullOrWhiteSpace(paraText))
                    continue;

                var isMainSection = IsMainSectionHeader(paraText);
                var isSubSection = IsSubSectionHeader(para);

                if (isMainSection)
                {
                    if (currentSection.Blocks.Count > 0 || currentSection.Title != "غلاف الكراسة")
                    {
                        result.Sections.Add(currentSection);
                    }

                    sectionOrder++;
                    paragraphOrder = 0;
                    currentSection = new ParsedSection
                    {
                        Title = paraText.Trim(),
                        SortOrder = sectionOrder,
                        IsMainSection = true
                    };
                    continue;
                }

                var blocks = ExtractColoredBlocks(para, stylesMap);
                if (blocks.Count == 0) continue;

                paragraphOrder++;
                currentSection.Blocks.Add(new ParsedBlock
                {
                    Order = paragraphOrder,
                    IsHeading = isSubSection,
                    Text = paraText.Trim(),
                    ColorType = DetermineBlockColor(blocks),
                    Segments = blocks,
                    HasBracketPlaceholders = ContainsBracketPlaceholders(paraText)
                });
                continue;
            }

            if (element.Name == W + "tbl")
            {
                var parsedTable = ExtractTable(element, stylesMap);
                if (parsedTable.Rows.Count == 0)
                {
                    continue;
                }

                result.Tables.Add(parsedTable);
                paragraphOrder++;
                currentSection.Blocks.Add(new ParsedBlock
                {
                    Order = paragraphOrder,
                    IsHeading = false,
                    Text = string.Join(" ", parsedTable.Rows.SelectMany(r => r.Cells).Select(c => c.Text).Where(t => !string.IsNullOrWhiteSpace(t))),
                    ColorType = DetermineBlockColor(parsedTable.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Segments).ToList()),
                    HtmlContent = RenderTableHtml(parsedTable),
                    Segments = []
                });
            }
        }

        if (currentSection.Blocks.Count > 0)
        {
            result.Sections.Add(currentSection);
        }

        return result;
    }

    private static Dictionary<string, string> LoadStyles(ZipArchive archive)
    {
        var stylesMap = new Dictionary<string, string>();
        var stylesEntry = archive.GetEntry("word/styles.xml");
        if (stylesEntry is null) return stylesMap;

        using var stream = stylesEntry.Open();
        var stylesDoc = XDocument.Load(stream);

        foreach (var style in stylesDoc.Descendants(W + "style"))
        {
            var styleId = style.Attribute(W + "styleId")?.Value;
            if (styleId is null) continue;

            var colorEl = style.Descendants(W + "color").FirstOrDefault();
            if (colorEl is not null)
            {
                var colorVal = colorEl.Attribute(W + "val")?.Value;
                if (colorVal is not null)
                    stylesMap[styleId] = colorVal;
            }
        }

        return stylesMap;
    }

    private static string GetParagraphText(XElement para)
    {
        return string.Join("", para.Descendants(W + "t").Select(t => t.Value));
    }

    private static bool IsMainSectionHeader(string text)
    {
        // Match "القسم الأول:" or "القسم الثاني:" etc.
        return Regex.IsMatch(text.Trim(), @"^القسم\s+(الأول|الثاني|الثالث|الرابع|الخامس|السادس|السابع|الثامن|التاسع|العاشر|الحادي عشر|الثاني عشر)");
    }

    private static bool IsSubSectionHeader(XElement para)
    {
        // Check if paragraph style is a heading
        var pStyle = para.Element(W + "pPr")?.Element(W + "pStyle")?.Attribute(W + "val")?.Value;
        if (pStyle is not null && pStyle.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if all runs are bold
        var runs = para.Elements(W + "r").ToList();
        if (runs.Count == 0) return false;

        return runs.All(r =>
        {
            var bold = r.Element(W + "rPr")?.Element(W + "b");
            return bold is not null && bold.Attribute(W + "val")?.Value != "0";
        });
    }

    private static List<TextSegment> ExtractColoredBlocks(XElement para, Dictionary<string, string> stylesMap)
    {
        var segments = new List<TextSegment>();

        // Check paragraph-level style for default color
        var paraStyleId = para.Element(W + "pPr")?.Element(W + "pStyle")?.Attribute(W + "val")?.Value;
        string? paraDefaultColor = null;
        if (paraStyleId is not null && stylesMap.TryGetValue(paraStyleId, out var styleColor))
        {
            paraDefaultColor = styleColor;
        }

        foreach (var run in para.Elements(W + "r"))
        {
            var text = string.Join("", run.Descendants(W + "t").Select(t => t.Value));
            if (string.IsNullOrEmpty(text)) continue;

            // Get run-level color
            var runColor = run.Element(W + "rPr")?.Element(W + "color")?.Attribute(W + "val")?.Value;
            var effectiveColor = runColor ?? paraDefaultColor ?? "000000";

            var colorType = ClassifyColor(effectiveColor);
            var isBold = run.Element(W + "rPr")?.Element(W + "b") is not null;

            segments.Add(new TextSegment
            {
                Text = text,
                ColorHex = effectiveColor,
                ColorType = colorType,
                IsBold = isBold
            });
        }

        return segments;
    }

    private static ExprocColorType ClassifyColor(string hex)
    {
        hex = hex.TrimStart('#').ToUpperInvariant();
        if (hex.Length < 6) hex = hex.PadLeft(6, '0');

        // Parse RGB
        if (!int.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out int r)) r = 0;
        if (!int.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out int g)) g = 0;
        if (!int.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out int b)) b = 0;

        // Black: dark colors
        if (r < 60 && g < 60 && b < 60)
            return ExprocColorType.Black;

        // Auto/default
        if (hex == "000000" || hex == "auto" || hex == "AUTO")
            return ExprocColorType.Black;

        // Green: various shades
        if (g > 100 && r < 120 && b < 120)
            return ExprocColorType.Green;
        if (g > 150 && r < 150)
            return ExprocColorType.Green;

        // Red: various shades
        if (r > 150 && g < 100 && b < 100)
            return ExprocColorType.Red;
        if (r > 200 && g < 60)
            return ExprocColorType.Red;

        // Blue: various shades
        if (b > 150 && r < 100 && g < 120)
            return ExprocColorType.Blue;
        if (b > 100 && r < 80)
            return ExprocColorType.Blue;

        // Default to black for unrecognized colors
        return ExprocColorType.Black;
    }

    private static ExprocColorType DetermineBlockColor(List<TextSegment> segments)
    {
        if (segments.Count == 0) return ExprocColorType.Black;

        // If all segments are the same color, use that
        var colors = segments.Select(s => s.ColorType).Distinct().ToList();
        if (colors.Count == 1) return colors[0];

        // Mixed colors - use the dominant non-black color
        var nonBlack = segments
            .Where(s => s.ColorType != ExprocColorType.Black)
            .GroupBy(s => s.ColorType)
            .OrderByDescending(g => g.Sum(s => s.Text.Length))
            .FirstOrDefault();

        return nonBlack?.Key ?? ExprocColorType.Black;
    }

    private static bool ContainsBracketPlaceholders(string text)
    {
        return Regex.IsMatch(text, @"\[.*?\]");
    }

    private static string RenderTableHtml(ParsedTable table)
    {
        var rows = table.Rows.Select((row, rowIndex) =>
        {
            var cells = row.Cells.Select(cell =>
            {
                var tag = rowIndex == 0 ? "th" : "td";
                var cssClass = cell.ColorType switch
                {
                    ExprocColorType.Black => "expro-fixed",
                    ExprocColorType.Green => "expro-editable",
                    ExprocColorType.Red => "expro-example",
                    ExprocColorType.Blue => "expro-guidance",
                    _ => "expro-fixed"
                };
                var content = string.IsNullOrWhiteSpace(cell.Text)
                    ? "&nbsp;"
                    : System.Net.WebUtility.HtmlEncode(cell.Text);
                return $"<{tag} class=\"{cssClass}\">{content}</{tag}>";
            });
            return $"<tr>{string.Join(string.Empty, cells)}</tr>";
        });

        return $"<div dir=\"rtl\" class=\"overflow-x-auto\"><table class=\"min-w-full border-collapse\">{string.Join(string.Empty, rows)}</table></div>";
    }

    private static ParsedTable ExtractTable(XElement table, Dictionary<string, string> stylesMap)
    {
        var parsedTable = new ParsedTable();

        foreach (var row in table.Elements(W + "tr"))
        {
            var parsedRow = new ParsedTableRow();
            foreach (var cell in row.Elements(W + "tc"))
            {
                var cellText = string.Join(" ", cell.Descendants(W + "t").Select(t => t.Value));
                var cellSegments = new List<TextSegment>();

                foreach (var para in cell.Elements(W + "p"))
                {
                    cellSegments.AddRange(ExtractColoredBlocks(para, stylesMap));
                }

                var cellColor = DetermineBlockColor(cellSegments);
                parsedRow.Cells.Add(new ParsedTableCell
                {
                    Text = cellText.Trim(),
                    ColorType = cellColor,
                    Segments = cellSegments
                });
            }
            parsedTable.Rows.Add(parsedRow);
        }

        return parsedTable;
    }
}

// ═══════════════════════════════════════════════════════════════
//  Result Models
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// EXPRO color classification per the official usage guide.
/// </summary>
public enum ExprocColorType
{
    /// <summary>Fixed/mandatory text - cannot be modified (النصوص الثابتة)</summary>
    Black = 0,
    /// <summary>Editable text within regulatory bounds (نصوص يمكن استبدالها)</summary>
    Green = 1,
    /// <summary>Example text that should be replaced (أمثلة يُستأنس بها)</summary>
    Red = 2,
    /// <summary>Internal guidance notes - must be removed from published version (إرشادات)</summary>
    Blue = 3
}

public sealed class TemplateParseResult
{
    public List<ParsedSection> Sections { get; set; } = [];
    public List<ParsedTable> Tables { get; set; } = [];
}

public sealed class ParsedSection
{
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsMainSection { get; set; }
    public List<ParsedBlock> Blocks { get; set; } = [];
}

public sealed class ParsedBlock
{
    public int Order { get; set; }
    public bool IsHeading { get; set; }
    public string Text { get; set; } = "";
    public string? HtmlContent { get; set; }
    public ExprocColorType ColorType { get; set; }
    public bool HasBracketPlaceholders { get; set; }
    public List<TextSegment> Segments { get; set; } = [];
}

public sealed class TextSegment
{
    public string Text { get; set; } = "";
    public string ColorHex { get; set; } = "000000";
    public ExprocColorType ColorType { get; set; }
    public bool IsBold { get; set; }
}

public sealed class ParsedTable
{
    public List<ParsedTableRow> Rows { get; set; } = [];
}

public sealed class ParsedTableRow
{
    public List<ParsedTableCell> Cells { get; set; } = [];
}

public sealed class ParsedTableCell
{
    public string Text { get; set; } = "";
    public ExprocColorType ColorType { get; set; }
    public List<TextSegment> Segments { get; set; } = [];
}
