using Microsoft.AspNetCore.Components;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Web.Services;

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
            
        var storeName = settings.FirstOrDefault(s => s.Key == "StoreName")?.Value ?? "ClipCore Studios";
        var firstItem = items.FirstOrDefault();
        var orderDate = firstItem?.CreatedAt.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        var itemRows = string.Join("\n", items.Select(i => {
            string thumbUrl = "https://via.placeholder.com/80?text=Clip";
            if (!string.IsNullOrEmpty(i.ClipThumbnailFileName))
            {
                var storageKey = i.ClipThumbnailFileName.Contains("/") 
                    ? i.ClipThumbnailFileName 
                    : $"thumbnails/{i.ClipThumbnailFileName}";
                thumbUrl = _storageService.GetPresignedDownloadUrl(storageKey);
            }

            return $@"
            <tr style=""border-bottom: 1px solid #eeeeee;"">
                <td style=""padding: 20px 0; width: 80px; vertical-align: top;"">
                    <img src=""{thumbUrl}"" alt=""{i.ClipTitle}"" style=""width: 80px; height: 80px; object-fit: cover; border-radius: 4px;"" />
                </td>
                <td style=""padding: 20px 0 20px 20px; vertical-align: top;"">
                    <div style=""font-weight: 600; color: #111111; font-size: 16px; margin-bottom: 4px;"">{i.ClipTitle}</div>
                    <div style=""font-size: 14px; color: #666666;"">{i.EventName}</div>
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
                            <div style=""font-size: 14px; font-weight: 600; color: #111111; margin-bottom: 8px;"">Delivery:</div>
                            <div style=""font-size: 14px; color: #666666; line-height: 1.5;"">
                                Your clips will be processed and available in your dashboard shortly.
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
        <div style=""background-color: #fafafa; padding: 24px; text-align: center; border-top: 1px solid #eeeeee; font-size: 12px; color: #888888;"">
            &copy; {DateTime.UtcNow.Year} {storeName}. All rights reserved.<br/>
            Need help? Contact us at {firstItem?.CustomerEmail ?? "support@project65.com"}
        </div>
    </div>
</body>
</html>
" ;
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
        var storeName = settings.FirstOrDefault(s => s.Key == "StoreName")?.Value ?? "ClipCore Studios";
        
        var itemRows = string.Join("\n", items.Select(i => {
            string? thumbUrl = null;
            if (!string.IsNullOrEmpty(i.ClipThumbnailFileName))
            {
                var storageKey = i.ClipThumbnailFileName.Contains("/") 
                    ? i.ClipThumbnailFileName 
                    : $"thumbnails/{i.ClipThumbnailFileName}";
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
                You can now view and download your high-quality clips directly from your account dashboard.
            </p>
            <a href=""{effectiveBaseUri}my-purchases"" style=""display: inline-block; padding: 18px 40px; background-color: #111111; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 18px;"">
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
        <div style=""background-color: #fafafa; padding: 24px; text-align: center; border-top: 1px solid #eeeeee; font-size: 12px; color: #888888;"">
            &copy; {DateTime.UtcNow.Year} {storeName}. All rights reserved.<br/>
        </div>
    </div>
</body>
</html>
" ;
    }
}
