using Catalog.Application.Interfaces;
using Shared.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Shared.Application.Models;

namespace Catalog.Application.Features.Toppings
{
    public record UpdateToppingCommand(
        int Id,
        string Name,
        decimal BasePrise,
        string Currency = "VND"
    ) : IRequest<Result>;

    public class UpdateToppingCommandValidator : AbstractValidator<UpdateToppingCommand>
    {
        public UpdateToppingCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id không hợp lệ.");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên topping không được để trống.")
                .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Tên không được chỉ chứa khoảng trắng.")
                .MaximumLength(255).WithMessage("Tên topping không quá 255 ký tự.");
            RuleFor(x => x.BasePrise).GreaterThanOrEqualTo(0).WithMessage("Giá cơ sở không được âm.");
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
            // 1. Lấy Topping từ DB
            var topping = await _toppingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (topping == null) return Result.Failure("Không tìm thấy topping.");
            // 2. Kiểm tra trùng lặp tên (Bỏ qua chính ID hiện tại)
            if (await _toppingRepository.ExistsByNameAsync(request.Name, request.Id, cancellationToken))
            {
                return Result.Failure($"Topping '{request.Name}' đã tồn tại.");
            }
            // 3. Cập nhật thông tin
            var basePrice = Money.Create(request.BasePrise, request.Currency);
            topping.UpdateDetails(request.Name, basePrice);
            return Result.Success();
        }
    }
}