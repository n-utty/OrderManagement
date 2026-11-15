namespace OrderManagement.Domain.Common.Exceptions;

// Details are structured (e.g. the allowed transitions) so the API can return
// them alongside the message and the UI can act on them without parsing text.
public sealed class BusinessRuleException(string message, IReadOnlyDictionary<string, object?>? details = null)
    : Exception(message)
{
    public IReadOnlyDictionary<string, object?> Details { get; } = details ?? new Dictionary<string, object?>();
}
