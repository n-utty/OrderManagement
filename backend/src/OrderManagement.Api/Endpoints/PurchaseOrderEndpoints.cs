using Microsoft.AspNetCore.Http.HttpResults;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Models;

namespace OrderManagement.Api.Endpoints;

public static class PurchaseOrderEndpoints
{
    private const string GetByIdRouteName = "GetPurchaseOrder";

    public static IEndpointRouteBuilder MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/purchase-orders").WithTags("Purchase orders");

        group.MapPost("/", SubmitAsync)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", ListAsync);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName(GetByIdRouteName)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/status", ChangeStatusAsync)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    // 201 for a new order, 200 for a replay of one already captured; the body is the order either way.
    private static async Task<Results<CreatedAtRoute<PurchaseOrderResponse>, Ok<PurchaseOrderResponse>>> SubmitAsync(
        CreatePurchaseOrderRequest request,
        PurchaseOrderManager manager,
        CancellationToken cancellationToken)
    {
        var result = await manager.SubmitAsync(request, cancellationToken);

        return result.WasCreated
            ? TypedResults.CreatedAtRoute(result.Order, GetByIdRouteName, new { id = result.Order.Id })
            : TypedResults.Ok(result.Order);
    }

    private static async Task<Ok<PagedResult<PurchaseOrderResponse>>> ListAsync(
        PurchaseOrderManager manager,
        CancellationToken cancellationToken,
        OrderStatus? status = null,
        int page = 1,
        int pageSize = PurchaseOrderManager.DefaultPageSize) =>
        TypedResults.Ok(await manager.ListAsync(status, page, pageSize, cancellationToken));

    private static async Task<Ok<PurchaseOrderResponse>> GetAsync(
        Guid id,
        PurchaseOrderManager manager,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await manager.GetAsync(id, cancellationToken));

    private static async Task<Ok<PurchaseOrderResponse>> ChangeStatusAsync(
        Guid id,
        ChangeOrderStatusRequest request,
        PurchaseOrderManager manager,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await manager.ChangeStatusAsync(id, request, cancellationToken));
}
