using HR_System.Models;
using HR_System.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Supabase.Gotrue;

namespace HR_System.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    // Check if the property is an Enum (or a Nullable Enum)
                    var type = property.ClrType;
                    if (type.IsEnum || (Nullable.GetUnderlyingType(type)?.IsEnum ?? false))
                    {
                        modelBuilder.Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion<string>();
                    }
                }
            }
        }

        public DbSet<Candidate> Candidates { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Vacancy> Vacancies { get; set; }
        public DbSet<EmploymentContract> EmploymentContracts { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Leave> Leaves { get; set; }

        public DbSet<Payslip> Payslips { get; set; }
    }
}
