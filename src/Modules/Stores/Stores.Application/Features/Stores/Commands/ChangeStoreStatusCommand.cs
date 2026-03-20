using MediatR;
using Stores.Domain.Enums;
using Stores.Application.Interfaces;
using Shared.Application.Models;
using Shared.Application.Interfaces;
using FluentValidation;

namespace Stores.Application.Features.Stores.Commands
{
    public record ChangeStoreStatusCommand(
        Guid PublicId,
        StoreStatusEnum Status,
        DateTime? ExpectedOpenDate) : IRequest<Result>;

    public class ChangeStoreStatusCommandValidator : AbstractValidator<ChangeStoreStatusCommand>
    {
        public ChangeStoreStatusCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Trạng thái cửa hàng không hợp lệ.");
            RuleFor(x => x.ExpectedOpenDate)
                .Must((command, expectedOpenDate) =>
                {
                    if (command.Status == StoreStatusEnum.ComingSoon)
                    {
                        return expectedOpenDate.HasValue && expectedOpenDate.Value > DateTime.UtcNow;
                    }
                    return true; 
                })
                .WithMessage("Ngày dự kiến mở cửa phải được cung cấp và phải là ngày trong tương lai khi trạng thái là 'OpeningSoon'.");
        }

    }

    public class ChangeStoreStatusCommandHandler : IRequestHandler<ChangeStoreStatusCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IUnitOfWork _unitOfWork;
        public ChangeStoreStatusCommandHandler(IStoreRepository storeRepository, IUnitOfWork unitOfWork)
        {
            _storeRepository = storeRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result> Handle(ChangeStoreStatusCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
            if (store == null)
            {
                return Result.Failure("Cửa hàng không tồn tại.");
            }
            try
            {
                switch (request.Status)
                {
                    case StoreStatusEnum.ComingSoon:
                        store.MarkAsComingSoon(request.ExpectedOpenDate);
                        break;
                    case StoreStatusEnum.Active:
                        store.OpenStore();
                        break;
                    case StoreStatusEnum.TemporarilyClosed:
                        store.PauseOperations();
                        break;
                    case StoreStatusEnum.ClosedDown:
                        store.CloseDown();
                        break;
                    case StoreStatusEnum.Draft:
                        return Result.Failure("Không thể quay ngược cửa hàng trở lại trạng thái Nháp (Draft).");
                    default:
                        return Result.Failure("Trạng thái cửa hàng không hợp lệ.");
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure($"Không thể chuyển trạng thái: {ex.Message}");
            }
        }
    }
}
