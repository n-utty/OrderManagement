using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OrderManagement.Infrastructure.Persistence.Converters;

public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => ToUtc(value),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
{
    // Unspecified is assumed to already be UTC rather than shifted by the server's local offset.
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
