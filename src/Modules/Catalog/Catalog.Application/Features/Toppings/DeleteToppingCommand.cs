using Catalog.Application.Interfaces;
using FluentValidation;
using MediatR;
using Shared.Application.Models;

namespace Catalog.Application.Features.Toppings
{
    public record DeleteToppingCommand (int id) : IRequest<Result>;

    public class DeleteToppingCommandValidator : AbstractValidator<DeleteToppingCommand>
    {
        public DeleteToppingCommandValidator()
        {
            RuleFor(x => x.id).GreaterThan(0).WithMessage("ID topping không hợp lệ.");
        }
    }
    public class DeleteToppingCommandHandler : IRequestHandler<DeleteToppingCommand, Result>
    {
        private readonly IToppingRepository _toppingRepository;
        public DeleteToppingCommandHandler(IToppingRepository toppingRepository)
        {
            _toppingRepository = toppingRepository;
        }
        public async Task<Result> Handle(DeleteToppingCommand request, CancellationToken cancellationToken)
        {
            var topping = await _toppingRepository.GetByIdAsync(request.id, cancellationToken);
            if (topping == null)
                return Result.Failure("Topping không tồn tại hoặc đã bị xóa.");
            topping.Delete();
            return Result.Success();
        }
    }
}
