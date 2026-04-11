using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Models;

namespace TeamTracker.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();
    public DbSet<MonthlyKpi> MonthlyKpis => Set<MonthlyKpi>();
    public DbSet<InviteLink> InviteLinks => Set<InviteLink>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<MonthlyKpi>()
            .HasIndex(k => new { k.MemberId, k.Year, k.Month })
            .IsUnique();

        mb.Entity<DailyLog>()
            .HasIndex(d => new { d.MemberId, d.Date });

        mb.Entity<InviteLink>()
            .HasIndex(i => i.Code)
            .IsUnique();

        mb.Entity<Organization>()
            .HasMany(o => o.Users)
            .WithOne(u => u.Organization)
            .HasForeignKey(u => u.OrganizationId);

        mb.Entity<Organization>()
            .HasMany(o => o.Teams)
            .WithOne(t => t.Organization)
            .HasForeignKey(t => t.OrganizationId);
    }
}
