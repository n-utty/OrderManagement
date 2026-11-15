using FluentValidation;
using OrderManagement.Domain.PurchaseOrders.Models;

namespace OrderManagement.Domain.PurchaseOrders.Validators;

public sealed class CreatePurchaseOrderRequestValidator : AbstractValidator<CreatePurchaseOrderRequest>
{
    // Public so the persistence layer can size its columns from the same numbers.
    public const int ExternalReferenceMaxLength = 50;
    public const int CustomerIdMaxLength = 50;
    public const int CustomerNameMaxLength = 200;
    public const int CustomerEmailMaxLength = 254; // RFC 5321 limit for a deliverable address.
    public const int NotesMaxLength = 1000;
    public const int MaxLineItems = 100;

    public static readonly IReadOnlyList<string> SupportedCurrencies = ["ZAR", "USD", "EUR", "GBP"];

    public CreatePurchaseOrderRequestValidator()
    {
        // Stop at the first failing rule per property: one clear message beats a pile of related ones.
        RuleLevelCascadeMode = CascadeMode.Stop;

        // Validates the normalised value, i.e. exactly what the manager stores,
        // so stray whitespace from a form isn't treated as an invalid character.
        RuleFor(x => PurchaseOrderReference.Normalise(x.ExternalReference))
            .NotEmpty()
            .MaximumLength(ExternalReferenceMaxLength)
            .Matches("^[A-Za-z0-9_/.#-]+$")
            .WithMessage("External reference may only contain letters, digits and the characters - _ / . #")
            .OverridePropertyName(nameof(CreatePurchaseOrderRequest.ExternalReference));

        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(CustomerIdMaxLength);

        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(CustomerNameMaxLength);

        RuleFor(x => x.CustomerEmail).NotEmpty().MaximumLength(CustomerEmailMaxLength).EmailAddress();

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(currency => SupportedCurrencies.Contains(currency.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}.");

        RuleFor(x => x.Notes).MaximumLength(NotesMaxLength);

        RuleFor(x => x.LineItems)
            .NotNull()
            .Must(items => items.Count is >= 1 and <= MaxLineItems)
            .WithMessage($"An order must have between 1 and {MaxLineItems} line items.")
            .Must(items => !FindDuplicateSkus(items).Any())
            .WithMessage(request =>
                $"Each SKU may appear only once; combine the quantities instead. Duplicate SKUs: {string.Join(", ", FindDuplicateSkus(request.LineItems))}.");

        RuleForEach(x => x.LineItems)
            .NotNull()
            .SetValidator(new LineItemRequestValidator());
    }

    private static IEnumerable<string> FindDuplicateSkus(IEnumerable<LineItemRequest?> items) =>
        items
            .Where(item => !string.IsNullOrWhiteSpace(item?.Sku))
            .GroupBy(item => item!.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
}
