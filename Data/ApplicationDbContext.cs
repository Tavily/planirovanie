using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using planirovanie.Models;

namespace planirovanie.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Указываем, что таблица всё равно называется AspNetUsers
            builder.Entity<ApplicationUser>().ToTable("AspNetUsers");

            builder.Entity<EventCategory>().HasData(
                new EventCategory { Id = 1, Name = "С участием Главы города" },
                new EventCategory { Id = 2, Name = "С участием городских СМИ" },
                new EventCategory { Id = 3, Name = "В режиме видеоконференции (ВКС)" },
                new EventCategory { Id = 4, Name = "С участием Депутатов Волгодонской городской Думы" }
            );

            // Настройка таблицы связей EventParticipants
            builder.Entity<EventParticipant>()
                .HasKey(ep => new { ep.EventId, ep.UserId, ep.Role });

            builder.Entity<EventParticipant>()
                .HasOne(ep => ep.Event)
                .WithMany(e => e.EventParticipants)
                .HasForeignKey(ep => ep.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EventParticipant>()
                .HasOne(ep => ep.User)
                .WithMany(u => u.EventParticipants)
                .HasForeignKey(ep => ep.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}