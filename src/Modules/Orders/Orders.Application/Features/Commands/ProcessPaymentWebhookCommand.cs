using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Orders.Application.Features.Commands
{
    public record ProcessPaymentWebhookCommand(
    Guid OrderId,
    string TransactionId,
    bool IsSuccess,
    string? ErrorMessage
) : IIdempotentCommand<Result<bool>>
    {
        public Guid IdempotencyKey => GenerateKey();

        private Guid GenerateKey()
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(TransactionId + OrderId));
            return new Guid(hash);
        }
    }

    public class ProcessPaymentWebhookCommandValidator : AbstractValidator<ProcessPaymentWebhookCommand>
    {
        public ProcessPaymentWebhookCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.TransactionId).NotEmpty().WithMessage("Mã giao dịch đối tác không được trống.");
        }
    }

    public class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, Result<bool>>
    {
        private readonly IOrderRepository _orderRepository;
        public ProcessPaymentWebhookCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result<bool>> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _orderRepository.GetByIdWithLockAsync(request.OrderId, cancellationToken);
                if (order == null)
                {
                    return Result<bool>.Failure($"CRITICAL: Nhận được thanh toán nhưng không tìm thấy OrderId: {request.OrderId}");
                }
                if (request.IsSuccess)
                {
                    order.MarkAsPaid(request.TransactionId);
                }
                else
                {
                    order.MarkPaymentFailed(request.ErrorMessage ?? "Khách hàng hủy giao dịch hoặc lỗi ngân hàng.");
                }
                return Result<bool>.Success(true);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure($"{ex.Message}");
            }
        }
    }
}
