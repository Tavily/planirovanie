using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using planirovanie.Models;

namespace planirovanie.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<EventCategory>().HasData(
                new EventCategory { Id = 1, Name = "С участием Главы города" },
                new EventCategory { Id = 2, Name = "С участием городских СМИ" },
                new EventCategory { Id = 3, Name = "В режиме видеоконференции (ВКС)" },
                new EventCategory { Id = 4, Name = "С участием Депутатов Волгодонской городской Думы" }
            );
        }
    }
}