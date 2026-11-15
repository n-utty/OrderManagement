using FluentValidation.TestHelper;
using OrderManagement.Domain.PurchaseOrders.Models;
using OrderManagement.Domain.PurchaseOrders.Validators;

namespace OrderManagement.Domain.Tests.Validators;

public class CreatePurchaseOrderRequestValidatorTests
{
    private readonly CreatePurchaseOrderRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        _validator.TestValidate(TestData.ValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("PO 1001")]
    [InlineData("PO@1001")]
    [InlineData("PO-1001!")]
    public void External_reference_rejects_empty_or_unsupported_characters(string reference)
    {
        var result = _validator.TestValidate(TestData.ValidRequest() with { ExternalReference = reference });

        result.ShouldHaveValidationErrorFor(nameof(CreatePurchaseOrderRequest.ExternalReference));
    }

    [Theory]
    [InlineData("PO-2026/001.A#7_x")]
    [InlineData("  PO-1001  ")]
    public void External_reference_accepts_allowed_characters_and_surrounding_whitespace(string reference)
    {
        var result = _validator.TestValidate(TestData.ValidRequest() with { ExternalReference = reference });

        result.ShouldNotHaveValidationErrorFor(nameof(CreatePurchaseOrderRequest.ExternalReference));
    }

    [Fact]
    public void External_reference_allows_50_characters_but_not_51()
    {
        _validator.TestValidate(TestData.ValidRequest() with { ExternalReference = new string('A', 50) })
            .ShouldNotHaveValidationErrorFor(nameof(CreatePurchaseOrderRequest.ExternalReference));

        _validator.TestValidate(TestData.ValidRequest() with { ExternalReference = new string('A', 51) })
            .ShouldHaveValidationErrorFor(nameof(CreatePurchaseOrderRequest.ExternalReference));
    }

    [Fact]
    public void External_reference_error_names_the_field()
    {
        var result = _validator.TestValidate(TestData.ValidRequest() with { ExternalReference = "" });

        result.ShouldHaveValidationErrorFor(nameof(CreatePurchaseOrderRequest.ExternalReference))
            .WithErrorMessage("'External Reference' must not be empty.");
    }

