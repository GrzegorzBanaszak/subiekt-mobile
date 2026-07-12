using System.Text;
using SubiektMobile.Application.Pallets;
using SubiektMobile.Infrastructure.Pallets;
using Xunit;

namespace SubiektMobile.Infrastructure.IntegrationTests;

public sealed class PalletLabelPdfRendererTests
{
    [Fact]
    public void Render_creates_100_by_150_mm_pdf_with_weights_and_code39_bars()
    {
        var pdf = new PalletLabelPdfRenderer().Render(Label("Żółć Sp. z o.o.", 2), PalletLabelLanguage.Polish);
        var text = Encoding.Latin1.GetString(pdf);

        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("/MediaBox [0 0 283.465 425.197]", text);
        Assert.Contains("Masa towarów", text);
        Assert.Contains("Tara", text);
        Assert.Contains("Masa ca", text);
        Assert.Contains(" re f", text);
        Assert.Contains((byte)142, pdf);
    }

    [Fact]
    public void Render_paginates_long_item_list_and_keeps_totals_on_last_page()
    {
        var pdf = new PalletLabelPdfRenderer().Render(Label("Customer", 20), PalletLabelLanguage.English);
        var text = Encoding.Latin1.GetString(pdf);

        Assert.Contains("/Count 2", text);
        Assert.Equal(1, Count(text, "Tara"));
    }

    private static PalletLabelPreviewDto Label(string customer, int count) => new(
        "ZAM-1", "PAL-20260712-ABCDEF12", customer, 12.5m, 25m, 37.5m,
        Enumerable.Range(1, count).Select(index => new PalletLabelItemDto(
            $"Pozycja magazynowa numer {index}", 1m, "szt.")).ToList());

    private static int Count(string text, string value) => text.Split(value, StringSplitOptions.None).Length - 1;
}
