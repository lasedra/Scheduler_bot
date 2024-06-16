using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Scheduler_bot.Models;

public partial class SchedulerDbContext : DbContext
{
    private static SchedulerDbContext DbContext { get; set; } = null!;
    public static Employee CurrentUser { get; set; } = null!;
    public IConfiguration appConfig { get; set; } = null!;
    public bool IsAppUpdated { get; set; } = false;
    public enum ChangeLogLevel
    {
        Intermediate,
        Secondary,
        Primary
    }

    public override int SaveChanges()
    {
        return SaveChanges(ChangeLogLevel.Intermediate, string.Empty);
    }
    public int SaveChanges(ChangeLogLevel level, string message)
    {
        EventLog newLog = new()
        {
            DateTime = DateTime.Now,
            Level = level.ToString(),
            Message = !string.IsNullOrWhiteSpace(message) ? message : "/*empty*/",
            IsUpdatedByApp = IsAppUpdated
        };
        this.EventLogs.Add(newLog);

        return base.SaveChanges();
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(appConfig.GetConnectionString("Host"));

    public virtual DbSet<Cabinet> Cabinets { get; set; }

    public virtual DbSet<ClassesTimingBody> ClassesTimingBodies { get; set; }

    public virtual DbSet<ClassesTimingHeader> ClassesTimingHeaders { get; set; }

    public virtual DbSet<DailyScheduleBody> DailyScheduleBodies { get; set; }

    public virtual DbSet<DailyScheduleHeader> DailyScheduleHeaders { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EventLog> EventLogs { get; set; }

    public virtual DbSet<StudentGroup> StudentGroups { get; set; }

    public virtual DbSet<Studying> Studyings { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Tution> Tutions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cabinet>(entity =>
        {
            entity.HasKey(e => e.Number).HasName("Cabinet_pkey");

            entity.ToTable("Cabinet");

            entity.Property(e => e.Number).HasColumnType("character varying");
            entity.Property(e => e.Name).HasColumnType("character varying");
        });

        modelBuilder.Entity<ClassesTimingBody>(entity =>
        {
            entity.HasKey(e => new { e.ClassesTimingHeaderId, e.ClassNumber }).HasName("ClassesTiming_body_pkey");

            entity.ToTable("ClassesTiming_body");

            entity.Property(e => e.ClassesTimingHeaderId).HasColumnName("ClassesTiming_header_ID");

            entity.HasOne(d => d.ClassesTimingHeader).WithMany(p => p.ClassesTimingBodies)
                .HasForeignKey(d => d.ClassesTimingHeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ClassesTiming_body_ClassesTiming_header_ID_fkey");
        });

        modelBuilder.Entity<ClassesTimingHeader>(entity =>
        {
            entity.HasKey(e => e.ClassesTimingHeaderId).HasName("ClassesTiming_header_pkey");

            entity.ToTable("ClassesTiming_header");

            entity.HasIndex(e => e.Name, "ClassesTiming_header_Name_key").IsUnique();

            entity.Property(e => e.ClassesTimingHeaderId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("ClassesTiming_header_ID");
            entity.Property(e => e.Name).HasColumnType("character varying");
        });

        modelBuilder.Entity<DailyScheduleBody>(entity =>
        {
            entity.HasKey(e => new { e.StudentGroupCode, e.OfDate, e.ClassNumber }).HasName("DailySchedule_body_pkey");

            entity.ToTable("DailySchedule_body");

            entity.Property(e => e.StudentGroupCode).HasColumnType("character varying");
            entity.Property(e => e.CabinetNumber).HasColumnType("character varying");
            entity.Property(e => e.ClassesTimingHeaderId).HasColumnName("ClassesTiming_header_ID");
            entity.Property(e => e.EmployeeId).HasColumnName("Employee_ID");
            entity.Property(e => e.SubjectId).HasColumnName("Subject_ID");

            entity.HasOne(d => d.CabinetNumberNavigation).WithMany(p => p.DailyScheduleBodies)
                .HasForeignKey(d => d.CabinetNumber)
                .HasConstraintName("DailySchedule_body_CabinetNumber_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.DailyScheduleBodies)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("DailySchedule_body_Employee_ID_fkey");

            entity.HasOne(d => d.Subject).WithMany(p => p.DailyScheduleBodies)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("DailySchedule_body_Subject_ID_fkey");

            entity.HasOne(d => d.DailyScheduleHeader).WithMany(p => p.DailyScheduleBodies)
                .HasForeignKey(d => new { d.StudentGroupCode, d.OfDate })
                .HasConstraintName("DailySchedule_body_StudentGroupCode_OfDate_fkey");
        });

        modelBuilder.Entity<DailyScheduleHeader>(entity =>
        {
            entity.HasKey(e => new { e.StudentGroupCode, e.OfDate }).HasName("DailySchedule_header_pkey");

            entity.ToTable("DailySchedule_header");

            entity.Property(e => e.StudentGroupCode).HasColumnType("character varying");

            entity.HasOne(d => d.StudentGroupCodeNavigation).WithMany(p => p.DailyScheduleHeaders)
                .HasForeignKey(d => d.StudentGroupCode)
                .HasConstraintName("DailySchedule_header_StudentGroupCode_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("Employee_pkey");

            entity.ToTable("Employee");

            entity.HasIndex(e => e.Login, "Employee_Login_key").IsUnique();

            entity.HasIndex(e => e.Phone, "Employee_Phone_key").IsUnique();

            entity.HasIndex(e => e.TgBotChatId, "Employee_TgBotChatId_key").IsUnique();

            entity.Property(e => e.EmployeeId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("Employee_ID");
            entity.Property(e => e.EMail)
                .HasColumnType("character varying")
                .HasColumnName("E-mail");
            entity.Property(e => e.Login).HasColumnType("character varying");
            entity.Property(e => e.Name).HasColumnType("character varying");
            entity.Property(e => e.Password).HasColumnType("character varying");
        });

        modelBuilder.Entity<EventLog>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("EVENT_LOG_pkey");

            entity.ToTable("EVENT_LOG");

            entity.Property(e => e.EventId).HasColumnName("Event_ID");
            entity.Property(e => e.DateTime).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Level).HasColumnType("character varying");
            entity.Property(e => e.Message).HasColumnType("character varying");
        });

        modelBuilder.Entity<StudentGroup>(entity =>
        {
            entity.HasKey(e => e.StudentGroupCode).HasName("StudentGroup_pkey");

            entity.ToTable("StudentGroup");

            entity.Property(e => e.StudentGroupCode).HasColumnType("character varying");
            entity.Property(e => e.Specialization).HasColumnType("character varying");
        });

        modelBuilder.Entity<Studying>(entity =>
        {
            entity.HasKey(e => new { e.SubjectId, e.StudentGroupCode }).HasName("Studying_pkey");

            entity.ToTable("Studying");

            entity.Property(e => e.SubjectId).HasColumnName("Subject_ID");
            entity.Property(e => e.StudentGroupCode).HasColumnType("character varying");

            entity.HasOne(d => d.StudentGroupCodeNavigation).WithMany(p => p.Studyings)
                .HasForeignKey(d => d.StudentGroupCode)
                .HasConstraintName("Studying_StudentGroupCode_fkey");

            entity.HasOne(d => d.Subject).WithMany(p => p.Studyings)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("Studying_Subject_ID_fkey");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("Subject_pkey");

            entity.ToTable("Subject");

            entity.HasIndex(e => e.Name, "Subject_Name_key").IsUnique();

            entity.Property(e => e.SubjectId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("Subject_ID");
            entity.Property(e => e.Name).HasColumnType("character varying");
        });

        modelBuilder.Entity<Tution>(entity =>
        {
            entity.HasKey(e => new { e.SubjectId, e.EmployeeId }).HasName("Tution_pkey");

            entity.ToTable("Tution");

            entity.Property(e => e.SubjectId).HasColumnName("Subject_ID");
            entity.Property(e => e.EmployeeId).HasColumnName("Employee_ID");

            entity.HasOne(d => d.Employee).WithMany(p => p.Tutions)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("Tution_Employee_ID_fkey");

            entity.HasOne(d => d.Subject).WithMany(p => p.Tutions)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("Tution_Subject_ID_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
