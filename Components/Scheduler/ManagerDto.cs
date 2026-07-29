using System.ComponentModel.DataAnnotations;

namespace planirovanie.Components.Scheduler
{
    public class ManagerDto
    {
        public int Id { get; set; }

        [StringLength(250)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Color { get; set; } = string.Empty;
    }
}
