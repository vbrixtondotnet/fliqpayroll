using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;

namespace FliqPayroll.Services;

public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _holidayRepository;

    public HolidayService(IHolidayRepository holidayRepository)
    {
        _holidayRepository = Guard.AgainstNull(holidayRepository, nameof(holidayRepository));
    }

    public Task<IReadOnlyList<HolidayDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _holidayRepository.GetAllAsync(cancellationToken);

    public Task<HolidayDto?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default) =>
        _holidayRepository.GetByDateAsync(PhilippineTime.ToPhilippineDate(date), cancellationToken);

    public async Task<HolidayDto> AddAsync(CreateHolidayDto dto, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(dto, nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            throw new ArgumentException("Description is required.");
        }

        var normalizedDate = PhilippineTime.ToPhilippineDate(dto.Date);

        if (await _holidayRepository.ExistsOnDateAsync(normalizedDate, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("A holiday already exists for this date.");
        }

        return await _holidayRepository.CreateAsync(
            new CreateHolidayDto
            {
                Date = normalizedDate,
                Description = dto.Description,
                HolidayType = dto.HolidayType
            },
            cancellationToken);
    }

    public async Task<HolidayDto?> UpdateAsync(int holidayId, UpdateHolidayDto dto, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(dto, nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            throw new ArgumentException("Description is required.");
        }

        return await _holidayRepository.UpdateAsync(holidayId, dto, cancellationToken);
    }

    public Task<bool> DeleteAsync(int holidayId, CancellationToken cancellationToken = default) =>
        _holidayRepository.DeleteAsync(holidayId, cancellationToken);
}
