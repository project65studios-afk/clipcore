using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;
using Project65.Infrastructure.Data;

namespace Project65.Infrastructure.Data.Repositories
{
    public class PromoCodeRepository : IPromoCodeRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public PromoCodeRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<PromoCode?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.PromoCodes.FindAsync(id);
        }

        public async Task<PromoCode?> GetByCodeAsync(string code)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.PromoCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper());
        }

        public async Task<List<PromoCode>> ListAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.PromoCodes
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(PromoCode promoCode)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.PromoCodes.Add(promoCode);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PromoCode promoCode)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.PromoCodes.Update(promoCode);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var promoCode = await context.PromoCodes.FindAsync(id);
            if (promoCode != null)
            {
                context.PromoCodes.Remove(promoCode);
                await context.SaveChangesAsync();
            }
        }

        public async Task IncrementUsageAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var promoCode = await context.PromoCodes.FindAsync(id);
            if (promoCode != null)
            {
                promoCode.UsageCount++;
                await context.SaveChangesAsync();
            }
        }
    }
}
