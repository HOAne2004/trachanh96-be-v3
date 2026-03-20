using MediatR;
using FluentValidation;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Tables.Commands
{
    public record DeleteTableCommand(Guid PublicId, int TableId) : IRequest<Result>;

    public class DeleteTableCommandValidator : AbstractValidator<DeleteTableCommand>
    {
        public DeleteTableCommandValidator()
        {
            RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");
            RuleFor(x => x.TableId).GreaterThan(0).WithMessage("ID bàn không hợp lệ.");
        }
    }
    public class DeleteTableCommandHandler : IRequestHandler<DeleteTableCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteTableCommandHandler(IStoreRepository storeRepository, IUnitOfWork unitOfWork)
        {
            _storeRepository = storeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(DeleteTableCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetAggregateAsync(request.PublicId, cancellationToken);
            if (store == null) return Result.Failure("Cửa hàng không tồn tại.");

            try
            {
                store.RemoveTable(request.TableId);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(ex.Message);
            }
        }
    }
}
