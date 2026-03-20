using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Areas.Commands
{
    public record UpdateAreaCommand(
        Guid PublicId,
        int AreaId,
        string Name,
        bool IsActive) : IRequest<Result>;

    public class UpdateAreaCommandValidator: AbstractValidator<UpdateAreaCommand>
    {
        public UpdateAreaCommandValidator()
        {
            RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");
            RuleFor(x => x.AreaId).GreaterThan(0).WithMessage("ID khu vực không hợp lệ.");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên khu vực không được để trống.")
                .MaximumLength(100).WithMessage("Tên khu vực không vượt quá 100 ký tự.");
        }
    }

    public class UpdateAreaCommandHandler : IRequestHandler<UpdateAreaCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IUnitOfWork _unitOfWork;
        public UpdateAreaCommandHandler(IStoreRepository storeRepository, IUnitOfWork unitOfWork)
        {
            _storeRepository = storeRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result> Handle(UpdateAreaCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetAggregateAsync(request.PublicId, cancellationToken);
            if (store == null) return Result.Failure("Cửa hàng không tồn tại hoặc đã bị xóa.");
            try
            {
                store.UpdateArea(request.AreaId, request.Name, request.IsActive);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure($"Không thể cập nhật khu vực: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return Result.Failure($"Dữ liệu không hợp lệ: {ex.Message}");
            }
        }
    }
}

