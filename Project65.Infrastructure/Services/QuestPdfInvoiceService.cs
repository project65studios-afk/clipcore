using Project65.Core.Entities;
using Project65.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net.Http;

namespace Project65.Infrastructure.Services;

public class QuestPdfInvoiceService : IInvoiceService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IStorageService _storageService;
    private readonly IHttpClientFactory _httpClientFactory;

    public QuestPdfInvoiceService(
        ISettingsRepository settingsRepository,
        IStorageService storageService,
        IHttpClientFactory httpClientFactory)
    {
        _settingsRepository = settingsRepository;
        _storageService = storageService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]> GenerateInvoiceAsync(Purchase purchase)
    {
        // 1. Fetch Settings
        var logoBytes = await GetLogoBytesAsync();
        var storeAddress = await _settingsRepository.GetValueAsync("StoreAddress");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text($"Invoice #{purchase.OrderId}").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                            column.Item().Text(text =>
                            {
                                text.Span("Date: ").SemiBold();
                                text.Span($"{purchase.CreatedAt:MMMM dd, yyyy}");
                            });
                             column.Item().Text(text =>
                            {
                                text.Span("Status: ").SemiBold();
                                text.Span(purchase.PricePaidCents > 0 ? "Paid" : "Free").FontColor(purchase.PricePaidCents > 0 ? Colors.Green.Medium : Colors.Grey.Medium);
                            });
                        });

                        if (logoBytes != null && logoBytes.Length > 0)
                        {
                            row.ConstantItem(100).Height(50).AlignRight().Image(logoBytes, ImageScaling.FitArea);
                        }
                        else 
                        {
                             row.ConstantItem(100).Height(50).Placeholder();
                        }
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);

                        x.Item().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("Bill To").SemiBold();
                                column.Item().Text(purchase.CustomerName ?? purchase.User?.UserName ?? "Valued Customer");
                                column.Item().Text(purchase.CustomerEmail ?? purchase.User?.Email ?? "");
                                if (!string.IsNullOrEmpty(purchase.CustomerAddress))
                                {
                                    column.Item().Text(purchase.CustomerAddress);
                                }
                            });

                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("From").SemiBold();
                                column.Item().Text("Project65 Studios");
                                
                                if (!string.IsNullOrEmpty(storeAddress))
                                {
                                    foreach (var line in storeAddress.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        column.Item().Text(line);
                                    }
                                }
                                else
                                {
                                    column.Item().Text("123 Creative Blvd");
                                    column.Item().Text("Los Angeles, CA 90001");
                                }
                                column.Item().Text("support@project65.com");
                            });
                        });
                        
                        x.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Item");
                                header.Cell().Element(CellStyle).Text("License");
                                header.Cell().Element(CellStyle).AlignRight().Text("Price");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                                }
                            });

                            table.Cell().Element(CellStyle).Text("1");
                            table.Cell().Element(CellStyle).Text(purchase.ClipTitle ?? "Video Clip");
                            table.Cell().Element(CellStyle).Text(purchase.LicenseType.ToString());
                            table.Cell().Element(CellStyle).AlignRight().Text($"${purchase.PricePaidCents / 100.00:F2}");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            }
                        });
                        
                        x.Item().AlignRight().Text($"Total: ${purchase.PricePaidCents / 100.00:F2}").FontSize(14).SemiBold();
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        })
        .GeneratePdf();
    }

    private async Task<byte[]?> GetLogoBytesAsync()
    {
        try 
        {
            var logoSetting = await _settingsRepository.GetValueAsync("BrandLogoUrl");
            if (string.IsNullOrEmpty(logoSetting)) return null;

            string downloadUrl;
            if (logoSetting.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = logoSetting;
            }
            else
            {
                // Assume Storage Key
                downloadUrl = _storageService.GetPresignedDownloadUrl(logoSetting);
            }

            var client = _httpClientFactory.CreateClient();
            return await client.GetByteArrayAsync(downloadUrl);
        }
        catch 
        {
            return null;
        }
    }
}
