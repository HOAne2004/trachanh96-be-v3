
using MediatR;

namespace Shared.Application.Interfaces
{
    public interface IIdempotentCommand<out TResponse> : IRequest<TResponse>
    {
        Guid IdempotencyKey {  get; }
    }
}
