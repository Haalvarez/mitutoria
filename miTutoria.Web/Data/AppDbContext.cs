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
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Promo> Promos => Set<Promo>();
    public DbSet<FocusSession> FocusSessions => Set<FocusSession>();

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

        modelBuilder.Entity<Family>()
            .Property(f => f.PayEnabled)
            .HasColumnName("pay_enabled");

        modelBuilder.Entity<Family>()
            .Property(f => f.PasswordHash)
            .HasColumnName("password_hash");

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

        // ── Cobro: pagos de la cuota mensual (MercadoPago Checkout Pro) ──
        modelBuilder.Entity<Payment>().ToTable("payments", "billing");
        modelBuilder.Entity<Payment>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<Payment>().Property(x => x.FamilyId).HasColumnName("family_id");
        modelBuilder.Entity<Payment>().Property(x => x.PreferenceId).HasColumnName("preference_id");
        modelBuilder.Entity<Payment>().Property(x => x.MpPaymentId).HasColumnName("mp_payment_id");
        modelBuilder.Entity<Payment>().Property(x => x.CycleMarker).HasColumnName("cycle_marker");
        modelBuilder.Entity<Payment>().Property(x => x.PromoCode).HasColumnName("promo_code");
        modelBuilder.Entity<Payment>().Property(x => x.AmountArs).HasColumnName("amount_ars");
        modelBuilder.Entity<Payment>().Property(x => x.Status).HasColumnName("status");
        modelBuilder.Entity<Payment>().Property(x => x.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<Payment>().Property(x => x.PaidAt).HasColumnName("paid_at");
        modelBuilder.Entity<Payment>().HasIndex(x => x.MpPaymentId);
        modelBuilder.Entity<Payment>().HasIndex(x => x.FamilyId);

        modelBuilder.Entity<Promo>().ToTable("promos", "billing");
        modelBuilder.Entity<Promo>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<Promo>().Property(x => x.Code).HasColumnName("code");
        modelBuilder.Entity<Promo>().Property(x => x.Name).HasColumnName("name");
        modelBuilder.Entity<Promo>().Property(x => x.AmountArs).HasColumnName("amount_ars");
        modelBuilder.Entity<Promo>().Property(x => x.ValidFrom).HasColumnName("valid_from");
        modelBuilder.Entity<Promo>().Property(x => x.ValidUntil).HasColumnName("valid_until");
        modelBuilder.Entity<Promo>().Property(x => x.Active).HasColumnName("active");
        modelBuilder.Entity<Promo>().Property(x => x.MaxUses).HasColumnName("max_uses");
        modelBuilder.Entity<Promo>().Property(x => x.UsedCount).HasColumnName("used_count");
        modelBuilder.Entity<Promo>().Property(x => x.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<Promo>().HasIndex(x => x.Code).IsUnique();

        // ── Atención dedicada: sesiones de foco del Aula ──
        modelBuilder.Entity<FocusSession>().ToTable("focus_sessions", "academic");
        modelBuilder.Entity<FocusSession>().Property(x => x.Id).HasColumnName("id");
        modelBuilder.Entity<FocusSession>().Property(x => x.StudentId).HasColumnName("student_id");
        modelBuilder.Entity<FocusSession>().Property(x => x.ClientKey).HasColumnName("client_key");
        modelBuilder.Entity<FocusSession>().Property(x => x.StartedAt).HasColumnName("started_at");
        modelBuilder.Entity<FocusSession>().Property(x => x.LastBeatAt).HasColumnName("last_beat_at");
        modelBuilder.Entity<FocusSession>().Property(x => x.FocusedMs).HasColumnName("focused_ms");
        modelBuilder.Entity<FocusSession>().Property(x => x.IdleMs).HasColumnName("idle_ms");
        modelBuilder.Entity<FocusSession>().Property(x => x.AwayMs).HasColumnName("away_ms");
        modelBuilder.Entity<FocusSession>().Property(x => x.Interruptions).HasColumnName("interruptions");
        modelBuilder.Entity<FocusSession>().HasIndex(x => new { x.StudentId, x.ClientKey }).IsUnique();
    }
}
