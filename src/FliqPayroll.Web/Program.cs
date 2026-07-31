using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Data;
using FliqPayroll.Data.Entities;
using FliqPayroll.Services;
using FliqPayroll.Web;
using FliqPayroll.Web.Jobs;
using FliqPayroll.Web.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddFliqPayrollData(connectionString);
builder.Services.AddFliqPayrollServices(builder.Configuration);
builder.Services.AddScoped<PayslipPdfService>();
builder.Services.AddScoped<IPayslipDocumentGenerator>(sp => sp.GetRequiredService<PayslipPdfService>());
builder.Services.AddScoped<PayslipEmailJobs>();

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
        PrepareSchemaIfNecessary = true
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
    options.Queues = ["default"];
});

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("FliqPayroll");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<FliqPayrollDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();

PhilippineTime.EnsureConfigured();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()],
    DashboardTitle = "FliqPayroll Jobs"
});

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

await DataSeeder.SeedAsync(app.Services);

app.Run();
