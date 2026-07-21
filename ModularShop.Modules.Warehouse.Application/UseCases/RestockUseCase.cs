using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application.UseCases;

/// <summary>
/// Use case: return on-hand stock for the given products — the inverse of <see cref="DecrementStockUseCase"/>.
/// Invoked by the Warehouse integration-event handler when an order is cancelled. Warehouse OWNS the stock
/// data, so the write happens here against its own entities. Reuses <see cref="ProductStockChange"/>; the
/// products are loaded <b>tracked</b> (by key) so the increment is persisted when the unit of work commits.
/// </summary>
public sealed class RestockUseCase : UseCase
{
    private readonly IReadRepository<Product> _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RestockUseCase> _logger;

    public RestockUseCase(IReadRepository<Product> products, IUnitOfWork unitOfWork, ILogger<RestockUseCase> logger)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(IReadOnlyCollection<ProductStockChange> changes, CancellationToken ct)
    {
        var ids = changes.Select(c => c.ProductId).ToList();
        var products = await _products.GetByIdsAsync(ids, ct); // tracked

        foreach (var change in changes)
        {
            var product = products.FirstOrDefault(p => p.Id == change.ProductId);
            product?.IncreaseStock(change.Quantity);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Warehouse restocked {Count} product line(s).", changes.Count);
    }
}
