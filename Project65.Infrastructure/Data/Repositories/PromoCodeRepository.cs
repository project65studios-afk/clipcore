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
        private readonly AppDbContext _context;

        public PromoCodeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PromoCode?> GetByIdAsync(int id)
        {
            return await _context.PromoCodes.FindAsync(id);
        }

        public async Task<PromoCode?> GetByCodeAsync(string code)
        {
            return await _context.PromoCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper());
        }

        public async Task<List<PromoCode>> ListAsync()
        {
            return await _context.PromoCodes
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(PromoCode promoCode)
        {
            _context.PromoCodes.Add(promoCode);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PromoCode promoCode)
        {
            var trackedEntity = _context.PromoCodes.Local.FirstOrDefault(e => e.Id == promoCode.Id);
            if (trackedEntity != null)
            {
                _context.Entry(trackedEntity).State = EntityState.Detached;
            }

            _context.PromoCodes.Update(promoCode);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode != null)
            {
                _context.PromoCodes.Remove(promoCode);
                await _context.SaveChangesAsync();
            }
        }

        public async Task IncrementUsageAsync(int id)
        {
            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode != null)
            {
                promoCode.UsageCount++;
                await _context.SaveChangesAsync();
            }
        }
    }
}
