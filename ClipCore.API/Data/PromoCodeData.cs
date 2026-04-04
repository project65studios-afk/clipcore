using ClipCore.API.Interfaces;
using ClipCore.Core.Entities;

namespace ClipCore.API.Data;

public class PromoCodeData : IPromoCodeData
{
    private readonly ISqlDataAccess _db;
    public PromoCodeData(ISqlDataAccess db) => _db = db;

    public Task<PromoCode?> GetByCodeAsync(string code) =>
        _db.LoadSingle<PromoCode, dynamic>(
            @"SELECT * FROM ""PromoCodes"" WHERE UPPER(""Code"")=UPPER(@Code)",
            new { Code = code });

    public Task<IEnumerable<PromoCode>> ListAsync() =>
        _db.LoadData<PromoCode>(@"SELECT * FROM ""PromoCodes"" ORDER BY ""CreatedAt"" DESC");

    public Task AddAsync(PromoCode p) =>
        _db.SaveData(
            @"INSERT INTO ""PromoCodes"" (""Code"",""DiscountType"",""Value"",""MaxUsages"",""UsageCount"",""ExpiryDate"",""IsActive"",""CreatedAt"") VALUES (@Code,@DiscountType,@Value,@MaxUsages,0,@ExpiryDate,@IsActive,NOW())",
            new { p.Code, DiscountType = (int)p.DiscountType, p.Value, p.MaxUsages, p.ExpiryDate, p.IsActive });

    public Task IncrementUsageAsync(int id) =>
        _db.SaveData(@"UPDATE ""PromoCodes"" SET ""UsageCount""=""UsageCount""+1 WHERE ""Id""=@Id", new { Id = id });

    public Task DeleteAsync(int id) =>
        _db.SaveData(@"DELETE FROM ""PromoCodes"" WHERE ""Id""=@Id", new { Id = id });
}
