using FliqPayroll.Data.Entities;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;



namespace FliqPayroll.Data;



public class FliqPayrollDbContext : IdentityDbContext<ApplicationUser>

{

    public FliqPayrollDbContext(DbContextOptions<FliqPayrollDbContext> options)

        : base(options)

    {

    }



    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<BiometricUpload> BiometricUploads => Set<BiometricUpload>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<LeaveRecord> LeaveRecords => Set<LeaveRecord>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)

    {

        base.OnModelCreating(modelBuilder);



        modelBuilder.Entity<Employee>(entity =>

        {

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EmployeeCode).IsUnique();

            entity.Property(e => e.EmployeeCode).HasMaxLength(20).IsRequired();

            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();

            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();

            entity.Property(e => e.MiddleName).HasMaxLength(100);

            entity.Property(e => e.Email).HasMaxLength(200);

            entity.Property(e => e.MobileNumber).HasMaxLength(30);

            entity.Property(e => e.Department).HasMaxLength(100);

            entity.Property(e => e.Position).HasMaxLength(100);

            entity.Property(e => e.Supervisor).HasMaxLength(100);

            entity.Property(e => e.HomeAddress).HasMaxLength(500);

            entity.Property(e => e.BankName).HasMaxLength(100);

            entity.Property(e => e.BankAccountNumber).HasMaxLength(50);

            entity.Property(e => e.TinNumber).HasMaxLength(30);

            entity.Property(e => e.SssNumber).HasMaxLength(30);

            entity.Property(e => e.PhilHealthNumber).HasMaxLength(30);

            entity.Property(e => e.PagIbigNumber).HasMaxLength(30);

            entity.Property(e => e.BasicSalary).HasPrecision(18, 2);

            entity.Property(e => e.SssErShare).HasPrecision(18, 2);

            entity.Property(e => e.SssEeShare).HasPrecision(18, 2);

            entity.Property(e => e.SssLoan).HasPrecision(18, 2);

            entity.Property(e => e.PhilHealthErShare).HasPrecision(18, 2);

            entity.Property(e => e.PhilHealthEeShare).HasPrecision(18, 2);

            entity.Property(e => e.PagIbigErShare).HasPrecision(18, 2);

            entity.Property(e => e.PagIbigEeShare).HasPrecision(18, 2);

            entity.Property(e => e.PagIbigLoan).HasPrecision(18, 2);

        });



        modelBuilder.Entity<AttendanceRecord>(entity =>

        {

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.EmployeeId, e.Date }).IsUnique();

            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Employee).WithMany(e => e.AttendanceRecords).HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);

        });



        modelBuilder.Entity<BiometricUpload>(entity =>

        {

            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();

        });



        modelBuilder.Entity<AuditLog>(entity =>

        {

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserName).HasMaxLength(100);

            entity.Property(e => e.Action).HasMaxLength(100);

            entity.Property(e => e.EntityName).HasMaxLength(100);

            entity.Property(e => e.EntityId).HasMaxLength(50);

            entity.Property(e => e.Details).HasMaxLength(2000);

        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.HolidayId);
            entity.HasIndex(e => e.Date).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<LeaveRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.FromDate, e.ToDate, e.LeaveType });
            entity.HasOne(e => e.Employee)
                .WithMany(e => e.LeaveRecords)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollPeriod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => new { e.StartDate, e.EndDate }).IsUnique();
        });

        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EmployeeCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.PaymentMethod).HasMaxLength(100);
            entity.HasIndex(e => new { e.EmployeeId, e.PayrollPeriodId }).IsUnique();
            entity.HasOne(e => e.Employee).WithMany(e => e.PayrollRecords).HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PayrollPeriod).WithMany(e => e.PayrollRecords).HasForeignKey(e => e.PayrollPeriodId).OnDelete(DeleteBehavior.Cascade);

            foreach (var property in entity.Metadata.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }
        });

    }

}


