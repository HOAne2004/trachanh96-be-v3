using MediatR;

namespace Shared.Application.Interfaces
{
    public interface ICommand<out TResponse> : IRequest<TResponse>
    {
    }
}
