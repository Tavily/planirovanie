using System.ComponentModel.DataAnnotations;

namespace planirovanie.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Organizer { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Participants { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string AdditionalInfo { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public EventCategory? Category { get; set; }

        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Время последнего обновления записи. Nullable — если запись ещё не обновлялась.
        public DateTime? UpdatedAt { get; set; }

        public bool IsLocked { get; set; }
    }

    public class EventCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}