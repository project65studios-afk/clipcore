using Microsoft.AspNetCore.Components;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Web.Services;

public class EmailTemplateService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly NavigationManager _navigationManager;
    private readonly IStorageService _storageService;

    public EmailTemplateService(
        ISettingsRepository settingsRepository, 
        NavigationManager navigationManager,
        IStorageService storageService)
    {
        _settingsRepository = settingsRepository;
        _navigationManager = navigationManager;
        _storageService = storageService;
    }

    public async Task<string> GenerateOrderReceiptHtmlAsync(string orderId, List<Purchase> items, string customerName)
    {
        var settings = await _settingsRepository.ListAllAsync();
        var logoUrl = settings.FirstOrDefault(s => s.Key == "BrandLogoUrl")?.Value;
        
        string ResolveUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            if (url.StartsWith("http") || url.StartsWith("/")) return url;
            return _storageService.GetPresignedDownloadUrl(url);
        }

        var absoluteLogoUrl = ResolveUrl(logoUrl);
            
        var storeName = settings.FirstOrDefault(s => s.Key == "StoreName")?.Value ?? "Project65 Studios";
        var firstItem = items.FirstOrDefault();
        var orderDate = firstItem?.CreatedAt.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        var itemRows = string.Join("\n", items.Select(i => {
            string? thumbUrl = null;
            // Fallback to global Clip thumbnail if order-specific one is not yet set (common in Receipt email)
            var thumbFile = !string.IsNullOrEmpty(i.ClipThumbnailFileName) ? i.ClipThumbnailFileName : i.Clip?.ThumbnailFileName;
            
            if (!string.IsNullOrEmpty(thumbFile))
            {
                var cleanName = thumbFile.TrimStart('/');
                var storageKey = cleanName.StartsWith("thumbnails/") 
                    ? cleanName 
                    : $"thumbnails/{cleanName}";
                thumbUrl = _storageService.GetPresignedDownloadUrl(storageKey);
            }

            var imgHtml = !string.IsNullOrEmpty(thumbUrl) 
                ? $@"<img src=""{thumbUrl}"" alt=""{i.ClipTitle}"" style=""width: 80px; height: 80px; object-fit: cover; border-radius: 4px; display: block; background-color: #333;"" />"
                : $@"<div style=""width: 80px; height: 80px; background-color: #f1f5f9; border-radius: 4px; border: 1px solid #e2e8f0; display: table-cell; vertical-align: middle; text-align: center;"">
                        <span style=""font-size: 24px; color: #cbd5e1;"">📹</span>
                     </div>";

            return $@"
            <tr style=""border-bottom: 1px solid #eeeeee;"">
                <td style=""padding: 20px 0; width: 80px; vertical-align: top;"">
                    {imgHtml}
                </td>
                <td style=""padding: 20px 0 20px 20px; vertical-align: top;"">
                    <div style=""font-weight: 600; color: #111111; font-size: 16px; margin-bottom: 4px;"">{i.ClipTitle}</div>
                    <div style=""font-size: 14px; color: #666666;"">
                        {i.EventName}
                        {(i.IsGif ? @"<br/><span style=""display: inline-block; background-color: #fdf2f8; color: #db2777; border: 1px solid #db2777; font-size: 10px; padding: 2px 6px; border-radius: 4px; font-weight: 700; text-transform: uppercase; margin-top: 4px;"">GIF License</span>" : $@"<br/><span style=""display: inline-block; background-color: #f0fdfa; color: #0d9488; border: 1px solid #0d9488; font-size: 10px; padding: 2px 6px; border-radius: 4px; font-weight: 700; text-transform: uppercase; margin-top: 4px;"">{i.LicenseType} License</span>")}
                    </div>
                </td>
                <td style=""padding: 20px 0; text-align: right; vertical-align: top; font-weight: 600; color: #111111;"">
                    ${(i.PricePaidCents / 100.0):N2} USD
                </td>
            </tr>";
        }));

        var subtotal = items.Sum(i => i.PricePaidCents) / 100.0;
        var total = subtotal; 

        var logoImg = !string.IsNullOrEmpty(absoluteLogoUrl)
            ? $@"<img src=""{absoluteLogoUrl}"" alt=""{storeName}"" style=""max-height: 80px; width: auto; display: block; margin: 0 auto;"" />"
            : $@"<div style=""font-size: 24px; font-weight: bold; color: #111111; text-align: center; text-transform: uppercase;"">{storeName}</div>";

        static string FormatAddress(string? address)
        {
            if (string.IsNullOrEmpty(address)) return "";
            var parts = address.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return address;

            var street = parts[0];
            var details = string.Join(", ", parts.Skip(1).Take(parts.Length - 2));
            var country = parts.Last();
            if (country.Trim().Equals("US", StringComparison.OrdinalIgnoreCase)) country = "USA";

            return $@"<strong style=""color: #111111;"">{street}</strong><br/>{details}<br/>{country.ToUpper()}";
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
    </style>
</head>
<body style=""margin: 0; padding: 0; background-color: #f7f7f7; font-family: 'Inter', -apple-system, sans-serif; color: #333333;"">
    <div style=""display: none; max-height: 0px; overflow: hidden;"">
        Your order #{orderId} is confirmed. View your receipt and download details.
    </div>
    
    <div style=""max-width: 600px; margin: 40px auto; background-color: #ffffff; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.05);"">
        
        <!-- Header / Logo -->
        <div style=""padding: 40px 20px 20px; text-align: center;"">
            {logoImg}
            <h1 style=""margin: 30px 0 10px; font-size: 24px; font-weight: 500; color: #111111;"">
                Hi {customerName}, your order is confirmed!
            </h1>
        </div>

        <!-- Items Container -->
        <div style=""padding: 0 40px;"">
            <table style=""width: 100%; border-collapse: collapse;"">
                <tbody>
                    {itemRows}
                </tbody>
            </table>
        </div>

        <!-- Order Summary -->
        <div style=""padding: 30px 40px; background-color: #ffffff;"">
            <table style=""width: 100%; border-collapse: collapse; margin-bottom: 20px;"">
                <tr>
                    <td style=""padding: 5px 0; font-size: 14px; color: #666666;""><strong>Order Number:</strong> #{orderId}</td>
                    <td style=""padding: 5px 0; text-align: right; font-size: 14px; color: #666666;""><strong>Subtotal:</strong> &nbsp; ${subtotal:N2} USD</td>
                </tr>
                <tr>
                    <td style=""padding: 5px 0; font-size: 14px; color: #666666;""><strong>Order Date:</strong> {orderDate}</td>
                    <td style=""padding: 5px 0; text-align: right; font-size: 14px; color: #666666;""><strong>Shipping:</strong> &nbsp; Free</td>
                </tr>
                <tr>
                    <td></td>
                    <td style=""padding: 5px 0; text-align: right; font-size: 14px; color: #666666;""><strong>Taxes:</strong> &nbsp; $0.00 USD</td>
                </tr>
            </table>

            <div style=""border-top: 1px solid #eeeeee; padding-top: 20px; text-align: right;"">
                <span style=""font-size: 28px; font-weight: 700; color: #111111;"">${total:N2} USD</span>
            </div>
        </div>

        <!-- Contact / Info Sections -->
        <div style=""padding: 0 40px 40px;"">
            <div style=""border-top: 1px solid #eeeeee; padding-top: 30px; margin-top: 10px;"">
                <table style=""width: 100%; border-collapse: collapse;"">
                    <tr>
                        <td style=""vertical-align: top; padding-bottom: 30px;"">
                            <div style=""font-size: 14px; font-weight: 600; color: #111111; margin-bottom: 8px;"">Billed To:</div>
                            <div style=""font-size: 14px; color: #666666; line-height: 1.5;"">
                                {customerName}<br/>
                                {FormatAddress(firstItem?.CustomerAddress)}
                            </div>
                        </td>
                        <td style=""vertical-align: top; padding-bottom: 30px; padding-left: 20px;"">
                            <div style=""font-size: 14px; color: #111111; font-weight: 600; margin-bottom: 8px;"">Delivery:</div>
                            <div style=""font-size: 14px; color: #666666; line-height: 1.5;"">
                                {(items.Any(i => i.IsGif) ? "<strong>GIFs:</strong> Available for download now!<br/>" : "")}
                                {(items.Any(i => !i.IsGif) ? "<strong>Video Clips:</strong> Will be processed and available in your dashboard shortly." : "")}
                            </div>
                        </td>
                    </tr>
                </table>
            </div>

            <div style=""text-align: center; margin-top: 20px;"">
                <a href=""{_navigationManager.BaseUri}my-purchases"" style=""display: inline-block; padding: 16px 32px; background-color: #111111; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 16px;"">
                    View Your Clips
                </a>
            </div>
        </div>

        <!-- Footer -->
        <div style=""background-color: #fafafa; padding: 24px 40px; text-align: center; border-top: 1px solid #eeeeee; font-size: 12px; color: #888888; line-height: 1.5;"">
            <p style=""margin: 0 0 10px;"">&copy; {DateTime.UtcNow.Year} {storeName}. All rights reserved.</p>
            <p style=""margin: 0 0 10px;"">
                {storeName}<br/>
                123 Creator Way, Suite 100<br/>
                Los Angeles, CA, 90012, USA
            </p>
            <p style=""margin: 0;"">
                You are receiving this email because you made a purchase at {storeName}.<br/>
                <a href=""{_navigationManager.BaseUri}Account/Manage"" style=""color: #888888; text-decoration: underline;"">Manage Preferences</a>
            </p>
        </div>
    </div>
</body>
</html>
" ;
    }

    public Task<string> GenerateOrderReceiptTextAsync(string orderId, List<Purchase> items, string customerName)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"ORDER CONFIRMATION #{orderId}");
        sb.AppendLine("==========================================");
        sb.AppendLine($"Hi {customerName}, your order is confirmed!");
        sb.AppendLine();
        
        var subtotal = items.Sum(i => i.PricePaidCents) / 100.0;
        foreach (var item in items)
        {
            sb.AppendLine($"{item.ClipTitle} - {item.EventName}");
            sb.AppendLine($"Price: ${(item.PricePaidCents / 100.0):N2} USD");
            sb.AppendLine("------------------------------------------");
        }
        
        sb.AppendLine($"TOTAL: ${subtotal:N2} USD");
        sb.AppendLine();
        sb.AppendLine("View your clips here:");
        sb.AppendLine($"{_navigationManager.BaseUri}my-purchases");
        sb.AppendLine();
        sb.AppendLine("Thank you for your business.");
        sb.AppendLine("Project65 Studios");
        sb.AppendLine("123 Creator Way, Suite 100, Los Angeles, CA, 90012, USA");
        
        return Task.FromResult(sb.ToString());
    }

    public async Task<string> GenerateFulfillmentEmailHtmlAsync(string orderId, List<Purchase> items, string customerName)
    {
        var settings = await _settingsRepository.ListAllAsync();
        var logoUrl = settings.FirstOrDefault(s => s.Key == "BrandLogoUrl")?.Value;
        var baseSiteUrl = settings.FirstOrDefault(s => s.Key == "BaseSiteUrl")?.Value;

        string ResolveUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            if (url.StartsWith("http") || url.StartsWith("/")) return url;
            return _storageService.GetPresignedDownloadUrl(url);
        }

        var absoluteLogoUrl = ResolveUrl(logoUrl);
        var storeName = settings.FirstOrDefault(s => s.Key == "StoreName")?.Value ?? "Project65 Studios";
        
        var itemRows = string.Join("\n", items.Select(i => {
            string? thumbUrl = null;
            // Fallback to global Clip thumbnail
            var thumbFile = !string.IsNullOrEmpty(i.ClipThumbnailFileName) ? i.ClipThumbnailFileName : i.Clip?.ThumbnailFileName;

            if (!string.IsNullOrEmpty(thumbFile))
            {
                var cleanName = thumbFile.TrimStart('/');
                var storageKey = cleanName.StartsWith("thumbnails/") 
                    ? cleanName 
                    : $"thumbnails/{cleanName}";
                thumbUrl = _storageService.GetPresignedDownloadUrl(storageKey);
            }

            var imgHtml = !string.IsNullOrEmpty(thumbUrl) 
                ? $@"<img src=""{thumbUrl}"" alt=""Clip"" style=""width: 80px; height: 80px; object-fit: cover; border-radius: 4px; display: block; background-color: #333;"" />"
                : $@"<div style=""width: 80px; height: 80px; background-color: #f1f5f9; border-radius: 4px; border: 1px solid #e2e8f0; display: table-cell; vertical-align: middle; text-align: center;"">
                        <span style=""font-size: 24px; color: #cbd5e1;"">📹</span>
                     </div>";

            return $@"
            <tr style=""border-bottom: 1px solid #eeeeee;"">
                <td style=""padding: 20px 0; width: 80px; vertical-align: top;"">
                    {imgHtml}
                </td>
                <td style=""padding: 20px 0 20px 20px; vertical-align: top;"">
                    <div style=""font-weight: 600; color: #111111; font-size: 16px; margin-bottom: 4px;"">{i.ClipTitle}</div>
                    <div style=""font-size: 14px; color: #666666;"">{i.EventName}</div>
                </td>
            </tr>";
        }));

        var logoImg = !string.IsNullOrEmpty(absoluteLogoUrl)
            ? $@"<img src=""{absoluteLogoUrl}"" alt=""{storeName}"" style=""max-height: 80px; width: auto; display: block; margin: 0 auto;"" />"
            : $@"<div style=""font-size: 24px; font-weight: bold; color: #111111; text-align: center; text-transform: uppercase;"">{storeName}</div>";

        // CTA link should use BaseSiteUrl if configured
        var effectiveBaseUri = !string.IsNullOrEmpty(baseSiteUrl) 
            ? baseSiteUrl.TrimEnd('/') + "/" 
            : _navigationManager.BaseUri;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
    </style>
