// Services/SchedulerStateService.cs
using Microsoft.EntityFrameworkCore;
using planirovanie.Data;
using planirovanie.Models;

namespace planirovanie.Services;

public class SchedulerStateService
{
    private readonly EventService _eventService;
    private readonly ApplicationDbContext _db;
    public List<EventCategory> Categories { get; private set; } = new();
    public DateTime CurrentDate { get; private set; } = DateTime.Today;
    public int SelectedCategoryFilter { get; set; }
    public CalendarView CurrentView { get; private set; } = CalendarView.Month;

    private List<Event> _events = new();
    public IEnumerable<Event> VisibleEvents =>
        _events.Where(e => SelectedCategoryFilter == 0 || e.CategoryId == SelectedCategoryFilter);

    public event Action? StateChanged;

    public SchedulerStateService(EventService eventService, ApplicationDbContext db)
    {
        _eventService = eventService;
        _db = db;
    }

    public async Task InitializeAsync()
    {
        Categories = await _db.EventCategories.OrderBy(c => c.Id).ToListAsync();
        await LoadEventsAsync();
        NotifyStateChanged();
    }

    public async Task SetViewAsync(CalendarView view)
    {
        CurrentView = view;
        await LoadEventsAsync();
        NotifyStateChanged();
    }

    public async Task PreviousPeriodAsync()
    {
        CurrentDate = CurrentView switch
        {
            CalendarView.Day => CurrentDate.AddDays(-1),
            CalendarView.Week => CurrentDate.AddDays(-7),
            CalendarView.Month => CurrentDate.AddMonths(-1),
            _ => CurrentDate
        };
        await LoadEventsAsync();
        NotifyStateChanged();
    }

    public async Task NextPeriodAsync()
    {
        CurrentDate = CurrentView switch
        {
            CalendarView.Day => CurrentDate.AddDays(1),
            CalendarView.Week => CurrentDate.AddDays(7),
            CalendarView.Month => CurrentDate.AddMonths(1),
            _ => CurrentDate
        };
        await LoadEventsAsync();
        NotifyStateChanged();
    }

    public async Task GoToDateAsync(DateTime date)
    {
        CurrentDate = date.Date;
        CurrentView = CalendarView.Day;
        await LoadEventsAsync();
        NotifyStateChanged();
    }

    private async Task LoadEventsAsync()
    {
        var (start, end) = GetVisibleRange();
        _events = await _eventService.GetEventsByDateRangeAsync(start, end);
    }

    private (DateTime start, DateTime end) GetVisibleRange() => CurrentView switch
    {
        CalendarView.Day => (CurrentDate.Date, CurrentDate.Date.AddDays(1).AddTicks(-1)),
        CalendarView.Week => GetWeekRange(),
        CalendarView.Month => GetMonthRange(),
        _ => (CurrentDate, CurrentDate)
    };

    private (DateTime, DateTime) GetWeekRange()
    {
        int diff = (7 + ((int)CurrentDate.DayOfWeek - 1)) % 7;
        var start = CurrentDate.AddDays(-diff).Date;
        return (start, start.AddDays(7).AddTicks(-1));
    }

    private (DateTime, DateTime) GetMonthRange()
    {
        var firstDay = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
        int daysBefore = ((int)firstDay.DayOfWeek + 6) % 7;
        var start = firstDay.AddDays(-daysBefore);
        int totalDays = (start.AddDays(35).Month == CurrentDate.Month) ? 42 : 35;
        var end = start.AddDays(totalDays).AddTicks(-1);
        return (start, end);
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}