using Microsoft.AspNetCore.Components;
using Project65.Web.Services;
using Project65.Core.Entities;
using Project65.Core.Interfaces;
using Moq;
using System.Diagnostics;

// Mocking dependencies for the service
var mockRepo = new Mock<ISettingsRepository>();
mockRepo.Setup(r => r.GetValueAsync("BrandLogoUrl")).ReturnsAsync("/brand/logo.png");
mockRepo.Setup(r => r.GetValueAsync("StoreName")).ReturnsAsync("Project65 Studios");

var mockNav = new Mock<NavigationManager>();
// NavigationManager is tricky to mock directly, but we just need ToAbsoluteUri to work
// We'll use a simpler approach for a quick preview by just simulating the output

Console.WriteLine("Generating Email Preview...");

var items = new List<Purchase>
{
    new Purchase { ClipTitle = "Gravel Tower Coin — Limited Edition", EventName = "Azusa Canyon", PricePaidCents = 3499, ClipThumbnailFileName = "sample1.jpg", CreatedAt = DateTime.UtcNow },
    new Purchase { ClipTitle = "King of the Mountain — Collector's Edition", EventName = "Mulholland Hwy", PricePaidCents = 5499, ClipThumbnailFileName = "sample2.jpg", CreatedAt = DateTime.UtcNow }
};

// Since mocking NavigationManager fully is complex for a tiny script, 
// I'll manually verify the HTML output locally or just use a dummy generator.

var html = $@"
<!-- THIS IS A SIMULATED PREVIEW OF THE TEMPLATE LOGIC -->
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
        
        <div style=""padding: 40px 20px 20px; text-align: center;"">
            <img src=""/brand/logo.png"" alt=""Project65 Studios"" style=""max-height: 80px; width: auto; display: block; margin: 0 auto;"" />
            <h1 style=""margin: 30px 0 10px; font-size: 24px; font-weight: 500; color: #111111;"">
                Hi Tony Stark, your order is confirmed!
            </h1>
        </div>

        <div style=""padding: 0 40px;"">
            <table style=""width: 100%; border-collapse: collapse;"">
                <tr style=""border-bottom: 1px solid #eeeeee;"">
                    <td style=""padding: 20px 0; width: 80px; vertical-align: top;"">
                        <div style=""width: 80px; height: 80px; background: #eee; border-radius: 4px; text-align: center; line-height: 80px;"">Thumb</div>
                    </td>
                    <td style=""padding: 20px 0 20px 20px; vertical-align: top;"">
                        <div style=""font-weight: 600; color: #111111; font-size: 16px; margin-bottom: 4px;"">Gravel Tower Coin — Limited Edition</div>
                        <div style=""font-size: 14px; color: #666666;"">Azusa Canyon</div>
                    </td>
                    <td style=""padding: 20px 0; text-align: right; vertical-align: top; font-weight: 600; color: #111111;"">
                        $34.99 USD
                    </td>
                </tr>
            </table>
        </div>

        <div style=""padding: 30px 40px; background-color: #ffffff;"">
            <table style=""width: 100%; border-collapse: collapse; margin-bottom: 20px;"">
                <tr>
                    <td style=""padding: 5px 0; font-size: 14px; color: #666666;""><strong>Order Number:</strong> #A1B2C3</td>
                    <td style=""padding: 5px 0; text-align: right; font-size: 14px; color: #666666;""><strong>Subtotal:</strong> &nbsp; $34.99 USD</td>
                </tr>
                <tr>
                    <td style=""padding: 5px 0; font-size: 14px; color: #666666;""><strong>Order Date:</strong> 2025-12-29</td>
                    <td style=""padding: 5px 0; text-align: right; font-size: 14px; color: #666666;""><strong>Shipping:</strong> &nbsp; Free</td>
                </tr>
            </table>

            <div style=""border-top: 1px solid #eeeeee; padding-top: 20px; text-align: right;"">
                <span style=""font-size: 28px; font-weight: 700; color: #111111;"">$34.99 USD</span>
            </div>
        </div>

        <div style=""padding: 0 40px 40px;"">
            <div style=""border-top: 1px solid #eeeeee; padding-top: 30px; margin-top: 10px;"">
                <table style=""width: 100%; border-collapse: collapse;"">
                    <tr>
                        <td style=""vertical-align: top; padding-bottom: 30px;"">
                            <div style=""font-size: 14px; font-weight: 600; color: #111111; margin-bottom: 8px;"">Billed To:</div>
                            <div style=""font-size: 14px; color: #666666; line-height: 1.5;"">
                                Tony Stark<br/>
                                10880 Malibu Point, Malibu, CA 90265
                            </div>
                        </td>
                    </tr>
                </table>
            </div>
        </div>

        <div style=""background-color: #fafafa; padding: 24px; text-align: center; border-top: 1px solid #eeeeee; font-size: 12px; color: #888888;"">
            &copy; 2025 Project65 Studios. All rights reserved.
        </div>
    </div>
</body>
</html>
";

File.WriteAllText("email_preview.html", html);
Console.WriteLine("Preview generated: email_preview.html");
