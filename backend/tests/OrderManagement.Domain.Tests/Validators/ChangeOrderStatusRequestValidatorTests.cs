using FluentValidation.TestHelper;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Models;
using OrderManagement.Domain.PurchaseOrders.Validators;

namespace OrderManagement.Domain.Tests.Validators;

public class ChangeOrderStatusRequestValidatorTests
{
    private readonly ChangeOrderStatusRequestValidator _validator = new();

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Fulfilled)]
    [InlineData(OrderStatus.Cancelled)]
    public void Known_status_passes(OrderStatus status)
    {
        _validator.TestValidate(new ChangeOrderStatusRequest { Status = status }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Missing_status_is_rejected()
    {
        _validator.TestValidate(new ChangeOrderStatusRequest()).ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Unknown_status_is_rejected_with_the_valid_names()
    {
        _validator.TestValidate(new ChangeOrderStatusRequest { Status = (OrderStatus)99 })
            .ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage("Status must be one of: Pending, Confirmed, Fulfilled, Cancelled.");
    }
}