    [Fact]
    public void Customer_id_is_required()
    {
        _validator.TestValidate(TestData.ValidRequest() with { CustomerId = " " })
            .ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Customer_name_is_required()
    {
        _validator.TestValidate(TestData.ValidRequest() with { CustomerName = "" })
            .ShouldHaveValidationErrorFor(x => x.CustomerName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Customer_email_must_be_present_and_valid(string email)
    {
        _validator.TestValidate(TestData.ValidRequest() with { CustomerEmail = email })
            .ShouldHaveValidationErrorFor(x => x.CustomerEmail);
    }

    [Fact]
    public void Max_length_constants_match_the_agreed_limits()
    {
        Assert.Equal(50, CreatePurchaseOrderRequestValidator.ExternalReferenceMaxLength);
        Assert.Equal(50, CreatePurchaseOrderRequestValidator.CustomerIdMaxLength);
        Assert.Equal(200, CreatePurchaseOrderRequestValidator.CustomerNameMaxLength);
        Assert.Equal(254, CreatePurchaseOrderRequestValidator.CustomerEmailMaxLength);
        Assert.Equal(1000, CreatePurchaseOrderRequestValidator.NotesMaxLength);
    }

    [Fact]
    public void Customer_id_allows_50_characters_but_not_51()
    {
        _validator.TestValidate(TestData.ValidRequest() with { CustomerId = new string('C', 50) })
            .ShouldNotHaveValidationErrorFor(x => x.CustomerId);

        _validator.TestValidate(TestData.ValidRequest() with { CustomerId = new string('C', 51) })
            .ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Customer_name_allows_200_characters_but_not_201()
    {
        _validator.TestValidate(TestData.ValidRequest() with { CustomerName = new string('n', 200) })
            .ShouldNotHaveValidationErrorFor(x => x.CustomerName);

        _validator.TestValidate(TestData.ValidRequest() with { CustomerName = new string('n', 201) })
            .ShouldHaveValidationErrorFor(x => x.CustomerName);
    }

    [Fact]
    public void Customer_email_allows_254_characters_but_not_255()
    {
        const string domain = "@acme.test";

        var longest = new string('a', 254 - domain.Length) + domain;
        var tooLong = new string('a', 255 - domain.Length) + domain;

        _validator.TestValidate(TestData.ValidRequest() with { CustomerEmail = longest })
            .ShouldNotHaveValidationErrorFor(x => x.CustomerEmail);

        _validator.TestValidate(TestData.ValidRequest() with { CustomerEmail = tooLong })
            .ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("The length of 'Customer Email' must be 254 characters or fewer. You entered 255 characters.");
    }

    [Theory]
    [InlineData("ZAR")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("zar")]
    public void Supported_currencies_pass(string currency)
    {
        _validator.TestValidate(TestData.ValidRequest() with { Currency = currency })
            .ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("JPY")]
    [InlineData("RAND")]
    public void Unsupported_currencies_fail_with_the_supported_list(string currency)
    {
        var result = _validator.TestValidate(TestData.ValidRequest() with { Currency = currency });

        result.ShouldHaveValidationErrorFor(x => x.Currency);
        if (currency.Length > 0)
        {
            result.ShouldHaveValidationErrorFor(x => x.Currency)
                .WithErrorMessage("Currency must be one of: ZAR, USD, EUR, GBP.");
        }
    }

    [Fact]
    public void Notes_are_optional_and_limited_to_1000_characters()
    {
        _validator.TestValidate(TestData.ValidRequest() with { Notes = null })
            .ShouldNotHaveValidationErrorFor(x => x.Notes);
        _validator.TestValidate(TestData.ValidRequest() with { Notes = new string('n', 1000) })
            .ShouldNotHaveValidationErrorFor(x => x.Notes);
        _validator.TestValidate(TestData.ValidRequest() with { Notes = new string('n', 1001) })
            .ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void At_least_one_line_item_is_required()
    {
        _validator.TestValidate(TestData.ValidRequest() with { LineItems = [] })
            .ShouldHaveValidationErrorFor(x => x.LineItems)
            .WithErrorMessage("An order must have between 1 and 100 line items.");
    }

    [Fact]
    public void Line_items_are_limited_to_100()
    {
        _validator.TestValidate(TestData.ValidRequest() with { LineItems = LineItems(100) })
            .ShouldNotHaveValidationErrorFor(x => x.LineItems);
        _validator.TestValidate(TestData.ValidRequest() with { LineItems = LineItems(101) })
            .ShouldHaveValidationErrorFor(x => x.LineItems);
    }

    [Fact]
    public void Duplicate_skus_are_rejected_case_insensitively_and_named_in_the_message()
    {
        var request = TestData.ValidRequest() with
        {
            LineItems =
            [
                new LineItemRequest { Sku = "SKU-A", Name = "Widget", Quantity = 1, UnitPrice = 1m },
                new LineItemRequest { Sku = " sku-a ", Name = "Widget again", Quantity = 2, UnitPrice = 1m },
            ],
        };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.LineItems)
            .WithErrorMessage("Each SKU may appear only once; combine the quantities instead. Duplicate SKUs: SKU-A.");
    }

    [Fact]
    public void Null_line_item_is_rejected()
    {
        _validator.TestValidate(TestData.ValidRequest() with { LineItems = [null!] })
            .ShouldHaveValidationErrorFor("LineItems[0]");
    }

    [Fact]
    public void Invalid_line_item_is_reported_with_its_index()
    {
        var request = TestData.ValidRequest();
        request = request with { LineItems = [request.LineItems[0], request.LineItems[1] with { Quantity = 0 }] };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor("LineItems[1].Quantity");
    }

    private static List<LineItemRequest> LineItems(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new LineItemRequest { Sku = $"SKU-{i}", Name = $"Item {i}", Quantity = 1, UnitPrice = 1m })
            .ToList();
}
