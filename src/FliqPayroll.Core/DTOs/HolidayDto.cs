using FliqPayroll.Core.Enums;

namespace FliqPayroll.Core.DTOs;

public class HolidayDto
{
    public int HolidayId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public HolidayType HolidayType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateHolidayDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public HolidayType HolidayType { get; set; }
}

public class UpdateHolidayDto
{
    public string Description { get; set; } = string.Empty;
    public HolidayType HolidayType { get; set; }
}
