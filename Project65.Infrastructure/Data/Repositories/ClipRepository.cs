using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Data.Repositories;

public class ClipRepository : IClipRepository
{
    private readonly AppDbContext _context;

    public ClipRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Clip?> GetByIdAsync(string id)
    {
        return await _context.Clips
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddAsync(Clip clip)
    {
        await _context.Clips.AddAsync(clip);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Clip clip)
    {
        _context.Entry(clip).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
}
