using MediatR;
using ModularShop.Modules.Sales.Contracts;
using ModularShop.Modules.Warehouse.Application.UseCases;

namespace ModularShop.Modules.Warehouse.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Reacts to the Sales module's <see cref="OrderCancelled"/> integration event by returning the ordered
/// units to stock — the mirror of <see cref="DecrementStockOnOrderPlaced"/>. A thin adapter over
/// <see cref="RestockUseCase"/>; MediatR discovers it when the host scans the Warehouse Infrastructure assembly.
/// </summary>
internal sealed class RestockOnOrderCancelled : INotificationHandler<OrderCancelled>
{
    private readonly RestockUseCase _restock;

    public RestockOnOrderCancelled(RestockUseCase restock) => _restock = restock;

    public Task Handle(OrderCancelled notification, CancellationToken cancellationToken)
    {
        var changes = notification.Lines
            .Select(l => new ProductStockChange(l.ProductId, l.Quantity))
            .ToList();
        return _restock.ExecuteAsync(changes, cancellationToken);
    }
}
