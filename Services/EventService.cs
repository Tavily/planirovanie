using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using planirovanie.Data;
using planirovanie.Models;

namespace planirovanie.Services
{
    public class EventService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EventService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public bool CanAddEvent(DateTime eventDate)
        {
            var now = DateTime.Now;

            if (now.DayOfWeek == DayOfWeek.Thursday && now.Hour >= 12)
            {
                if (eventDate <= now.AddDays(7))
                    return false;
            }

            if (eventDate.Month > now.Month && now.Day > 25)
                return false;

            return true;
        }

        public async Task<List<EventCategory>> GetCategoriesAsync()
        {
            return await _context.EventCategories.OrderBy(c => c.Id).ToListAsync();
        }

        public async Task AddEventAsync(Event newEvent, string userId)
        {
            if (!CanAddEvent(newEvent.StartDate))
                throw new InvalidOperationException("Срок ввода данного плана истек согласно Регламенту.");

            newEvent.CreatedByUserId = userId;
            newEvent.CreatedAt = DateTime.UtcNow;
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateEventAsync(Event updatedEvent, string userId)
        {
            var existing = await _context.Events.FindAsync(updatedEvent.Id);
            if (existing == null)
            {
                await AddEventAsync(updatedEvent, userId);
                return;
            }

            existing.Title = updatedEvent.Title;
            existing.StartDate = updatedEvent.StartDate;
            existing.EndDate = updatedEvent.EndDate;
            existing.Location = updatedEvent.Location;
            existing.Organizer = updatedEvent.Organizer;
            existing.Participants = updatedEvent.Participants;
            existing.AdditionalInfo = updatedEvent.AdditionalInfo;
            existing.CategoryId = updatedEvent.CategoryId;
            existing.CreatedByUserId = userId;
            existing.CreatedAt = existing.CreatedAt == default ? DateTime.UtcNow : existing.CreatedAt;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteEventAsync(int id)
        {
            var existing = await _context.Events.FindAsync(id);
            if (existing != null)
            {
                _context.Events.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Event>> GetEventsByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Where(e => e.StartDate >= start && e.StartDate <= end)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }
    }
}