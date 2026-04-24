using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.Interfaces;
namespace Stores.Application.Features.Stores.Commands
{
    public record DeleteStoreCommand(Guid PublicId) : ICommand<Result>;

    public class DeleteStoreCommandValidator : AbstractValidator<DeleteStoreCommand>
    {
        public DeleteStoreCommandValidator()
        {
            RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");
        }
    }

    public class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;

        public DeleteStoreCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public async Task<Result> Handle(DeleteStoreCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
            if (store == null) return Result.Failure("Cửa hàng không tồn tại hoặc đã bị xóa.");

            try
            {
                store.SoftDelete();

                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure($"Không thể xóa cửa hàng: {ex.Message}");
            }
        }
    }
}
