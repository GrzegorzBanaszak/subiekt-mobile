using System.Globalization;
using System.Text;
using SubiektMobile.Application.Pallets;

namespace SubiektMobile.Infrastructure.Pallets;

/// <summary>Produces the fixed 100 by 150 mm pallet label PDF without a general-purpose PDF dependency.</summary>
public sealed class PalletLabelPdfRenderer : IPalletLabelPdfRenderer
{
    private const decimal PageWidth = 283.465m;
    private const decimal PageHeight = 425.197m;
    private const decimal Left = 18m;
    private const decimal Right = 18m;
    private static readonly IReadOnlyDictionary<char, byte> PolishCharacters = new Dictionary<char, byte>
    {
        ['Ą'] = 128, ['ą'] = 129, ['Ć'] = 130, ['ć'] = 131,
        ['Ę'] = 132, ['ę'] = 133, ['Ł'] = 134, ['ł'] = 135,
        ['Ń'] = 136, ['ń'] = 137, ['Ś'] = 138, ['ś'] = 139,
        ['Ź'] = 140, ['ź'] = 141, ['Ż'] = 142, ['ż'] = 143
    };

    public byte[] Render(PalletLabelPreviewDto label, PalletLabelLanguage language)
    {
        ArgumentNullException.ThrowIfNull(label);
        var pages = Paginate(label.Items);
        var content = pages.Select((items, index) => BuildPage(label, items, index + 1, pages.Count,
            index == pages.Count - 1, language)).ToList();
        return BuildPdf(content);
    }

    private static IReadOnlyList<IReadOnlyList<LabelLine>> Paginate(IReadOnlyList<PalletLabelItemDto> items)
    {
        var lines = items.SelectMany(item => WrapItem(item)).ToList();
        var pages = new List<IReadOnlyList<LabelLine>>();
        while (lines.Count > 0)
        {
            const int lastPageCapacity = 9;
            const int regularPageCapacity = 16;
            var take = lines.Count <= lastPageCapacity
                ? lines.Count
                : Math.Min(regularPageCapacity, lines.Count - lastPageCapacity);
            pages.Add(lines.Take(take).ToList());
            lines.RemoveRange(0, take);
        }

        return pages.Count == 0 ? [[]] : pages;
    }

    private static IEnumerable<LabelLine> WrapItem(PalletLabelItemDto item)
    {
        var words = item.ProductName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        var first = true;
        foreach (var word in words.DefaultIfEmpty("-"))
        {
            if (current.Length > 0 && current.Length + word.Length + 1 > 35)
            {
                yield return new LabelLine(current.ToString(), first ? Quantity(item) : null);
                current.Clear();
                first = false;
            }

            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }

        yield return new LabelLine(current.ToString(), first ? Quantity(item) : null);
    }

    private static string Quantity(PalletLabelItemDto item) =>
        $"{item.Quantity.ToString("0.####", CultureInfo.InvariantCulture)} {item.Unit}";

    private static string BuildPage(PalletLabelPreviewDto label, IReadOnlyList<LabelLine> items,
        int pageNumber, int pageCount, bool isLastPage, PalletLabelLanguage language)
    {
        var strings = language == PalletLabelLanguage.English ? English : Polish;
        var builder = new StringBuilder();
        var y = PageHeight - 20m;
        Text(builder, strings.Title, Left, y, 14, true);
        Text(builder, $"{strings.Page} {pageNumber}/{pageCount}", PageWidth - Right - 48, y, 8, false);
        y -= 20;

        Text(builder, strings.Customer, Left, y, 7, true);
        y -= 10;
        foreach (var line in Wrap(label.CustomerName, 40))
        {
            Text(builder, line, Left, y, 11, true);
            y -= 12;
        }

        y -= 3;
        Text(builder, $"{strings.Order}: {label.OrderNumber}", Left, y, 9, false);
        y -= 14;
        Text(builder, $"{strings.Pallet}: {label.PalletNumber}", Left, y, 12, true);
        y -= 50;
        Barcode(builder, label.PalletNumber, Left, y, PageWidth - Left - Right);
        y -= 14;

        Line(builder, Left, y, PageWidth - Right, y, 0.8m);
        y -= 10;
        Text(builder, strings.Item, Left, y, 7, true);
        Text(builder, strings.Quantity, PageWidth - Right - 46, y, 7, true);
        y -= 6;
        Line(builder, Left, y, PageWidth - Right, y, 0.5m);
        y -= 10;

        foreach (var item in items)
        {
            Text(builder, item.Name, Left, y, 8, false);
            if (item.Quantity is not null) Text(builder, item.Quantity, PageWidth - Right - 46, y, 8, true);
            y -= 10;
        }

        if (isLastPage)
        {
            y -= 4;
            Line(builder, Left, y, PageWidth - Right, y, 0.8m);
            y -= 13;
            Weight(builder, strings.GoodsWeight, label.GoodsWeightKg, ref y);
            Weight(builder, strings.Tare, label.EmptyPalletWeightKg, ref y);
            Weight(builder, strings.TotalWeight, label.TotalWeightKg, ref y, true);
        }

        return builder.ToString();
    }

