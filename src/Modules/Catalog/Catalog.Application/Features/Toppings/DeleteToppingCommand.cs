using Catalog.Application.Interfaces;
using FluentValidation;
using MediatR;
using Shared.Application.Models;
using Shared.Application.Interfaces;

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
        private readonly IUnitOfWork _unitOfWork;
        public DeleteToppingCommandHandler(IToppingRepository toppingRepository, IUnitOfWork unitOfWork)
        {
            _toppingRepository = toppingRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result> Handle(DeleteToppingCommand request, CancellationToken cancellationToken)
        {
            var topping = await _toppingRepository.GetByIdAsync(request.id, cancellationToken);
            if (topping == null)
                return Result.Failure("Topping không tồn tại hoặc đã bị xóa.");
            topping.Delete();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
