using FluentValidation;
using MediatR;
using Shared.Application.Models; 

namespace Shared.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            // Nếu Command này không có Validator nào, cho đi tiếp
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        // Chạy tất cả các Validator cùng lúc
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Gom các lỗi lại
        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            // Lấy thông báo lỗi đầu tiên (hoặc gom lại tùy ý bạn)
            var error = string.Join("; ", failures.Select(f => f.ErrorMessage));

            // Trả về đối tượng Result<T> báo lỗi
            // Sử dụng Reflection để tạo ra Result<T>.Failure(...) thay vì Throw Exception
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(resultType)
                    .GetMethod("Failure");

                return (TResponse)failureMethod!.Invoke(null, new object[] { error })!;
            }

            // Nếu không dùng Result<T>, đành phải Throw Exception
            throw new ValidationException(failures);
        }

        // Nếu hợp lệ, cho phép request đi tới Handler
        return await next();
    }
}