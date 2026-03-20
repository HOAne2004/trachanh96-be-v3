using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Shared.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Shared.Application.Models;
using Shared.Application.Interfaces;

namespace Catalog.Application.Features.Toppings
{
    public record CreateToppingCommand(
        string Name,
        decimal BasePrice,
        string Currency = "VND"
    ) : IRequest<Result<int>>;

    public class CreateToppingCommandValidator : AbstractValidator<CreateToppingCommand>
    {
        public CreateToppingCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên topping không được để trống.")
                .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Tên không được chỉ chứa khoảng trắng.")
                .MaximumLength(255).WithMessage("Tên topping không quá 255 ký tự.");
            RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0).WithMessage("Giá cơ sở không được âm.");
        }
    }

    public class CreateToppingCommandHandler : IRequestHandler<CreateToppingCommand, Result<int>>
    {
        private readonly IToppingRepository _toppingRepository;
        private readonly IUnitOfWork _unitOfWork;
        public CreateToppingCommandHandler(IToppingRepository toppingRepository, IUnitOfWork unitOfWork)
        {
            _toppingRepository = toppingRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<int>> Handle(CreateToppingCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra trùng lặp
            if (await _toppingRepository.ExistsByNameAsync(request.Name, null, cancellationToken))
            {
                return Result<int>.Failure($"Topping '{request.Name}' đã tồn tại.");
            }
            // 2. Khởi tạo Entity
            var basePrice = Money.Create(request.BasePrice, request.Currency);
            var topping = new Topping(
                name: request.Name,
                basePrice: basePrice);
            // 3. Thêm vào DB
            _toppingRepository.Add(topping);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(topping.Id);
        }
    }
}
