using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Data.Entities;

namespace FliqPayroll.Data.Repositories;

/// <summary>Compares a stored payroll row with a proposed one and applies approved values.</summary>
internal static partial class PayrollChangeDetector
{
    private static readonly HashSet<string> IgnoredFields = ["Id", "PayrollPeriodId", "EmployeeId"];

    /// <summary>Decimal columns present on both the entity and the DTO, plus PaymentMethod.</summary>
    private static readonly (PropertyInfo Entity, PropertyInfo Dto)[] DecimalFields = typeof(PayrollRecord)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(decimal) && !IgnoredFields.Contains(p.Name))
        .Select(p => (Entity: p, Dto: typeof(PayrollDto).GetProperty(p.Name)))
        .Where(x => x.Dto is { CanRead: true } && x.Dto.PropertyType == typeof(decimal))
        .Select(x => (x.Entity, x.Dto!))
        .ToArray();

    // Storage has scale 2, so compare at that precision to avoid phantom differences.
    private static decimal Normalize(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static List<PayrollFieldChangeDto> Compare(PayrollRecord live, PayrollDto proposed)
    {
        var changes = new List<PayrollFieldChangeDto>();

        foreach (var (entityProp, dtoProp) in DecimalFields)
        {
            var oldValue = Normalize((decimal)entityProp.GetValue(live)!);
            var newValue = Normalize((decimal)dtoProp.GetValue(proposed)!);

            if (oldValue != newValue)
            {
                changes.Add(new PayrollFieldChangeDto
                {
                    Field = entityProp.Name,
                    Label = Humanize(entityProp.Name),
                    OldValue = oldValue.ToString("0.00", CultureInfo.InvariantCulture),
                    NewValue = newValue.ToString("0.00", CultureInfo.InvariantCulture)
                });
            }
        }

        var oldMethod = live.PaymentMethod?.Trim() ?? string.Empty;
        var newMethod = proposed.PaymentMethod?.Trim() ?? string.Empty;
        if (!string.Equals(oldMethod, newMethod, StringComparison.Ordinal))
        {
            changes.Add(new PayrollFieldChangeDto
            {
                Field = nameof(PayrollRecord.PaymentMethod),
                Label = "Payment Method",
                OldValue = oldMethod,
                NewValue = newMethod
            });
        }

        return changes;
    }

    /// <summary>Copies the proposed values onto the live record (used when a request is approved).</summary>
    public static void Apply(PayrollRecord live, PayrollDto proposed)
    {
        foreach (var (entityProp, dtoProp) in DecimalFields)
        {
            entityProp.SetValue(live, dtoProp.GetValue(proposed));
        }

        live.PaymentMethod = proposed.PaymentMethod;
        live.UpdatedAt = DateTime.UtcNow;
    }

    private static string Humanize(string name) => WordBoundary().Replace(name, " $1").Trim();

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex WordBoundary();
}
