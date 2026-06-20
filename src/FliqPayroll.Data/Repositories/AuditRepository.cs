using FliqPayroll.Core.Interfaces;

using FliqPayroll.Core.Utilities;

using FliqPayroll.Data.Entities;

using Microsoft.EntityFrameworkCore;



namespace FliqPayroll.Data.Repositories;



public class AuditRepository : IAuditRepository

{

    private readonly FliqPayrollDbContext _context;



    public AuditRepository(FliqPayrollDbContext context)

    {

        _context = Guard.AgainstNull(context, nameof(context));

    }



    public async Task LogAsync(

        string userName,

        string action,

        string entityName,

        string? entityId,

        string? details,

        CancellationToken cancellationToken = default)

    {

        var entry = new AuditLog

        {

            UserName = Guard.AgainstNullOrWhiteSpace(userName, nameof(userName)),

            Action = Guard.AgainstNullOrWhiteSpace(action, nameof(action)),

            EntityName = Guard.AgainstNullOrWhiteSpace(entityName, nameof(entityName)),

            EntityId = entityId?.Trim(),

            Details = details?.Trim(),

            CreatedAt = DateTime.UtcNow

        };



        _context.AuditLogs.Add(entry);

        await _context.SaveChangesAsync(cancellationToken);

    }



    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default)

    {

        var safeTake = take <= 0 ? 50 : take;



        return await _context.AuditLogs

            .AsNoTracking()

            .OrderByDescending(a => a.CreatedAt)

            .Take(safeTake)

            .Select(a => new AuditLogDto

            {

                Id = a.Id,

                UserName = a.UserName,

                Action = a.Action,

                EntityName = a.EntityName,

                EntityId = a.EntityId,

                Details = a.Details,

                CreatedAt = a.CreatedAt

            })

            .ToListAsync(cancellationToken);

    }

}


