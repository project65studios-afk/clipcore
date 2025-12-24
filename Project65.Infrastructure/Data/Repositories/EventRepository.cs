using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(string id)
    {
        return await _context.Events
            .Include(e => e.Clips)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Event>> ListAsync()
    {
        return await _context.Events
            .Include(e => e.Clips)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task AddAsync(Event evt)
    {
        await _context.Events.AddAsync(evt);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Event evt)
    {
        _context.Events.Update(evt);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var evt = await _context.Events
            .Include(e => e.Clips)
            .FirstOrDefaultAsync(e => e.Id == id);
            
        if (evt != null)
        {
            // Note: DB will cascade delete clips if foreign keys are setup that way.
            // But we should return the assets to the caller or handle it here?
            // The plan said the UI will handle Mux deletion. 
            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
        }
    }
}
