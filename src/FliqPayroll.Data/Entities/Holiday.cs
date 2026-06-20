using FliqPayroll.Core.Enums;

namespace FliqPayroll.Data.Entities;

public class Holiday
{
    public int HolidayId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public HolidayType HolidayType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
