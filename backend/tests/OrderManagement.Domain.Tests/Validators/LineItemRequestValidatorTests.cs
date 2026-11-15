using FluentValidation.TestHelper;
using OrderManagement.Domain.PurchaseOrders.Models;
using OrderManagement.Domain.PurchaseOrders.Validators;

namespace OrderManagement.Domain.Tests.Validators;

public class LineItemRequestValidatorTests
{
    private readonly LineItemRequestValidator _validator = new();

    private static readonly LineItemRequest Valid = new() { Sku = "SKU-A", Name = "Widget", Quantity = 1, UnitPrice = 10m };

    [Fact]
    public void Valid_line_item_passes()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Sku_is_required(string sku)
    {
        _validator.TestValidate(Valid with { Sku = sku }).ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Name_is_required(string name)
    {
        _validator.TestValidate(Valid with { Name = name }).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Max_length_constants_match_the_agreed_limits()
    {
        Assert.Equal(64, LineItemRequestValidator.SkuMaxLength);
        Assert.Equal(200, LineItemRequestValidator.NameMaxLength);
    }

    [Fact]
    public void Sku_allows_64_characters_but_not_65()
    {
        _validator.TestValidate(Valid with { Sku = new string('S', 64) }).ShouldNotHaveValidationErrorFor(x => x.Sku);
        _validator.TestValidate(Valid with { Sku = new string('S', 65) }).ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Fact]
    public void Name_allows_200_characters_but_not_201()
    {
        _validator.TestValidate(Valid with { Name = new string('n', 200) }).ShouldNotHaveValidationErrorFor(x => x.Name);
        _validator.TestValidate(Valid with { Name = new string('n', 201) }).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Quantity_below_one_is_rejected(int quantity)
    {
        _validator.TestValidate(Valid with { Quantity = quantity })
            .ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity must be a whole number of at least 1.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void Positive_quantity_passes(int quantity)
    {
        _validator.TestValidate(Valid with { Quantity = quantity }).ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Negative_unit_price_is_rejected()
    {
        _validator.TestValidate(Valid with { UnitPrice = -0.01m })
            .ShouldHaveValidationErrorFor(x => x.UnitPrice)
            .WithErrorMessage("Unit price can't be negative.");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.01")]
    [InlineData("10.1")]
    [InlineData("10.10")]
    [InlineData("10.120")]
    public void Unit_price_of_zero_or_up_to_two_decimals_passes(string price)
    {
        _validator.TestValidate(Valid with { UnitPrice = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture) })
            .ShouldNotHaveValidationErrorFor(x => x.UnitPrice);
    }

    [Theory]
    [InlineData("10.123")]
    [InlineData("0.001")]
    public void Unit_price_with_more_than_two_decimals_is_rejected(string price)
    {
        _validator.TestValidate(Valid with { UnitPrice = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture) })
            .ShouldHaveValidationErrorFor(x => x.UnitPrice)
            .WithErrorMessage("Unit price can have at most 2 decimal places.");
    }
}