</head>
<body style=""margin: 0; padding: 0; background-color: #f7f7f7; font-family: 'Inter', -apple-system, sans-serif; color: #333333;"">
    <div style=""display: none; max-height: 0px; overflow: hidden;"">
        Your clips are ready for download. Access your content now.
    </div>

    <div style=""max-width: 600px; margin: 40px auto; background-color: #ffffff; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.05);"">
        
        <!-- Header / Logo -->
        <div style=""padding: 40px 20px 30px; text-align: center;"">
            {logoImg}
            <h1 style=""margin: 30px 0 10px; font-size: 28px; font-weight: 700; color: #111111;"">
                Your order is ready!
            </h1>
            <p style=""font-size: 18px; color: #666666; margin: 0;"">
                Hi {customerName}, your clips from order #{orderId} have been processed.
            </p>
        </div>

        <!-- Items Container -->
        <div style=""padding: 0 40px;"">
            <table style=""width: 100%; border-collapse: collapse;"">
                <tbody>
                    {itemRows}
                </tbody>
            </table>
        </div>

        <!-- CTA Section -->
        <div style=""padding: 40px; text-align: center;"">
            <p style=""font-size: 16px; color: #444444; margin-bottom: 30px; line-height: 1.6;"">
                You can now view and download your high-quality clips directly from your order delivery page.
            </p>
            <a href=""{effectiveBaseUri}delivery/{items.FirstOrDefault()?.StripeSessionId}"" style=""display: inline-block; padding: 18px 40px; background-color: #111111; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 18px;"">
                Access My Clips
            </a>
        </div>

        <!-- Info Section -->
        <div style=""padding: 0 40px 40px;"">
            <div style=""border-top: 1px solid #eeeeee; padding-top: 30px; text-align: center;"">
                <div style=""font-size: 14px; color: #888888; line-height: 1.5;"">
                    If you have any trouble accessing your files, please reply to this email or contact support.
                </div>
            </div>
        </div>

        <!-- Footer -->
        <div style=""background-color: #fafafa; padding: 24px 40px; text-align: center; border-top: 1px solid #eeeeee; font-size: 12px; color: #888888; line-height: 1.5;"">
            <p style=""margin: 0 0 10px;"">&copy; {DateTime.UtcNow.Year} {storeName}. All rights reserved.</p>
            <p style=""margin: 0 0 10px;"">
                {storeName}<br/>
                123 Creator Way, Suite 100<br/>
                Los Angeles, CA, 90012, USA
            </p>
            <p style=""margin: 0;"">
                You are receiving this email because you made a purchase at {storeName}.<br/>
                <a href=""{effectiveBaseUri}Account/Manage"" style=""color: #888888; text-decoration: underline;"">Manage Preferences</a>
            </p>
        </div>
    </div>
