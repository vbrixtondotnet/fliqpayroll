using FliqPayroll.Core.Constants;
using Hangfire.Dashboard;

namespace FliqPayroll.Web;

/// <summary>
/// Restricts the Hangfire dashboard to signed-in SuperAdmin / HRAdmin users.
/// </summary>
public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;

        return user?.Identity?.IsAuthenticated == true
            && (user.IsInRole(RoleConstants.SuperAdmin) || user.IsInRole(RoleConstants.HrAdmin));
    }
}
