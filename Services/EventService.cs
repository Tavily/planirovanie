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

        public bool CanAddEvent(DateTime eventDate, string? userRole = null)
        {
            // ПРАВИЛО ДЛЯ РОЛЕЙ: обход всех ограничений для Администратора и Исполнителя
            if (!string.IsNullOrEmpty(userRole) &&
                (userRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                 userRole.Equals("Executor", StringComparison.OrdinalIgnoreCase)))
            {
                return true; // Administrator и Executor могут вносить изменения в любое время
            }

            var now = GetMoscowNow();
            var eventDateOnly = eventDate.Date;
            var nowDateOnly = now.Date;

            // Находим понедельник текущей недели
            int daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
            var currentMonday = nowDateOnly.AddDays(-daysSinceMonday);
            
            // Определяем границы "следующей недели" (пн-вс)
            var nextMonday = currentMonday.AddDays(7);
            var nextSunday = nextMonday.AddDays(6);
            
            // Дедлайн: четверг текущей недели, 12:00
            var deadlineThursday = currentMonday.AddDays(3).AddHours(12);
            
            // Период действия запрета: с четверга 12:00 до конца воскресенья
            var banLiftsAt = currentMonday.AddDays(7);
            
            if (now >= deadlineThursday && now < banLiftsAt)
            {
                if (eventDateOnly >= nextMonday && eventDateOnly <= nextSunday)
                    return false;
            }

            // Месячные планы
            var plannedMonthStart = new DateTime(eventDate.Year, eventDate.Month, 1);
            var previousMonth = plannedMonthStart.AddMonths(-1);
            
            if (now.Year == previousMonth.Year && 
                now.Month == previousMonth.Month && 
                now.Day > 25)
            {
                return false;
            }
            
            if (nowDateOnly >= plannedMonthStart)
            {
                return false;
            }

            // Годовые планы
            if (eventDate.Year > now.Year)
            {
                var yearDeadline = new DateTime(now.Year, 12, 20, 23, 59, 59);
                if (now > yearDeadline)
                    return false;
            }

            return true;
        }

        public async Task<List<EventCategory>> GetCategoriesAsync()
        {
            return await _context.EventCategories.OrderBy(c => c.Id).ToListAsync();
        }

        private static DateTime GetMoscowNow()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch (TimeZoneNotFoundException)
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
        }

        public async Task AddEventAsync(Event newEvent, string userId, string? userRole = null)
        {
            // Теперь передаем роль пользователя для проверки
            if (!CanAddEvent(newEvent.StartDate, userRole))
                throw new InvalidOperationException("Срок ввода данного плана истек согласно Регламенту Администрации города Волгодонска.");

            newEvent.CreatedByUserId = userId;
            newEvent.CreatedAt = GetMoscowNow();
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateEventAsync(Event updatedEvent, string userId, string? userRole = null)
        {
            // Администратор может обновлять в любое время
            if (!CanAddEvent(updatedEvent.StartDate, userRole))
                throw new InvalidOperationException("Срок изменения данного плана истек согласно Регламенту.");

            var existing = await _context.Events.FindAsync(updatedEvent.Id);
            if (existing == null)
            {
                await AddEventAsync(updatedEvent, userId, userRole);
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
            existing.UpdatedAt = GetMoscowNow();

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