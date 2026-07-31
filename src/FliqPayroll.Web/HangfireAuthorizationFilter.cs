using FliqPayroll.Core.Constants;
using Hangfire.Dashboard;

namespace FliqPayroll.Web;

/// <summary>
/// Restricts the Hangfire dashboard to SuperAdmin / HRAdmin when authenticated;
/// allows access in Development when the request is unauthenticated (local debugging).
/// </summary>
public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.IsInRole(RoleConstants.SuperAdmin) || user.IsInRole(RoleConstants.HrAdmin);
        }

        var env = httpContext.RequestServices.GetService(typeof(IHostEnvironment)) as IHostEnvironment;
        return env?.IsDevelopment() == true;
    }
}
