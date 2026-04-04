using ClipCore.API.Interfaces;
using ClipCore.API.Models.Purchase;
using ClipCore.Core.Entities;

namespace ClipCore.API.Data;

public class PurchaseData : IPurchaseData
{
    private readonly ISqlDataAccess _db;
    public PurchaseData(ISqlDataAccess db) => _db = db;

    public Task<IEnumerable<PurchaseItem>> GetPurchasesByUser(string userId) =>
        _db.LoadData<PurchaseItem, dynamic>(
            @"SELECT ""Id"",""ClipId"",""ClipTitle"",""CollectionName"",""CollectionDate"",""PricePaidCents"",""LicenseType"",""FulfillmentStatus"",""CreatedAt"",""HighResDownloadUrl"",""IsGif"" FROM ""Purchases"" WHERE ""UserId""=@UserId ORDER BY ""CreatedAt"" DESC",
            new { UserId = userId });

    public Task<IEnumerable<PurchaseItem>> GetPurchasesByEmail(string email) =>
        _db.LoadData<PurchaseItem, dynamic>(
            @"SELECT ""Id"",""ClipId"",""ClipTitle"",""CollectionName"",""CollectionDate"",""PricePaidCents"",""LicenseType"",""FulfillmentStatus"",""CreatedAt"",""HighResDownloadUrl"",""IsGif"" FROM ""Purchases"" WHERE LOWER(""CustomerEmail"")=LOWER(@Email) ORDER BY ""CreatedAt"" DESC",
            new { Email = email });

    public Task<IEnumerable<PurchaseItem>> GetPurchasesBySeller(int sellerId) =>
        _db.LoadData<PurchaseItem, dynamic>(
            @"SELECT ""Id"",""ClipId"",""ClipTitle"",""CollectionName"",""CollectionDate"",""PricePaidCents"",""LicenseType"",""FulfillmentStatus"",""CreatedAt"",""HighResDownloadUrl"",""IsGif"" FROM ""Purchases"" WHERE ""SellerId""=@SellerId ORDER BY ""CreatedAt"" DESC",
            new { SellerId = sellerId });

    public Task<IEnumerable<PurchaseDetail>> GetBySessionId(string sessionId) =>
        _db.LoadData<PurchaseDetail, dynamic>(
            @"SELECT * FROM ""Purchases"" WHERE ""StripeSessionId""=@SessionId",
            new { SessionId = sessionId });

    public Task<PurchaseDetail?> GetPurchaseDetail(int purchaseId) =>
        _db.LoadSingle<PurchaseDetail, dynamic>(
            @"SELECT * FROM ""Purchases"" WHERE ""Id""=@PurchaseId",
            new { PurchaseId = purchaseId });

