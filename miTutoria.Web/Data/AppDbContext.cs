using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data.Entities.Academic;
using miTutoria.Web.Data.Entities.Auth;
using miTutoria.Web.Data.Entities.Billing;

namespace miTutoria.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Family> Families => Set<Family>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<TokenEvent> TokenEvents => Set<TokenEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");

        modelBuilder.Entity<Family>().ToTable("families", "auth");
        modelBuilder.Entity<User>().ToTable("users", "auth");
        modelBuilder.Entity<Subject>().ToTable("subjects", "academic");
        modelBuilder.Entity<Classroom>().ToTable("classrooms", "academic");
        modelBuilder.Entity<Message>().ToTable("messages", "academic");
        modelBuilder.Entity<TokenEvent>().ToTable("token_events", schema: "billing");

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Message>()
            .Property(m => m.Role)
            .HasConversion<string>();
    }
}
