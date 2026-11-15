using FluentValidation;
using OrderManagement.Domain.PurchaseOrders.Models;

namespace OrderManagement.Domain.PurchaseOrders.Validators;

public sealed class ChangeOrderStatusRequestValidator : AbstractValidator<ChangeOrderStatusRequest>
{
    public ChangeOrderStatusRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Status)
            .NotNull()
            .IsInEnum()
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<OrderStatus>())}.");
    }
}
