using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Auth.Commands;

public record VerifyEmailCommand(string Email, string Token) : IRequest<Result<string>>;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty().WithMessage("Mã xác thực không được để trống.");
    }
}

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public VerifyEmailCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Chống User Enumeration: Cố tình che giấu việc User có tồn tại hay không
        if (user == null)
            return Result<string>.Failure("Tài khoản không tồn tại hoặc mã xác thực không đúng.");

        try
        {
            // Gọi logic nghiệp vụ từ Aggregate Root
            user.VerifyEmail(request.Token);

            // Lưu thay đổi an toàn qua Unit of Work
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Xác thực Email thành công! Bạn đã có thể đăng nhập.");
        }
        catch (DomainException ex) // CHỈ BẮT LỖI NGHIỆP VỤ (Mã sai, Hết hạn, Đã xác thực...)
        {
            return Result<string>.Failure(ex.Message);
        }
        // Các exception khác như DbUpdateException sẽ đâm xuyên lên GlobalExceptionHandler 
        // để trả về lỗi 500 và log lại cho Developer.
    }
}