namespace FliqPayroll.Core.Constants;

public static class RoleConstants
{
    public const string SuperAdmin = "SuperAdmin";
    public const string HrAdmin = "HRAdmin";
    public const string PayrollOfficer = "PayrollOfficer";
    public const string FinanceOfficer = "FinanceOfficer";

    /// <summary>Roles allowed to approve or reject edits to generated payroll (comma separated for [Authorize]).</summary>
    public const string PayrollApprovers = FinanceOfficer;

    public static readonly string[] All =
    [
        SuperAdmin,
        HrAdmin,
        PayrollOfficer,
        FinanceOfficer
    ];
}
