namespace OrderManagement.Domain.Common.Exceptions;

// Carries the existing order's id so the client can link the user to the order
// that already holds the reference instead of leaving them guessing.
public sealed class ConflictException(string message, Guid existingOrderId) : Exception(message)
{
    public Guid ExistingOrderId { get; } = existingOrderId;
}
