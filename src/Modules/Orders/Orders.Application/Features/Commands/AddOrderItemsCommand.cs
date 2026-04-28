
using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Orders.Application.Interfaces.ExternalServices;
using Orders.Domain.ValueObjects;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Enums;
using Shared.Domain.Exceptions;
using Shared.Domain.ValueObjects;

namespace Orders.Application.Features.Commands
{
    public record OrderItemRequest(
        Guid ProductId,
        SizeEnum SizeName,
        IceLevelEnum? IceLevel,
        SugarLevelEnum? SugarLevel,
        int Quantity,
        string? Notes,
        List<Guid> ToppingIds
        );
    public record AddOrderItemsCommand(
        Guid OrderId,
        List<OrderItemRequest> Items) : ICommand<Result<Guid>>;

    public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemsCommand>
    {
        public AddOrderItemCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty().Must(x => x.Count <= 50);

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty();
                item.RuleFor(i => i.SizeName).IsInEnum();

                // Đá và Đường có thể Null (do tùy món), nhưng nếu có gửi lên thì phải là giá trị Enum hợp lệ
                item.RuleFor(i => i.IceLevel).IsInEnum().When(i => i.IceLevel.HasValue);
                item.RuleFor(i => i.SugarLevel).IsInEnum().When(i => i.SugarLevel.HasValue);

                item.RuleFor(i => i.Quantity).GreaterThan(0);
                item.RuleFor(i => i.Notes).MaximumLength(500);
                item.RuleFor(i => i.ToppingIds).NotNull();
            });
        }
    }

    public class AddOrderItemsCommandHandler : IRequestHandler<AddOrderItemsCommand, Result<Guid>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductSnapshotService _productSnapshotService;

        public AddOrderItemsCommandHandler(
            IOrderRepository orderRepository,
            IProductSnapshotService productSnapshotService)
        {
            _orderRepository = orderRepository;
            _productSnapshotService = productSnapshotService;
        }

        public async Task<Result<Guid>> Handle(AddOrderItemsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null)
                    return Result<Guid>.Failure("Không tìm thấy đơn hàng.");
                order.ClearItems();

                var distinctProductSizes = request.Items
                    .Select(i => (i.ProductId, i.SizeName))
                    .Distinct()
                    .ToList();

                var distinctToppingIds = request.Items
                    .SelectMany(i => i.ToppingIds)
                    .Distinct()
                    .ToList();

                var productsTask = _productSnapshotService.GetProductSnapshotsAsync(distinctProductSizes, cancellationToken);
                var toppingsTask = distinctToppingIds.Any()
                    ? _productSnapshotService.GetToppingSnapshotsAsync(distinctToppingIds, cancellationToken)
                    : Task.FromResult(new List<ToppingSnapshotDto>());

                await Task.WhenAll(productsTask, toppingsTask);

                var productSnapshots = await productsTask;
                var toppingSnapshots = await toppingsTask;

                foreach (var reqItem in request.Items)
                {
                    var product = productSnapshots.FirstOrDefault(p => p.ProductId == reqItem.ProductId && p.Size == reqItem.SizeName);
                    if (product == null)
                        return Result<Guid>.Failure($"Sản phẩm với ID {reqItem.ProductId} và Size {reqItem.SizeName} không tồn tại hoặc đã ngừng bán.");

                    var itemToppings = new List<OrderItemTopping>();
                    foreach (var toppingId in reqItem.ToppingIds)
                    {
                        var topping = toppingSnapshots.FirstOrDefault(t => t.ToppingId == toppingId);
                        if (topping == null)
                            return Result<Guid>.Failure($"Topping với ID {toppingId} không tồn tại.");

                        itemToppings.Add(new OrderItemTopping(
                            topping.ToppingId,
                            topping.ToppingName,
                            Money.Create(topping.Price, topping.Currency),
                            1
                        ));
                    }

                    order.AddItem(
                        product.ProductId,
                        product.ProductName,
                        product.ImageUrl,
                        product.Size, 
                        reqItem.IceLevel,
                        reqItem.SugarLevel,
                        Money.Create(product.Price, product.Currency),
                        reqItem.Quantity,
                        reqItem.Notes,
                        itemToppings
                    );
                }

                return Result<Guid>.Success(order.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(ex.Message);
            }
        }
    }
}
