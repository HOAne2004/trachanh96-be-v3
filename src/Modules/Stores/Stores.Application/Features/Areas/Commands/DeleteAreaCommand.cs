using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Areas.Commands
{
    public record DeleteAreaCommand(
        Guid PublicId,
        int AreaId
    ) : ICommand<Result>;

    public class DeleteAreaCommandValidator: AbstractValidator<DeleteAreaCommand>
    {
        public DeleteAreaCommandValidator()
        {
            RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");
            RuleFor(x => x.AreaId).GreaterThan(0).WithMessage("ID khu vực không hợp lệ.");
        }
    }

    public class  DeleteAreaCommandHandler : IRequestHandler<DeleteAreaCommand,  Result>
    {
        private readonly IStoreRepository _storeRepository;

        public  DeleteAreaCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public async Task<Result> Handle(DeleteAreaCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetAggregateAsync(request.PublicId, cancellationToken);
            if (store == null) return Result.Failure("Cửa hàng không tồn tại hoặc đã bị xóa.");
            try
            {
                store.RemoveArea(request.AreaId);
                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure($"Không thể xóa khu vực: {ex.Message}");
            }
        }
    }
}