    public async Task<bool> HasPurchasedAsync(string? userId, string clipId, LicenseType license)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var n = await _db.ExecuteScalar<int, dynamic>(
            @"SELECT COUNT(1) FROM ""Purchases"" WHERE ""UserId""=@UserId AND ""ClipId""=@ClipId AND ""LicenseType""=@LicenseType",
            new { UserId = userId, ClipId = clipId, LicenseType = (int)license });
        return n > 0;
    }

    public async Task<bool> HasPurchasedGifAsync(string? userId, string clipId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var n = await _db.ExecuteScalar<int, dynamic>(
            @"SELECT COUNT(1) FROM ""Purchases"" WHERE ""UserId""=@UserId AND ""ClipId""=@ClipId AND ""IsGif""=true",
            new { UserId = userId, ClipId = clipId });
        return n > 0;
    }

    // Complex fuzzy search stays inline — too dynamic for a static function
    public async Task<IEnumerable<PurchaseDetail>> ListFiltered(FulfillmentStatus? status = null, DateTime? since = null, string? search = null)
    {
        var conds = new List<string>();
        var p     = new Dictionary<string, object?>();

        if (status.HasValue) { conds.Add(@"""FulfillmentStatus""=@Status"); p["Status"] = (int)status.Value; }
        if (since.HasValue && string.IsNullOrEmpty(search)) { conds.Add(@"""CreatedAt"">=@Since"); p["Since"] = since.Value; }

        if (!string.IsNullOrEmpty(search))
        {
            var s   = search.Trim().ToLower();
            var sz  = s.Replace("o", "0");
            var so  = s.Replace("0", "o");
            var sid = s.StartsWith("ord-") ? s[4..] : s;
            var sidz = sid.Replace("o", "0");
            var sido = sid.Replace("0", "o");
            conds.Add(@"(LOWER(""CustomerEmail"") LIKE '%'||@S||'%' OR LOWER(""CustomerName"") LIKE '%'||@S||'%' OR LOWER(""ClipTitle"") LIKE '%'||@S||'%' OR LOWER(""CollectionName"") LIKE '%'||@S||'%' OR LOWER(""StripeSessionId"") LIKE ANY(ARRAY[@S,@SZ,@SO,@SId,@SIdZ,@SIdO]) OR LOWER(""OrderId"") LIKE ANY(ARRAY[@S,@SZ,@SO,@SId,@SIdZ,@SIdO]))");
            p["S"] = s; p["SZ"] = sz; p["SO"] = so; p["SId"] = sid; p["SIdZ"] = sidz; p["SIdO"] = sido;
        }

        var where = conds.Any() ? "WHERE " + string.Join(" AND ", conds) : "";
        return await _db.LoadData<PurchaseDetail, dynamic>(
            $@"SELECT * FROM ""Purchases"" {where} ORDER BY ""CreatedAt"" DESC", p);
    }

    public async Task<int> CreatePurchase(
        string? userId, string clipId, int sellerId,
        int pricePaidCents, int platformFeeCents, int sellerPayoutCents,
        string stripeSessionId, string orderId, LicenseType licenseType,
        string? customerEmail, string? customerName,
        bool isGif, double? gifStartTime, double? gifEndTime)
    {
        var snap = await _db.LoadSingle<PurchaseSnap, dynamic>(
            "SELECT * FROM cc_s_purchase_snapshot(@ClipId)", new { ClipId = clipId });

        return await _db.ExecuteScalar<int, dynamic>(
            "SELECT cc_i_purchase(@UserId,@ClipId,@SellerId,@ClipTitle,@CollectionName,@CollectionDate,@RecordingStartedAt,@DurationSec,@MasterFileName,@ThumbnailFileName,@StripeSessionId,@OrderId,@PricePaidCents,@PlatformFeeCents,@SellerPayoutCents,@LicenseType,@CustomerEmail,@CustomerName,@IsGif,@GifStartTime,@GifEndTime)",
            new
            {
                UserId = userId, ClipId = clipId, SellerId = sellerId,
                ClipTitle = snap?.ClipTitle, CollectionName = snap?.CollectionName, CollectionDate = snap?.CollectionDate,
                RecordingStartedAt = snap?.RecordingStartedAt, DurationSec = snap?.DurationSec,
                MasterFileName = snap?.MasterFileName, ThumbnailFileName = snap?.ThumbnailFileName,
                StripeSessionId = stripeSessionId, OrderId = orderId,
                PricePaidCents = pricePaidCents, PlatformFeeCents = platformFeeCents, SellerPayoutCents = sellerPayoutCents,
                LicenseType = (int)licenseType, CustomerEmail = customerEmail, CustomerName = customerName,
                IsGif = isGif, GifStartTime = gifStartTime, GifEndTime = gifEndTime
            })
            ?? throw new InvalidOperationException("Failed to create purchase");
    }

    public Task FulfillPurchase(int purchaseId, string highResDownloadUrl, string? muxAssetId) =>
        _db.SaveData("CALL cc_u_purchase_fulfill(@PurchaseId,@HighResDownloadUrl,@MuxAssetId)",
            new { PurchaseId = purchaseId, HighResDownloadUrl = highResDownloadUrl, MuxAssetId = muxAssetId });

    public Task<IEnumerable<SellerSalesSummary>> GetSellerSalesSummary() =>
        _db.LoadData<SellerSalesSummary>("SELECT * FROM cc_s_seller_sales_summary()");

    public Task<IEnumerable<DailyRevenue>> GetDailyRevenue(int days) =>
        _db.LoadData<DailyRevenue, dynamic>("SELECT * FROM cc_s_daily_revenue(@Days)", new { Days = days });

    public Task<IEnumerable<PurchaseItem>> GetRecentSales(int count) =>
        _db.LoadData<PurchaseItem, dynamic>(
            @"SELECT ""Id"",""ClipId"",""ClipTitle"",""CollectionName"",""CollectionDate"",""PricePaidCents"",""LicenseType"",""FulfillmentStatus"",""CreatedAt"",""HighResDownloadUrl"",""IsGif"" FROM ""Purchases"" ORDER BY ""CreatedAt"" DESC LIMIT @Count",
            new { Count = count });

    public async Task<bool> HasUserPurchasedClip(string? userId, string customerEmail, string clipId)
    {
        var n = await _db.ExecuteScalar<int, dynamic>(
            @"SELECT COUNT(1) FROM ""Purchases"" WHERE ""ClipId""=@ClipId AND (""UserId""=@UserId OR LOWER(""CustomerEmail"")=LOWER(@CustomerEmail)) AND ""FulfillmentStatus""=1",
            new { ClipId = clipId, UserId = userId, CustomerEmail = customerEmail });
        return n > 0;
    }

    private class PurchaseSnap
    {
        public string?  ClipTitle { get; set; }
        public string?  CollectionName { get; set; }
        public DateOnly? CollectionDate { get; set; }
        public DateTime? RecordingStartedAt { get; set; }
        public double?  DurationSec { get; set; }
        public string?  MasterFileName { get; set; }
        public string?  ThumbnailFileName { get; set; }
    }
}
