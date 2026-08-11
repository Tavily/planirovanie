using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using planirovanie.Data;
using planirovanie.Models;
using System.Globalization;

namespace planirovanie.Components.Scheduler;

public class CalendarViewBase : ComponentBase
{
    [Inject] protected planirovanie.Services.EventService EventSvc { get; set; } = default!;
    [Inject] protected ApplicationDbContext Db { get; set; } = default!;
    
    [Parameter] public DateTime CurrentDate { get; set; } = DateTime.Today;
    [Parameter] public EventCallback<DateTime> CurrentDateChanged { get; set; }
    
    [Parameter] public int SelectedCategoryFilter { get; set; }
    [Parameter] public EventCallback<int> SelectedCategoryFilterChanged { get; set; }
    
    [Parameter] public EventCallback<DateTime> OnDayClick { get; set; }
    [Parameter] public EventCallback<Event> OnEventClick { get; set; }
    [Parameter] public EventCallback OnCreateEvent { get; set; }
    
    protected List<Event> Events { get; set; } = new();
    protected List<EventCategory> Categories { get; set; } = new();
    
    private HashSet<DateTime> _stateHolidays = new();
    
    protected override async Task OnInitializedAsync()
    {
        InitializeHolidays();
        await LoadCategoriesAsync();
        await LoadEventsAsync();
    }
    
    protected IEnumerable<Event> VisibleEvents => Events.Where(e => SelectedCategoryFilter == 0 || e.CategoryId == SelectedCategoryFilter);
    
    protected async Task LoadCategoriesAsync()
    {
        Categories = await Db.EventCategories.OrderBy(c => c.Id).ToListAsync();
    }
    
    protected async Task LoadEventsAsync()
    {
        try
        {
            var start = GetVisibleStart();
            var end = GetVisibleEnd();
            Events = await EventSvc.GetEventsByDateRangeAsync(start, end);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadEventsAsync failed: {ex}");
            Events = new List<Event>();
        }
    }
    
    protected void InitializeHolidays()
    {
        _stateHolidays.Clear();
        for (int year = CurrentDate.Year - 1; year <= CurrentDate.Year + 1; year++)
        {
            _stateHolidays.Add(new DateTime(year, 1, 1));
            _stateHolidays.Add(new DateTime(year, 1, 7));
            _stateHolidays.Add(new DateTime(year, 2, 23));
            _stateHolidays.Add(new DateTime(year, 3, 8));
            _stateHolidays.Add(new DateTime(year, 5, 1));
            _stateHolidays.Add(new DateTime(year, 5, 9));
            _stateHolidays.Add(new DateTime(year, 6, 12));
            _stateHolidays.Add(new DateTime(year, 11, 4));
        }
    }
    
    protected bool IsHoliday(DateTime day) => 
        _stateHolidays.Contains(day.Date) || day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
    
    protected bool IsPreHoliday(DateTime day)
    {
        var nextDay = day.AddDays(1).Date;
        return _stateHolidays.Contains(nextDay) && day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday;
    }
    
    protected List<Event> GetEventsForDay(DateTime day) => 
        VisibleEvents.Where(e => e.StartDate.Date == day.Date).ToList();
    
    protected string GetCategoryColor(int categoryId)
    {
        var category = Categories.FirstOrDefault(c => c.Id == categoryId);
        
        if (category != null)
        {
            var colorProperty = category.GetType().GetProperty("ColorCode");
            if (colorProperty != null)
            {
                var colorValue = colorProperty.GetValue(category) as string;
                if (!string.IsNullOrEmpty(colorValue))
                {
                    return colorValue;
                }
            }
        }
        
        return category?.Name switch
        {
            "С участием Главы города" => "#dc3545",
            "С участием городских СМИ" => "#0d6efd",
            "В режиме видеоконференции (ВКС)" => "#198754",
            "С участием Депутатов Волгодонской городской Думы" => "#6f42c1",
            "-" => "#6c757d",
            _ => "#6c757d"
        };
    }
    
    protected virtual DateTime GetVisibleStart() => CurrentDate.Date;
    protected virtual DateTime GetVisibleEnd() => CurrentDate.Date.AddDays(1).AddTicks(-1);
    
    protected async Task HandleDayClick(DateTime day)
    {
        if (OnDayClick.HasDelegate)
        {
            await OnDayClick.InvokeAsync(day);
        }
    }
    
    protected async Task HandleEventClick(Event evt)
    {
        if (OnEventClick.HasDelegate)
        {
            await OnEventClick.InvokeAsync(evt);
        }
    }
    
    protected async Task HandleCreateEvent()
    {
        if (OnCreateEvent.HasDelegate)
        {
            await OnCreateEvent.InvokeAsync();
        }
    }
    
    protected async Task PreviousPeriod(Action<DateTime> setDate)
    {
        var newDate = CurrentDate.AddDays(-1);
        setDate(newDate);
        if (CurrentDateChanged.HasDelegate)
        {
            await CurrentDateChanged.InvokeAsync(newDate);
        }
        await LoadEventsAsync();
    }
    
    protected async Task NextPeriod(Action<DateTime> setDate)
    {
        var newDate = CurrentDate.AddDays(1);
        setDate(newDate);
        if (CurrentDateChanged.HasDelegate)
        {
            await CurrentDateChanged.InvokeAsync(newDate);
        }
        await LoadEventsAsync();
    }
}
