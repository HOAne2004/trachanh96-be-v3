using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.ValueObjects;

namespace Catalog.Application.Features.Toppings
{
    // ĐÃ SỬA: Bổ sung ImageUrl
    public record CreateToppingCommand(
        string Name,
        decimal BasePrice,
        string? ImageUrl = null,
        string Currency = "VND"
    ) : ICommand<Result<int>>;

    public class CreateToppingCommandValidator : AbstractValidator<CreateToppingCommand>
    {
        public CreateToppingCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên topping không được để trống.")
                .MaximumLength(255).WithMessage("Tên topping không quá 255 ký tự.");
            RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0).WithMessage("Giá cơ sở không được âm.");
        }
    }

    public class CreateToppingCommandHandler : IRequestHandler<CreateToppingCommand, Result<int>>
    {
        private readonly IToppingRepository _toppingRepository;
        public CreateToppingCommandHandler(IToppingRepository toppingRepository)
        {
            _toppingRepository = toppingRepository;
        }
        public async Task<Result<int>> Handle(CreateToppingCommand request, CancellationToken cancellationToken)
        {
            if (await _toppingRepository.ExistsByNameAsync(request.Name, null, cancellationToken))
            {
                return Result<int>.Failure($"Topping '{request.Name}' đã tồn tại.");
            }

            var basePrice = Money.Create(request.BasePrice, request.Currency);
            // ĐÃ SỬA: Truyền ImageUrl vào constructor
            var topping = new Topping(
                name: request.Name,
                basePrice: basePrice,
                imageUrl: request.ImageUrl);

            _toppingRepository.Add(topping);
            return Result<int>.Success(topping.Id);
        }
    }
}