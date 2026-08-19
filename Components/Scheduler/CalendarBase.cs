using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using planirovanie.Data;
using planirovanie.Models;
using planirovanie.Services;
using Microsoft.EntityFrameworkCore;

namespace planirovanie.Components.Scheduler
{
    public class CalendarBase : ComponentBase
    {
        [Inject] protected EventService EventSvc { get; set; } = default!;
        [Inject] protected ApplicationDbContext Db { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        protected DateTime CurrentDate { get; set; } = DateTime.Today;
        protected List<Event> Events { get; set; } = new();
        protected List<EventCategory> Categories { get; set; } = new();
        protected int SelectedCategoryFilter { get; set; }
        protected bool IsFormOpen { get; set; }
        protected EventFormModel EditingEvent { get; set; } = new();
        protected string FormMessage { get; set; } = string.Empty;
        protected bool ShowImportPanel { get; set; }

        // Кэш праздников
        protected HashSet<DateTime> _stateHolidays = new();

        protected override async Task OnInitializedAsync()
        {
            InitializeHolidays();
            await LoadCategoriesAsync();
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

        protected async Task LoadCategoriesAsync()
        {
            Categories = await Db.EventCategories.OrderBy(c => c.Id).ToListAsync();
            if (Categories.Count > 0 && EditingEvent.CategoryId == 0)
            {
                EditingEvent.CategoryId = Categories[0].Id;
            }
        }

        protected async Task LoadEventsForRange(DateTime start, DateTime end)
        {
            try
            {
                Events = await EventSvc.GetEventsByDateRangeAsync(start, end);
            }
            catch
            {
                Events = new List<Event>();
            }
        }

        protected IEnumerable<Event> VisibleEvents => Events.Where(e => SelectedCategoryFilter == 0 || e.CategoryId == SelectedCategoryFilter);

        protected List<DateTime> GetMonthDays()
        {
            var firstDay = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
            int daysBefore = ((int)firstDay.DayOfWeek + 6) % 7;
            var start = firstDay.AddDays(-daysBefore);
            int totalDays = (start.AddDays(35).Month == CurrentDate.Month) ? 42 : 35;
            return Enumerable.Range(0, totalDays).Select(i => start.AddDays(i)).ToList();
        }

        protected List<DateTime> GetWeekDays()
        {
            var current = CurrentDate.Date;
            int diff = (7 + ((int)current.DayOfWeek - 1)) % 7;
            var startOfWeek = current.AddDays(-diff);
            return Enumerable.Range(0, 7).Select(i => startOfWeek.AddDays(i)).ToList();
        }

        protected List<Event> GetEventsForDay(DateTime day)
        {
            return VisibleEvents.Where(e => e.StartDate.Date == day.Date).ToList();
        }

        protected bool IsHoliday(DateTime day)
        {
            return _stateHolidays.Contains(day.Date) || day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
        }

        protected bool IsPreHoliday(DateTime day)
        {
            var nextDay = day.AddDays(1).Date;
            bool isNextDayStateHoliday = _stateHolidays.Contains(nextDay);
            return isNextDayStateHoliday && day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday;
        }

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
                        return colorValue;
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

        protected void OnCategoryFilterChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var id))
            {
                SelectedCategoryFilter = id;
                StateHasChanged();
            }
        }

        protected void ToggleImportPanel() => ShowImportPanel = !ShowImportPanel;

        protected void OpenCreateDialog()
        {
            EditingEvent = new EventFormModel
            {
                StartDate = CurrentDate.Date.AddHours(9),
                EndDate = CurrentDate.Date.AddHours(10),
                CategoryId = Categories.FirstOrDefault()?.Id ?? 0
            };
            FormMessage = string.Empty;
            IsFormOpen = true;
        }

        protected void OpenEditDialog(Event evt)
        {
            EditingEvent = new EventFormModel
            {
                Id = evt.Id,
                Title = evt.Title,
                StartDate = evt.StartDate,
                EndDate = evt.EndDate ?? evt.StartDate.AddHours(1),
                Location = evt.Location,
                Organizer = evt.Organizer,
                Participants = evt.Participants,
                AdditionalInfo = evt.AdditionalInfo,
                CategoryId = evt.CategoryId
            };
            FormMessage = string.Empty;
            IsFormOpen = true;
        }

        protected void CloseForm()
        {
            IsFormOpen = false;
            EditingEvent = new EventFormModel();
            FormMessage = string.Empty;
        }

        protected async Task SaveEvent()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditingEvent.Title))
                {
                    FormMessage = "Введите название мероприятия.";
                    return;
                }

                if (!EditingEvent.StartDate.HasValue)
                {
                    FormMessage = "Введите дату и время начала мероприятия.";
                    return;
                }

                var start = EditingEvent.StartDate.Value;
                var end = EditingEvent.EndDate ?? EditingEvent.StartDate.Value.AddHours(1);

                if (end <= start)
                {
                    FormMessage = "Время окончания должно быть позже времени начала.";
                    return;
                }

                var entity = new Event
                {
                    Id = EditingEvent.Id,
                    Title = EditingEvent.Title,
                    StartDate = start,
                    EndDate = end,
                    Location = EditingEvent.Location,
                    Organizer = EditingEvent.Organizer,
                    Participants = EditingEvent.Participants,
                    AdditionalInfo = EditingEvent.AdditionalInfo,
                    CategoryId = EditingEvent.CategoryId
                };

                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.Identity?.Name ?? "system";
                var userRole = authState.User.IsInRole("Administrator") ? "Administrator"
                    : authState.User.IsInRole("Executor") ? "Executor"
                    : "User";

                if (EditingEvent.Id > 0)
                {
                    await EventSvc.UpdateEventAsync(entity, userId, userRole);
                }
                else
                {
                    await EventSvc.AddEventAsync(entity, userId, userRole);
                }

                CloseForm();
                await LoadEventsForRange(GetVisibleStart(), GetVisibleEnd());
            }
            catch (InvalidOperationException ex)
            {
                FormMessage = ex.Message;
            }
            catch (Exception ex)
            {
                FormMessage = $"Ошибка сохранения: {ex.Message}";
            }
        }

        protected async Task DeleteCurrentEvent()
        {
            if (EditingEvent.Id > 0)
            {
                await EventSvc.DeleteEventAsync(EditingEvent.Id);
                CloseForm();
                await LoadEventsForRange(GetVisibleStart(), GetVisibleEnd());
            }
        }

        // Вспомогательные методы для загрузки видимого диапазона по умолчанию
        protected DateTime GetVisibleStart() => CurrentDate.Date;
        protected DateTime GetVisibleEnd() => CurrentDate.Date.AddDays(1).AddTicks(-1);

        public class EventFormModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string Organizer { get; set; } = string.Empty;
            public string Participants { get; set; } = string.Empty;
            public string AdditionalInfo { get; set; } = string.Empty;
            public int CategoryId { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public bool IsAllDay { get; set; }
        }
    }
}
