using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FliqPayroll.Data.Repositories;

public class HolidayRepository : IHolidayRepository
{
    private readonly FliqPayrollDbContext _context;

    public HolidayRepository(FliqPayrollDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HolidayDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Holidays
            .AsNoTracking()
            .OrderBy(h => h.Date)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<HolidayDto>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var phStart = PhilippineTime.ToPhilippineDate(startDate);
        var phEnd = PhilippineTime.ToPhilippineDate(endDate);
        var (startUtc, endUtcExclusive) = PhilippineTime.ToUtcDateRange(phStart, phEnd);

        var entities = await _context.Holidays
            .AsNoTracking()
            .Where(h =>
                (h.Date >= startUtc && h.Date < endUtcExclusive) ||
                (h.Date >= phStart && h.Date <= phEnd))
            .OrderBy(h => h.Date)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<HolidayDto?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var calendarDate = PhilippineTime.ForDateStorage(date);
        var legacyUtcDate = PhilippineTime.ToUtcDate(calendarDate);

        var entity = await _context.Holidays
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Date == calendarDate || h.Date == legacyUtcDate, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<HolidayDto?> GetByIdAsync(int holidayId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Holidays
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HolidayId == holidayId, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<HolidayDto> CreateAsync(CreateHolidayDto dto, CancellationToken cancellationToken = default)
    {
        var calendarDate = PhilippineTime.ForDateStorage(dto.Date);

        var entity = new Holiday
        {
            Date = calendarDate,
            Description = dto.Description.Trim(),
            HolidayType = dto.HolidayType,
            CreatedAt = DateTime.UtcNow
        };

        _context.Holidays.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<HolidayDto?> UpdateAsync(int holidayId, UpdateHolidayDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Holidays
            .FirstOrDefaultAsync(h => h.HolidayId == holidayId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Description = dto.Description.Trim();
        entity.HolidayType = dto.HolidayType;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(int holidayId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Holidays
            .FirstOrDefaultAsync(h => h.HolidayId == holidayId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _context.Holidays.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ExistsOnDateAsync(DateTime date, int? excludeHolidayId = null, CancellationToken cancellationToken = default)
    {
        var calendarDate = PhilippineTime.ForDateStorage(date);
        var legacyUtcDate = PhilippineTime.ToUtcDate(calendarDate);

        var query = _context.Holidays
            .AsNoTracking()
            .Where(h => h.Date == calendarDate || h.Date == legacyUtcDate);

        if (excludeHolidayId.HasValue)
        {
            query = query.Where(h => h.HolidayId != excludeHolidayId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private static HolidayDto MapToDto(Holiday entity) => new()
    {
        HolidayId = entity.HolidayId,
        Date = PhilippineTime.ToPhilippineDate(entity.Date),
        Description = entity.Description,
        HolidayType = entity.HolidayType,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
