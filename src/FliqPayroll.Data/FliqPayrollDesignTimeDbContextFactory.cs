using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FliqPayroll.Data;

public class FliqPayrollDesignTimeDbContextFactory : IDesignTimeDbContextFactory<FliqPayrollDbContext>
{
    public FliqPayrollDbContext CreateDbContext(string[] args)
    {
        var webProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "FliqPayroll.Web");
        if (!Directory.Exists(webProjectPath))
        {
            webProjectPath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webProjectPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found in appsettings.json.");

        var optionsBuilder = new DbContextOptionsBuilder<FliqPayrollDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new FliqPayrollDbContext(optionsBuilder.Options);
    }
}
