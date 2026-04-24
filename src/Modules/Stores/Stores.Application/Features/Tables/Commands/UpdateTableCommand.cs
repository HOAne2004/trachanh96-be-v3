using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Tables.Commands
{
    public record UpdateTableCommand(Guid PublicId, int TableId, string Name, int SeatCapacity, bool IsActive) : ICommand<Result>;

    public class UpdateTableCommandValidator : AbstractValidator<UpdateTableCommand>
    {
        public UpdateTableCommandValidator()
        {
            RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ."); ;
            RuleFor(x => x.TableId).GreaterThan(0).WithMessage("ID khu vực không hợp lệ.");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50).WithMessage("Tên bàn hợp lệ từ 1-50 ký tự.");
            RuleFor(x => x.SeatCapacity).InclusiveBetween(1, 20).WithMessage("Sức chứa từ 1 đến 20 người.");
        }
    }

    public class UpdateTableCommandHandler : IRequestHandler<UpdateTableCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;

        public UpdateTableCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public async Task<Result> Handle(UpdateTableCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetAggregateAsync(request.PublicId, cancellationToken);
            if (store == null) return Result.Failure("Cửa hàng không tồn tại.");

            try
            {
                store.UpdateTable(request.TableId, request.Name, request.SeatCapacity, request.IsActive);
                return Result.Success();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return Result.Failure(ex.Message);
            }
        }
    }
}
