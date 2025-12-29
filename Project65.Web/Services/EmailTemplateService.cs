using System.Text;
using Project65.Core.Entities;

namespace Project65.Web.Services;

public class EmailTemplateService
{
    public string GenerateOrderReceiptHtml(string orderNumber, List<Purchase> purchases, string customerName)
    {
        var sb = new StringBuilder();
        int totalCents = purchases.Sum(p => p.PricePaidCents);
        
        sb.Append($@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: #0f172a; color: #ffffff; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; letter-spacing: 1px; }}
        .content {{ padding: 30px; }}
        .order-number {{ background: #f8fafc; border: 1px dashed #cbd5e1; padding: 15px; text-align: center; margin-bottom: 25px; border-radius: 6px; }}
        .order-number span {{ display: block; font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: bold; }}
        .order-number strong {{ font-size: 20px; color: #0f172a; font-family: monospace; }}
        .item-list {{ width: 100%; border-collapse: collapse; margin-bottom: 25px; }}
        .item-list th {{ text-align: left; border-bottom: 2px solid #f1f5f9; padding-bottom: 10px; color: #64748b; font-size: 12px; text-transform: uppercase; }}
        .item-list td {{ padding: 15px 0; border-bottom: 1px solid #f1f5f9; vertical-align: top; }}
        .item-title {{ font-weight: bold; color: #0f172a; display: block; }}
        .item-details {{ font-size: 13px; color: #64748b; }}
        .price {{ text-align: right; font-weight: bold; color: #0f172a; }}
        .totals {{ float: right; width: 200px; }}
        .totals-row {{ display: flex; justify-content: space-between; padding: 5px 0; }}
        .total {{ font-size: 18px; font-weight: bold; color: #0f172a; border-top: 2px solid #f1f5f9; margin-top: 10px; padding-top: 10px; }}
        .footer {{ background: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; }}
        .btn {{ display: inline-block; background: #0ea5e9; color: #ffffff; padding: 12px 25px; text-decoration: none; border-radius: 6px; font-weight: bold; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Order Confirmed</h1>
        </div>
        <div class='content'>
            <p>Hi {customerName},</p>
            <p>Thank you for your purchase from Project65 Studios! Your order has been processed successfully.</p>
            
            <div class='order-number'>
                <span>Order Number</span>
                <strong>{orderNumber.ToUpper()}</strong>
            </div>

            <table class='item-list'>
                <thead>
                    <tr>
                        <th>Item</th>
                        <th style='text-align: right;'>Price</th>
                    </tr>
                </thead>
                <tbody>");

        foreach (var p in purchases)
        {
            var eventName = p.EventName ?? "Event";
            var clipTitle = p.ClipTitle ?? $"Clip #{p.ClipId}";
            sb.Append($@"
                    <tr>
                        <td>
                            <span class='item-title'>{clipTitle}</span>
                            <span class='item-details'>{eventName}</span>
                        </td>
                        <td class='price'>${(p.PricePaidCents / 100.0).ToString("N2")}</td>
                    </tr>");
        }

        sb.Append($@"
                </tbody>
            </table>

            <div style='overflow: hidden;'>
                <div class='totals'>
                    <div class='total totals-row'>
                        <span>Total Paid</span>
                        <span>${(totalCents / 100.0).ToString("N2")}</span>
                    </div>
                </div>
            </div>

            <div style='text-align: center;'>
                <a href='https://project65.com/my-purchases' class='btn'>View Your Clips</a>
            </div>
        </div>
        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} Project65 Studios. All rights reserved.<br/>
            If you have any questions, please reply to this email.
        </div>
    </div>
</body>
</html>");

        return sb.ToString();
    }
}