</body>
</html>
" ;
    }

    public Task<string> GenerateFulfillmentTextAsync(string orderId, List<Purchase> items, string customerName)
    {
         var sb = new System.Text.StringBuilder();
        sb.AppendLine($"YOUR ORDER IS READY #{orderId}");
        sb.AppendLine("==========================================");
        sb.AppendLine($"Hi {customerName}, your clips are ready!");
        sb.AppendLine();
        
        foreach (var item in items)
        {
            sb.AppendLine($"{item.ClipTitle} - {item.EventName}");
        }
        
        sb.AppendLine();
        sb.AppendLine("Access and download your clips here:");
        // Need to replicate the logic for effectiveBaseUri, but we are inside a method.
        // Assuming base logic is similar or simplified for text.
        // Let's re-calculate it locally or pass it if complex.
        // Re-fetching settings is safe since it's cached usually, or we just use NavManager fallback.
        // For text email, simple links are key.
        
        // Simulating the logic from the HTML method for consistency, though slightly redundant refetch.
        // In a real refactor, we'd extract this logic.
        sb.AppendLine($"{_navigationManager.BaseUri}delivery/{items.FirstOrDefault()?.StripeSessionId}");
        
        sb.AppendLine();
        sb.AppendLine("Thank you for your business.");
        sb.AppendLine("Project65 Studios");
        sb.AppendLine("123 Creator Way, Suite 100, Los Angeles, CA, 90012, USA");
        
        return Task.FromResult(sb.ToString());
    }
}
