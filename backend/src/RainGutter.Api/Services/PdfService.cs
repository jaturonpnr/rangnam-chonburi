using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RainGutter.Api.Dtos;
using RainGutter.Api.Models;

namespace RainGutter.Api.Services;

public class PdfService : IPdfService
{
    public byte[] GenerateQuotePdf(QuoteRequest quote, Lead lead, ShopProfile shop)
    {
        var breakdown = JsonSerializer.Deserialize<List<BreakdownItem>>(quote.BreakdownJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Sarabun").FontSize(12));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text(shop.ShopName).Bold().FontSize(18);
                            inner.Item().Text($"โทร: {shop.Phone}");
                            inner.Item().Text(shop.Address);
                        });
                        row.ConstantItem(100).AlignRight().Column(inner =>
                        {
                            inner.Item().Text("ใบเสนอราคา").Bold().FontSize(14).AlignRight();
                            inner.Item().Text($"เลขที่: {quote.QuoteNumber}").AlignRight();
                        });
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    // Quote meta
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"วันที่: {quote.CreatedAt:dd/MM/yyyy}");
                        row.RelativeItem().AlignRight().Text(
                            $"ราคายืนยันภายใน {shop.QuoteValidityDays} วัน ({quote.CreatedAt.AddDays(shop.QuoteValidityDays):dd/MM/yyyy})");
                    });

                    // Customer info
                    col.Item().PaddingTop(12).Column(inner =>
                    {
                        inner.Item().Text("ข้อมูลลูกค้า").Bold().Underline();
                        inner.Item().Text($"ชื่อ: {lead.CustomerName}");
                        inner.Item().Text($"โทร: {lead.Phone}");
                        if (!string.IsNullOrEmpty(lead.Address))
                            inner.Item().Text($"ที่อยู่: {lead.Address}");
                    });

                    // Line items table
                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(5);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(6).Text("รายการ").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(6).AlignRight().Text("จำนวนเงิน (บาท)").Bold();
                        });

                        foreach (var item in breakdown)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(item.Label);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"{item.Amount:N0}");
                        }

                        table.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("ยอดรวมประเมิน").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"{quote.EstimatedTotal:N0}").Bold();
                    });

                    // Disclaimer
                    col.Item().PaddingTop(16).Background(Colors.Orange.Lighten4).Padding(8).Text(
                        "⚠️ ราคาประเมินเบื้องต้น ราคาจริงยืนยันหลังสำรวจหน้างาน")
                        .FontColor(Colors.Orange.Darken3).Bold();

                    // VAT disclaimer
                    col.Item().PaddingTop(8).Text(
                        "หมายเหตุ: เอกสารนี้เป็นใบเสนอราคา ไม่ใช่ใบกำกับภาษี")
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text(shop.QuoteFooterNote).FontColor(Colors.Grey.Darken1).FontSize(10);
                        row.ConstantItem(120).AlignRight().Text($"หน้า 1/1").FontColor(Colors.Grey.Darken1).FontSize(10);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
