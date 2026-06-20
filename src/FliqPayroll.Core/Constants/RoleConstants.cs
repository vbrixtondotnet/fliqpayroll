namespace FliqPayroll.Core.Constants;

public static class RoleConstants
{
    public const string SuperAdmin = "SuperAdmin";
    public const string HrAdmin = "HRAdmin";
    public const string PayrollOfficer = "PayrollOfficer";
    public const string FinanceOfficer = "FinanceOfficer";

    public static readonly string[] All =
    [
        SuperAdmin,
        HrAdmin,
        PayrollOfficer,
        FinanceOfficer
    ];
}
