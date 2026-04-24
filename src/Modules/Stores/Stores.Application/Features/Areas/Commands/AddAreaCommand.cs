using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Areas.Commands
{
    public record AddAreaCommand(
     Guid PublicId,
     string Name
 ) : ICommand<Result>;

    // 2. Validator
    public class AddAreaCommandValidator : AbstractValidator<AddAreaCommand>
    {
        public AddAreaCommandValidator()
        {
            RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên khu vực không được để trống.")
                .MaximumLength(100).WithMessage("Tên khu vực không vượt quá 100 ký tự.");
        }
    }

    // 3. Handler
    public class AddAreaCommandHandler : IRequestHandler<AddAreaCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;

        public AddAreaCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public async Task<Result> Handle(AddAreaCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetAggregateAsync(request.PublicId, cancellationToken);

            if (store == null) return Result.Failure("Cửa hàng không tồn tại hoặc đã bị xóa.");

            try
            {
                store.AddArea(request.Name);

                return Result.Success();
            }
            catch (InvalidOperationException ex) 
            {
                return Result.Failure($"Không thể thêm khu vực: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return Result.Failure($"Dữ liệu không hợp lệ: {ex.Message}");
            }
        }
    }
}
