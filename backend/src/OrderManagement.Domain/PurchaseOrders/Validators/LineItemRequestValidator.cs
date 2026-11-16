using FluentValidation;
using OrderManagement.Domain.PurchaseOrders.Models;

namespace OrderManagement.Domain.PurchaseOrders.Validators;

public sealed class LineItemRequestValidator : AbstractValidator<LineItemRequest>
{
    // Public so the persistence layer can size its columns from the same numbers.
    public const int SkuMaxLength = 64;
    public const int NameMaxLength = 200;
    // With quantity capped by int, this keeps line totals well inside decimal and the (18,2) column.
    public const decimal UnitPriceMax = 1_000_000.00m;

    public LineItemRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Sku).NotEmpty().MaximumLength(SkuMaxLength);

        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMaxLength);

        // Quantity is an int, so whole numbers are already guaranteed by the type.
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Quantity must be a whole number of at least 1.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit price can't be negative.")
            .LessThanOrEqualTo(UnitPriceMax)
            .WithMessage("Unit price can be at most 1,000,000.00.")
            // Sub-cent prices would make line totals disagree with what the customer is invoiced.
            .Must(price => decimal.Round(price, 2) == price)
            .WithMessage("Unit price can have at most 2 decimal places.");
    }
}
