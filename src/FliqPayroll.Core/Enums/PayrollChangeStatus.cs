namespace FliqPayroll.Core.Enums;

/// <summary>Lifecycle of a manual edit made to an already-generated (saved) payroll row.</summary>
public enum PayrollChangeStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    /// <summary>Replaced by a newer submission for the same employee before it was reviewed.</summary>
    Superseded = 3
}
