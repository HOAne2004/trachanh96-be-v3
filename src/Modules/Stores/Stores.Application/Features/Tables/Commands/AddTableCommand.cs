using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Tables.Commands
{
    public record AddTableCommand(Guid PublicId, int AreaId, string Name, int SeatCapacity) : IRequest<Result>;

    public class AddTableCommandValidator : AbstractValidator<AddTableCommand>
    {
        public AddTableCommandValidator()
        {
            RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");
            RuleFor(x => x.AreaId).GreaterThan(0).WithMessage("ID khu vực không hợp lệ.");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50).WithMessage("Tên bàn hợp lệ từ 1-50 ký tự.");
            RuleFor(x => x.SeatCapacity).InclusiveBetween(1, 20).WithMessage("Sức chứa từ 1 đến 20 người.");
        }
    }

    public class AddTableCommandHandler : IRequestHandler<AddTableCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddTableCommandHandler(IStoreRepository storeRepository, IUnitOfWork unitOfWork)
        {
            _storeRepository = storeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(AddTableCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetAggregateAsync(request.PublicId, cancellationToken);
            if (store == null) return Result.Failure("Cửa hàng không tồn tại.");

            try
            {
                store.AddTableToArea(request.AreaId, request.Name, request.SeatCapacity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return Result.Failure(ex.Message);
            }
        }
    }
}
