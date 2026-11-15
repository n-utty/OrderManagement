using FluentValidation;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Common.Exceptions;
using OrderManagement.Domain.PurchaseOrders.Models;

namespace OrderManagement.Domain.PurchaseOrders;

public class PurchaseOrderManager(
    IPurchaseOrderRepository repository,
    IValidator<CreatePurchaseOrderRequest> createValidator,
    IValidator<ChangeOrderStatusRequest> changeStatusValidator)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public async Task<SubmitPurchaseOrderResult> SubmitAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Built up front so the incoming fingerprint is ready to compare against an existing order.
        var order = BuildOrder(request);

        var existing = await repository.GetByReferenceAsync(order.ExternalReference, cancellationToken);
        if (existing is not null)
        {
            return ReplayOrConflict(existing, order.Fingerprint);
        }

        if (await repository.TryAddAsync(order, cancellationToken))
        {
            return new SubmitPurchaseOrderResult(ToResponse(order), WasCreated: true);
        }

        // A concurrent submission with the same reference was saved between our lookup and our insert.
        var winner = await repository.GetByReferenceAsync(order.ExternalReference, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Order '{order.ExternalReference}' was rejected as a duplicate but could not be found.");

        return ReplayOrConflict(winner, order.Fingerprint);
    }

    public async Task<PurchaseOrderResponse> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetWithLineItemsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseOrder), id);

        return ToResponse(order);
    }

    public async Task<PagedResult<PurchaseOrderResponse>> ListAsync(
        OrderStatus? status = null,
        int page = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        // Out-of-range paging is clamped rather than rejected: a list view should always show something.
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await repository.ListAsync(status, page, pageSize, cancellationToken);

        return new PagedResult<PurchaseOrderResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<PurchaseOrderResponse> ChangeStatusAsync(
        Guid id,
        ChangeOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await changeStatusValidator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await repository.GetWithLineItemsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseOrder), id);

        var target = request.Status!.Value;

        // Asking for the status the order already has is treated as success,
        // so double clicks and retried requests are harmless.
        if (order.Status == target)
        {
            return ToResponse(order);
        }

        if (!OrderStatusTransitions.CanTransition(order.Status, target))
        {
            throw InvalidTransition(order, target);
        }

        order.Status = target;
        repository.Update(order);
        await repository.SaveChangesAsync(cancellationToken);

        return ToResponse(order);
    }

    private static PurchaseOrder BuildOrder(CreatePurchaseOrderRequest request)
    {
        var order = new PurchaseOrder
        {
            ExternalReference = PurchaseOrderReference.Normalise(request.ExternalReference),
            CustomerId = request.CustomerId.Trim(),
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = OrderStatus.Pending,
        };

        order.LineItems = request.LineItems
            .Select(item => new LineItem
            {
                PurchaseOrderId = order.Id,
                Sku = item.Sku.Trim(),
                Name = item.Name.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            })
            .ToList();

        order.CalculateTotals();
        order.Fingerprint = PurchaseOrderFingerprint.Compute(order);

        return order;
    }

    private static SubmitPurchaseOrderResult ReplayOrConflict(PurchaseOrder existing, string incomingFingerprint)
    {
        if (existing.Fingerprint != incomingFingerprint)
        {
            throw new ConflictException(
                $"An order with external reference '{existing.ExternalReference}' already exists with different details. " +
                "Use a new reference for a new order, or open the existing order to review it.",
                existing.Id);
        }

        // Same content means the rep is retrying an order that already went through:
        // hand back the original so they see it was captured once.
        return new SubmitPurchaseOrderResult(ToResponse(existing), WasCreated: false);
    }

    private static BusinessRuleException InvalidTransition(PurchaseOrder order, OrderStatus target)
    {
        var allowed = OrderStatusTransitions.AllowedFrom(order.Status);

        var message = allowed.Count == 0
            ? $"Order {order.ExternalReference} is {order.Status}, which is a final status, so it can't be changed to {target}."
            : $"Order {order.ExternalReference} can't move from {order.Status} to {target}. " +
              $"Allowed next statuses: {string.Join(" or ", allowed)}.";

        return new BusinessRuleException(message, new Dictionary<string, object?>
        {
            ["currentStatus"] = order.Status.ToString(),
            ["requestedStatus"] = target.ToString(),
            ["allowedTransitions"] = allowed.Select(status => status.ToString()).ToArray(),
        });
    }

    private static PurchaseOrderResponse ToResponse(PurchaseOrder order) => new(
        order.Id,
        order.ExternalReference,
        order.CustomerId,
        order.CustomerName,
        order.CustomerEmail,
        order.Currency,
        order.Notes,
        order.Status,
        order.Subtotal,
        order.Total,
        order.CreatedAt,
        order.UpdatedAt,
        order.LineItems
            .Select(item => new LineItemResponse(item.Sku, item.Name, item.Quantity, item.UnitPrice, item.LineTotal))
            .ToList(),
        OrderStatusTransitions.AllowedFrom(order.Status));
}
