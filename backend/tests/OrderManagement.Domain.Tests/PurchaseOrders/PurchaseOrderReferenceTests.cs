using FluentValidation.TestHelper;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Models;
using OrderManagement.Domain.PurchaseOrders.Validators;
using OrderManagement.Domain.Tests.Fakes;

namespace OrderManagement.Domain.Tests.PurchaseOrders;

public class PurchaseOrderReferenceTests
{
    [Theory]
    [InlineData("PO-1001", "PO-1001")]
    [InlineData("  po-1001  ", "PO-1001")]
    [InlineData("\tpo-2026/a.b#7_x\n", "PO-2026/A.B#7_X")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Normalise_trims_then_upper_cases(string? input, string expected)
    {
        Assert.Equal(expected, PurchaseOrderReference.Normalise(input));
    }

    [Theory]
    [InlineData("  po-1001  ")]
    [InlineData("po-2026/a.b#7_x")]
    public async Task Manager_stores_exactly_the_value_the_validator_checked(string reference)
    {
        var repository = new InMemoryPurchaseOrderRepository();
        var request = TestData.ValidRequest(reference);

        new CreatePurchaseOrderRequestValidator().TestValidate(request).ShouldNotHaveAnyValidationErrors();
        var result = await TestData.CreateManager(repository).SubmitAsync(request);

        Assert.Equal(PurchaseOrderReference.Normalise(reference), result.Order.ExternalReference);
    }

    [Fact]
    public async Task Length_limit_applies_to_the_normalised_reference()
    {
        // 50 characters plus surrounding whitespace: valid because only the trimmed value is stored.
        var reference = "  " + new string('a', CreatePurchaseOrderRequestValidator.ExternalReferenceMaxLength) + "  ";
        var repository = new InMemoryPurchaseOrderRepository();

        var result = await TestData.CreateManager(repository).SubmitAsync(TestData.ValidRequest(reference));

        Assert.Equal(new string('A', CreatePurchaseOrderRequestValidator.ExternalReferenceMaxLength), result.Order.ExternalReference);
    }

    [Fact]
    public void Validation_error_is_reported_against_the_external_reference_field()
    {
        var result = new CreatePurchaseOrderRequestValidator()
            .TestValidate(TestData.ValidRequest() with { ExternalReference = new string('A', 51) });

        result.ShouldHaveValidationErrorFor(nameof(CreatePurchaseOrderRequest.ExternalReference));
    }
}
