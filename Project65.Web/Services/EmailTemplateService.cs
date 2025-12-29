using System.Text;
using Project65.Core.Entities;

namespace Project65.Web.Services;

public class EmailTemplateService
{
    public string GenerateOrderReceiptHtml(string orderId, List<Purchase> items, string customerName)
    {
        var itemRows = string.Join("\n", items.Select(i => $@"
            <tr>
                <td style=""padding: 12px 0;"">
                    <div style=""font-weight: bold; color: #ffffff;"">{i.ClipTitle}</div>
                    <div style=""font-size: 12px; color: #a0a0a0;"">{i.EventName}</div>
                </td>
                <td style=""padding: 12px 0; text-align: right; color: #ffffff;"">${(i.PricePaidCents / 100.0):N2}</td>
            </tr>
        "));

        var total = items.Sum(i => i.PricePaidCents) / 100.0;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;700&display=swap');
    </style>
</head>
<body style=""margin: 0; padding: 0; background-color: #0d0d0d; font-family: 'Inter', -apple-system, sans-serif; color: #ffffff;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: #1a1a1a; border-radius: 12px; overflow: hidden; margin-top: 40px; margin-bottom: 40px; border: 1px solid #333333;"">
        <!-- Header -->
        <div style=""background-color: #0d0d0d; padding: 40px; text-align: center; border-bottom: 1px solid #333333;"">
            <h1 style=""margin: 0; font-size: 28px; letter-spacing: 2px; text-transform: uppercase;"">Order Confirmed</h1>
        </div>

        <!-- Body -->
        <div style=""padding: 40px;"">
            <p style=""font-size: 16px; line-height: 1.6; color: #e0e0e0;"">Hi {customerName},</p>
            <p style=""font-size: 16px; line-height: 1.6; color: #e0e0e0;"">Thank you for your purchase from Project65 Studios! Your order has been processed successfully.</p>

            <!-- Order Box -->
            <div style=""background-color: #0d0d0d; border-radius: 8px; border: 1px dashed #444444; padding: 24px; margin: 32px 0; text-align: center;"">
                <div style=""font-size: 12px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; margin-bottom: 8px;"">Order Number</div>
                <div style=""font-size: 32px; font-weight: bold; color: #3b82f6; letter-spacing: 2px;"">{orderId}</div>
            </div>

            <!-- Items -->
            <table style=""width: 100%; border-collapse: collapse; margin-bottom: 32px;"">
                <thead>
                    <tr style=""border-bottom: 1px solid #333333;"">
                        <th style=""text-align: left; padding-bottom: 12px; color: #888888; font-size: 12px; text-transform: uppercase;"">Item</th>
                        <th style=""text-align: right; padding-bottom: 12px; color: #888888; font-size: 12px; text-transform: uppercase;"">Price</th>
                    </tr>
                </thead>
                <tbody>
                    {itemRows}
                </tbody>
                <tfoot>
                    <tr style=""border-top: 2px solid #333333;"">
                        <td style=""padding-top: 16px; font-weight: bold; font-size: 18px;"">Total</td>
                        <td style=""padding-top: 16px; text-align: right; font-weight: bold; font-size: 18px; color: #3b82f6;"">${total:N2}</td>
                    </tr>
                </tfoot>
            </table>

            <p style=""font-size: 14px; color: #888888; text-align: center; margin-top: 40px;"">
                You can access your high-resolution downloads anytime from your dashboard.
            </p>
        </div>

        <!-- Footer -->
        <div style=""background-color: #0d0d0d; padding: 24px; text-align: center; border-top: 1px solid #333333; font-size: 12px; color: #666666;"">
            &copy; {DateTime.UtcNow.Year} Project65 Studios. All rights reserved.
        </div>
    </div>
</body>
</html>
" ;
    }
}