    private static void Weight(StringBuilder builder, string label, decimal value, ref decimal y, bool bold = false)
    {
        Text(builder, label, Left, y, bold ? 10 : 8, bold);
        Text(builder, $"{value.ToString("0.####", CultureInfo.InvariantCulture)} kg", PageWidth - Right - 70,
            y, bold ? 10 : 8, bold);
        y -= 13;
    }

    private static IEnumerable<string> Wrap(string text, int maximumLength)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var word in words.DefaultIfEmpty("-"))
        {
            if (current.Length > 0 && current.Length + word.Length + 1 > maximumLength)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }

        yield return current.ToString();
    }

    private static void Text(StringBuilder builder, string value, decimal x, decimal y, decimal size, bool bold) =>
        builder.AppendFormat(CultureInfo.InvariantCulture,
            "BT /F{0} {1:0.##} Tf {2:0.###} {3:0.###} Td ({4}) Tj ET\n",
            bold ? 2 : 1, size, x, y, Escape(value));

    private static void Line(StringBuilder builder, decimal x1, decimal y1, decimal x2, decimal y2, decimal width) =>
        builder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} w {1:0.###} {2:0.###} m {3:0.###} {4:0.###} l S\n",
            width, x1, y1, x2, y2);

    private static void Barcode(StringBuilder builder, string value, decimal x, decimal y, decimal availableWidth)
    {
        const decimal narrow = 0.55m;
        const decimal wide = 1.2m;
        const decimal height = 38m;
        var encoded = $"*{value.ToUpperInvariant()}*";
        var width = encoded.Sum(character => Pattern(character).Sum(part => part == 'w' ? wide : narrow) + narrow);
        if (width > availableWidth)
            throw new InvalidOperationException("Pallet number is too long for the Code 39 label.");

        var cursor = x + ((availableWidth - width) / 2m);
        foreach (var character in encoded)
        {
            var pattern = Pattern(character);
            for (var index = 0; index < pattern.Length; index++)
            {
                var segmentWidth = pattern[index] == 'w' ? wide : narrow;
                if (index % 2 == 0)
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} {3:0.###} re f\n",
                        cursor, y, segmentWidth, height);
                cursor += segmentWidth;
            }
            cursor += narrow;
        }
    }

    private static string Pattern(char character) => Code39.TryGetValue(character, out var pattern)
        ? pattern
        : throw new InvalidOperationException("Pallet number contains a character unsupported by Code 39.");

    private static string Escape(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (PolishCharacters.TryGetValue(character, out var encoded)) result.Append((char)encoded);
            else if (character is '\\' or '(' or ')') result.Append('\\').Append(character);
            else if (character <= 255) result.Append(character);
            else result.Append('?');
        }
        return result.ToString();
    }

    private static byte[] BuildPdf(IReadOnlyList<string> content)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, content.Count).Select(x => $"{5 + (x * 2)} 0 R"))}] /Count {content.Count} >>",
            $"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding {FontEncoding()} >>",
            $"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding {FontEncoding()} >>"
        };

        var pageObjects = new List<string>();
        foreach (var pageContent in content)
        {
            var pageId = 5 + (pageObjects.Count * 2);
            var contentId = pageId + 1;
            pageObjects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth.ToString("0.###", CultureInfo.InvariantCulture)} {PageHeight.ToString("0.###", CultureInfo.InvariantCulture)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>");
            pageObjects.Add($"<< /Length {Encoding.Latin1.GetByteCount(pageContent)} >>\nstream\n{pageContent}endstream");
        }
        objects.AddRange(pageObjects);

        using var stream = new MemoryStream();
        Write(stream, "%PDF-1.4\n%\u00e2\u00e3\u00cf\u00d3\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            Write(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xref = stream.Position;
        Write(stream, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(stream, $"{offset:0000000000} 00000 n \n");
        Write(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return stream.ToArray();
    }

    private static string FontEncoding() =>
        "<< /Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [128 /Aogonek /aogonek /Cacute /cacute /Eogonek /eogonek /Lslash /lslash /Nacute /nacute /Sacute /sacute /Zacute /zacute /Zdotaccent /zdotaccent] >>";

    private static void Write(Stream stream, string value)
    {
        var data = Encoding.Latin1.GetBytes(value);
        stream.Write(data, 0, data.Length);
    }

    private sealed record LabelLine(string Name, string? Quantity);

    private sealed record LabelStrings(string Title, string Page, string Customer, string Order,
        string Pallet, string Item, string Quantity, string GoodsWeight, string Tare, string TotalWeight);

    private static readonly LabelStrings Polish = new("ETYKIETA PALETY", "Strona", "ZAMAWIAJĄCY",
        "Zamówienie", "Paleta", "POZYCJA", "ILOŚĆ", "Masa towarów", "Tara", "Masa całkowita");
    private static readonly LabelStrings English = new("PALLET LABEL", "Page", "CUSTOMER", "Order",
        "Pallet", "ITEM", "QUANTITY", "Goods weight", "Tare", "Total weight");

    private static readonly IReadOnlyDictionary<char, string> Code39 = new Dictionary<char, string>
    {
        ['0'] = "nnnwwnwnn", ['1'] = "wnnwnnnnw", ['2'] = "nnwwnnnnw", ['3'] = "wnwwnnnnn",
        ['4'] = "nnnwwnnnw", ['5'] = "wnnwwnnnn", ['6'] = "nnwwwnnnn", ['7'] = "nnnwnnwnw",
        ['8'] = "wnnwnnwnn", ['9'] = "nnwwnnwnn", ['A'] = "wnnnnwnnw", ['B'] = "nnwnnwnnw",
        ['C'] = "wnwnnwnnn", ['D'] = "nnnnwwnnw", ['E'] = "wnnnwwnnn", ['F'] = "nnwnwwnnn",
        ['G'] = "nnnnnwwnw", ['H'] = "wnnnnwwnn", ['I'] = "nnwnnwwnn", ['J'] = "nnnnwwwnn",
        ['K'] = "wnnnnnnww", ['L'] = "nnwnnnnww", ['M'] = "wnwnnnnwn", ['N'] = "nnnnwnnww",
        ['O'] = "wnnnwnnwn", ['P'] = "nnwnwnnwn", ['Q'] = "nnnnnnwww", ['R'] = "wnnnnnwwn",
        ['S'] = "nnwnnnwwn", ['T'] = "nnnnwnwwn", ['U'] = "wwnnnnnnw", ['V'] = "nwwnnnnnw",
        ['W'] = "wwwnnnnnw", ['X'] = "nwnnwnnnw", ['Y'] = "wwnnwnnnn", ['Z'] = "nwwnwnnnn",
        ['-'] = "nwnnnnwnw", ['.'] = "wwnnnnwnn", [' '] = "nwwnnnwnn", ['$'] = "nwnwnwnnn",
        ['/'] = "nwnnwnnnw", ['+'] = "nwnnnwnwn", ['%'] = "nnnwnwnwn", ['*'] = "nwnnwnwnn"
    };
}
