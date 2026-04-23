using Catalog.Application.Interfaces;
using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.ValueObjects;

namespace Catalog.Application.Features.Toppings
{
    // ĐÃ SỬA: Thêm ImageUrl
    public record UpdateToppingCommand(
        int Id,
        string Name,
        decimal BasePrice,
        string? ImageUrl = null,
        string Currency = "VND"
    ) : ICommand<Result>;

    public class UpdateToppingCommandValidator : AbstractValidator<UpdateToppingCommand>
    {
        public UpdateToppingCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id không hợp lệ.");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên topping không được để trống.")
                .MaximumLength(255).WithMessage("Tên topping không quá 255 ký tự.");
            RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0).WithMessage("Giá cơ sở không được âm.");
        }
    }

    public class UpdateToppingCommandHandler : IRequestHandler<UpdateToppingCommand, Result>
    {
        private readonly IToppingRepository _toppingRepository;
        public UpdateToppingCommandHandler(IToppingRepository toppingRepository)
        {
            _toppingRepository = toppingRepository;
        }
        public async Task<Result> Handle(UpdateToppingCommand request, CancellationToken cancellationToken)
        {
            var topping = await _toppingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (topping == null) return Result.Failure("Không tìm thấy topping.");

            if (await _toppingRepository.ExistsByNameAsync(request.Name, request.Id, cancellationToken))
            {
                return Result.Failure($"Topping '{request.Name}' đã tồn tại.");
            }

            var basePrice = Money.Create(request.BasePrice, request.Currency);

            // ĐÃ SỬA: Truyền ImageUrl vào hàm UpdateDetails
            topping.UpdateDetails(request.Name, basePrice, request.ImageUrl);
            return Result.Success();
        }
    }
}