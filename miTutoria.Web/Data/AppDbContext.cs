using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data.Entities;
using miTutoria.Web.Data.Entities.Academic;
using miTutoria.Web.Data.Entities.Auth;
using miTutoria.Web.Data.Entities.Billing;
using miTutoria.Web.Data.Entities.Inbox;

namespace miTutoria.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Family> Families => Set<Family>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<TokenEvent> TokenEvents => Set<TokenEvent>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<InboxMessageRaw> InboxMessagesRaw => Set<InboxMessageRaw>();
    public DbSet<DetectedAssignment> DetectedAssignments => Set<DetectedAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");

        modelBuilder.Entity<Family>().ToTable("families", "auth");
        modelBuilder.Entity<User>().ToTable("users", "auth");
        modelBuilder.Entity<Subject>().ToTable("subjects", "academic");
        modelBuilder.Entity<Classroom>().ToTable("classrooms", "academic");
        modelBuilder.Entity<Message>().ToTable("messages", "academic");
        modelBuilder.Entity<TokenEvent>().ToTable("token_events", schema: "billing");
        modelBuilder.Entity<WaitlistEntry>().ToTable("waitlist_entries", schema: "auth");

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.SchoolLevel)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.Gender)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.ExplanationLevel)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.HasAdhd)
            .HasColumnName("has_adhd");

        modelBuilder.Entity<User>()
            .Property(u => u.Interests)
            .HasColumnName("interests");

        modelBuilder.Entity<User>()
            .Property(u => u.Avatar)
            .HasColumnName("avatar");

        modelBuilder.Entity<User>()
            .Property(u => u.TutorName)
            .HasColumnName("tutor_name");

        modelBuilder.Entity<User>()
            .Property(u => u.TutorAvatar)
            .HasColumnName("tutor_avatar");

        modelBuilder.Entity<User>()
            .Property(u => u.Nickname)
            .HasColumnName("nickname");

        modelBuilder.Entity<User>()
            .Property(u => u.SchoolLevel)
            .HasColumnName("school_level")
            .HasDefaultValue(SchoolLevel.Primario);

        modelBuilder.Entity<Family>()
            .Property(f => f.ParentRole)
            .HasConversion<string>();

        modelBuilder.Entity<ErrorLog>().ToTable("error_logs", "public");
        modelBuilder.Entity<ErrorLog>().Property(e => e.Id).HasColumnName("id");
        modelBuilder.Entity<ErrorLog>().Property(e => e.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<ErrorLog>().Property(e => e.Source).HasColumnName("source");
        modelBuilder.Entity<ErrorLog>().Property(e => e.Message).HasColumnName("message");
        modelBuilder.Entity<ErrorLog>().Property(e => e.Detail).HasColumnName("detail");
        modelBuilder.Entity<ErrorLog>().Property(e => e.Context).HasColumnName("context");

        modelBuilder.Entity<Classroom>()
            .Property(c => c.Name)
            .HasColumnName("name");

        modelBuilder.Entity<Classroom>()
            .Property(c => c.Mode)
            .HasColumnName("mode");

        modelBuilder.Entity<Classroom>()
            .Property(c => c.LastActiveAt)
            .HasColumnName("last_active_at");

        modelBuilder.Entity<Classroom>()
            .Property(c => c.MaterialSections)
            .HasColumnName("material_sections")
            .HasColumnType("jsonb");

        modelBuilder.Entity<Classroom>()
            .Property(c => c.MaterialSectionIndex)
            .HasColumnName("material_section_index");

        modelBuilder.Entity<Classroom>()
            .Property(c => c.MaterialOcrSource)
            .HasColumnName("material_ocr_source");

        modelBuilder.Entity<Family>()
            .Property(f => f.CreatedAt)
            .HasColumnName("created_at");

        modelBuilder.Entity<Family>()
            .Property(f => f.SubscriptionStatus)
            .HasColumnName("subscription_status");

        modelBuilder.Entity<Family>()
            .Property(f => f.TrialEndsAt)
            .HasColumnName("trial_ends_at");

        modelBuilder.Entity<Family>()
            .Property(f => f.PaidUntil)
            .HasColumnName("paid_until");

        modelBuilder.Entity<Family>()
            .Property(f => f.ConsentAt)
            .HasColumnName("consent_at");

        modelBuilder.Entity<Family>()
            .Property(f => f.ConsentIp)
            .HasColumnName("consent_ip");

        modelBuilder.Entity<Family>()
            .Property(f => f.ConsentVersion)
            .HasColumnName("consent_version");

        modelBuilder.Entity<Family>()
            .Property(f => f.CostAlertMarker)
            .HasColumnName("cost_alert_marker");

        modelBuilder.Entity<Family>()
            .Property(f => f.RenewalAlertMarker)
            .HasColumnName("renewal_alert_marker");

        modelBuilder.Entity<Family>()
            .Property(f => f.InboxEnabled)
            .HasColumnName("inbox_enabled");

        modelBuilder.Entity<User>()
            .Property(u => u.ClassroomEmail)
            .HasColumnName("classroom_email");

        modelBuilder.Entity<Message>()
            .Property(m => m.Role)
            .HasConversion<string>();

        // ── Track 2: Inbox (captura de Classroom vía Apps Script) ──
        modelBuilder.Entity<InboxMessageRaw>().ToTable("inbox_messages_raw", "inbox");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.Source).HasColumnName("source");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.GmailId).HasColumnName("gmail_id");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.MessageDate).HasColumnName("message_date");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.ToAddress).HasColumnName("to_address");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.FromAddress).HasColumnName("from_address");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.Subject).HasColumnName("subject");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.PlainBody).HasColumnName("plain_body");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.ReceivedAt).HasColumnName("received_at");
        modelBuilder.Entity<InboxMessageRaw>().Property(x => x.Processed).HasColumnName("processed");
        modelBuilder.Entity<InboxMessageRaw>().HasIndex(x => x.GmailId).IsUnique();

        modelBuilder.Entity<DetectedAssignment>().ToTable("detected_assignments", "inbox");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.StudentId).HasColumnName("student_id");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.ClassroomId).HasColumnName("classroom_id");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.Type).HasColumnName("type").HasConversion<string>();
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.Title).HasColumnName("title");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.CourseName).HasColumnName("course_name");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.Teacher).HasColumnName("teacher");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.Description).HasColumnName("description");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.DueDateRaw).HasColumnName("due_date_raw");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.DueDate).HasColumnName("due_date");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.CourseId).HasColumnName("course_id");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.ItemId).HasColumnName("item_id");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.MessageDate).HasColumnName("message_date");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.DetectedAt).HasColumnName("detected_at");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.Done).HasColumnName("done");
        modelBuilder.Entity<DetectedAssignment>().Property(x => x.DoneAt).HasColumnName("done_at");
        modelBuilder.Entity<DetectedAssignment>().HasIndex(x => x.StudentId);
    }
}
