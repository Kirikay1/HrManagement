using Microsoft.EntityFrameworkCore;

namespace HrManagement.Model;

public sealed class HrManagementDbContext : DbContext
{
    public HrManagementDbContext()
    {
    }

    public HrManagementDbContext(DbContextOptions<HrManagementDbContext> options) : base(options)
    {
    }

    public DbSet<Calendar> Calendar => Set<Calendar>();
    public DbSet<Department> Department => Set<Department>();
    public DbSet<Employee> Employee => Set<Employee>();
    public DbSet<HrEvent> HrEvent => Set<HrEvent>();
    public DbSet<LearningCalendar> LearningCalendar => Set<LearningCalendar>();
    public DbSet<Material> Material => Set<Material>();
    public DbSet<Position> Position => Set<Position>();
    public DbSet<typeEvent> typeEvent => Set<typeEvent>();
    public DbSet<VacationCalendar> VacationCalendar => Set<VacationCalendar>();
    public DbSet<WorkingCalendar> WorkingCalendar => Set<WorkingCalendar>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=HrManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().ToTable("Employee");
        modelBuilder.Entity<Department>().ToTable("Department");
        modelBuilder.Entity<Position>().ToTable("Position");
        modelBuilder.Entity<Calendar>().ToTable("Calendar");
        modelBuilder.Entity<HrEvent>().ToTable("HrEvent");
        modelBuilder.Entity<LearningCalendar>().ToTable("LearningCalendar");
        modelBuilder.Entity<VacationCalendar>().ToTable("VacationCalendar");
        modelBuilder.Entity<WorkingCalendar>().ToTable("WorkingCalendar");
        modelBuilder.Entity<Material>().ToTable("Material");
        modelBuilder.Entity<typeEvent>().ToTable("typeEvent");

        modelBuilder.Entity<Department>()
            .HasOne(d => d.Department2)
            .WithMany(d => d.Department1)
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employee)
            .HasForeignKey(e => e.IdEmployeeDepartment)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Position)
            .WithMany(p => p.Employee)
            .HasForeignKey(e => e.IdPosition)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Calendar)
            .WithMany(c => c.Employee)
            .HasForeignKey(e => e.CalendarEmployee)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Employee2)
            .WithMany(e => e.Employee1)
            .HasForeignKey(e => e.DirectSupervisor)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Employee3)
            .WithMany(e => e.Employee11)
            .HasForeignKey(e => e.AssistantEmployee)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
