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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
builder.Services.AddScoped<PayrollApprovalEmailJobs>();

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

// Every endpoint requires a signed-in user unless it explicitly opts out with [AllowAnonymous]
// (only the login page, the error page and the Gmail OAuth callback do).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.Cookie.Name = "FliqPayroll.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

    // 30-day login that survives closing the browser; every visit pushes the expiry out another 30 days.
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;

    var identityValidate = options.Events.OnValidatePrincipal;
    options.Events.OnValidatePrincipal = async context =>
    {
        // Keep Identity's security-stamp check, then re-issue the cookie on every request
        // (the default only renews once half the lifetime has passed).
        if (identityValidate is not null)
        {
            await identityValidate(context);
        }

        if (context.Principal is not null)
        {
            context.ShouldRenew = true;
        }
    };

    // API calls get a 401/403 instead of an HTML redirect so scripts can react to it.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
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

// Users flagged PasswordChange = 1 can do nothing but change their password (or sign out) until they have.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var exempt = Path.HasExtension(path.Value)
        || path.StartsWithSegments("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/Account/Logout", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/Account/Login", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/Home/Error", StringComparison.OrdinalIgnoreCase);

    if (!exempt && context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var mustChange = userId is not null && await context.RequestServices
            .GetRequiredService<FliqPayrollDbContext>()
            .Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PasswordChange)
            .FirstOrDefaultAsync(context.RequestAborted);

        if (mustChange)
        {
            if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { Success = false, Message = "You must change your password before continuing." });
            }
            else
            {
                context.Response.Redirect("/Account/ChangePassword");
            }

            return;
        }
    }

    await next();
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()],
    DashboardTitle = "FliqPayroll Jobs"
});

// Static assets (css/js/logo) must load on the login page, so they are exempt from the fallback policy.
app.MapStaticAssets().AllowAnonymous();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

await DataSeeder.SeedAsync(app.Services);

app.Run();
