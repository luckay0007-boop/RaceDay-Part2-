using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(u => u.Gender).HasMaxLength(10);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.City).HasMaxLength(100);
            entity.Property(u => u.Province).HasMaxLength(100);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");

            // CHECK: Gender IN ('Male', 'Female', 'Other')
            entity.ToTable(t => t.HasCheckConstraint("CK_Users_Gender",
                "Gender IN ('Male', 'Female', 'Other')"));
        });

        // Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.RoleId);
            entity.HasIndex(r => r.RoleName).IsUnique();
            entity.Property(r => r.RoleName).IsRequired().HasMaxLength(50);

            // Seed the two required roles
            entity.HasData(
                new Role { RoleId = 1, RoleName = "Organiser" },
                new Role { RoleId = 2, RoleName = "Participant" }
            );
        });

        // UserRoles
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => ur.UserRoleId);
            entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Events
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Location).IsRequired().HasMaxLength(300);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Province).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Draft");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.OrganisedEvents)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.NoAction);

            // CHECK constraints
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Events_EventType",
                    "EventType IN ('Running', 'Walking', 'Cycling')");
                t.HasCheckConstraint("CK_Events_Status",
                    "Status IN ('Draft', 'Published', 'Completed', 'Cancelled')");
            });
        });

        // Categories 
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.CategoryId);

            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Distance).IsRequired().HasColumnType("decimal(6,2)");
            entity.Property(c => c.EntryFee).IsRequired().HasColumnType("decimal(10,2)").HasDefaultValue(0.00m);
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Categories_Distance", "Distance > 0");
                t.HasCheckConstraint("CK_Categories_EntryFee", "EntryFee >= 0");
            });
        });

        // Enrolments 
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasKey(en => en.EnrolmentId);
            entity.HasIndex(en => new { en.ParticipantId, en.CategoryId }).IsUnique();

            entity.Property(en => en.BibNumber).HasMaxLength(20);
            entity.Property(en => en.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Registered");
            entity.Property(en => en.EnrolmentDate).HasDefaultValueSql("GETDATE()");
            entity.Property(en => en.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(en => en.Participant)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(en => en.ParticipantId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(en => en.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(en => en.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status",
                "Status IN ('Registered', 'Confirmed', 'Withdrawn', 'DNS')"));
        });

        // Results
        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(r => r.ResultId);
            entity.HasIndex(r => r.EnrolmentId).IsUnique();

            entity.Property(r => r.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Finished");
            entity.Property(r => r.RecordedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(r => r.Enrolment)
                  .WithOne(en => en.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Results_Status",
                    "Status IN ('Finished', 'DNF', 'DSQ')");
                t.HasCheckConstraint("CK_Results_Position",
                    "Position IS NULL OR Position > 0");
            });
        });
    }
}
