using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VideoStudio.Models;

namespace VideoStudio.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Service> Services { get; set; }

    public DbSet<PortfolioItem> PortfolioItems { get; set; }

    public DbSet<Booking> Bookings { get; set; }

    public DbSet<Review> Reviews { get; set; }

    public DbSet<ContactMessage> ContactMessages { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<Notification> Notifications { get; set; }
}