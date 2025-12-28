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

    public async Task<List<Event>> SearchAsync(string query)
    {
        return await _context.Events
            .AsNoTracking()
            .Include(e => e.Clips) // Include clips so we can show counts
            .Where(e => e.Name.ToLower().Contains(query.ToLower()) || (e.Summary != null && e.Summary.ToLower().Contains(query.ToLower())))
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
        var existing = await _context.Events.FirstOrDefaultAsync(e => e.Id == evt.Id);
        
        if (existing != null)
        {
            existing.Name = evt.Name;
            existing.Date = evt.Date;
            existing.Location = evt.Location;
            existing.Summary = evt.Summary;
            // Note: We are not updating CreatedAt or Clips here to avoid complexity
            
            await _context.SaveChangesAsync();
        }
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
