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
    }
}
