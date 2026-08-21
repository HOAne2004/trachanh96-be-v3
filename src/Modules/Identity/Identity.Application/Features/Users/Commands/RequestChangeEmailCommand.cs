using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

public record RequestChangeEmailCommand(
    string NewEmail
) : IRequest<Result<string>>;

public class RequestChangeEmailCommandValidator : AbstractValidator<RequestChangeEmailCommand>
{
    public RequestChangeEmailCommandValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("Email mới không được để trống.")
            .EmailAddress().WithMessage("Email mới không hợp lệ.");
    }
}

public class RequestChangeEmailCommandHandler : IRequestHandler<RequestChangeEmailCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public RequestChangeEmailCommandHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IIdentityUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<Result<string>> Handle(RequestChangeEmailCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<string>.Failure("Bạn chưa đăng nhập.");

        var emailExists = await _userRepository.IsEmailExistsAsync(request.NewEmail, cancellationToken);
        if (emailExists)
            return Result<string>.Failure("Email này đã được sử dụng bởi một tài khoản khác.");

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
            return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");

        try
        {
            var otpToken = SecureTokenGenerator.GenerateReadableCode(6);
            // RequestEmailChange giờ tự raise ChangeEmailRequestedEvent.
            user.RequestEmailChange(request.NewEmail, otpToken, expiryHours: 24);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);


            return Result<string>.Success(
                "Đã gửi mã xác nhận đến email mới. Vui lòng kiểm tra hộp thư và xác nhận để hoàn tất " +
                "thay đổi. Email đăng nhập hiện tại của bạn chưa thay đổi cho đến khi được xác nhận.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}