using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// Không còn nhận Password từ Admin - tài khoản được tạo với 1 hash ngẫu nhiên không ai biết
// (không thể đăng nhập bằng bất kỳ chuỗi nào), người dùng tự thiết lập mật khẩu qua email mời.
public record CreateUserByAdminCommand(
    string Email,
    string FullName,
    string? PhoneNumber,
    List<Guid> RoleIds
) : IRequest<Result<Guid>>;

public class CreateUserByAdminCommandValidator : AbstractValidator<CreateUserByAdminCommand>
{
    public CreateUserByAdminCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.");

        RuleFor(x => x.RoleIds)
            .NotNull()
            .NotEmpty().WithMessage("Phải gán ít nhất 1 quyền (Role) cho tài khoản mới.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^0[35789][0-9]{8}$").WithMessage("Số điện thoại không đúng định dạng VN.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}

public class CreateUserByAdminCommandHandler : IRequestHandler<CreateUserByAdminCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateUserByAdminCommandHandler> _logger;

    private const int InvitationExpiryDays = 7;

    public CreateUserByAdminCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        ICurrentUser currentUser,
        ILogger<CreateUserByAdminCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateUserByAdminCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await _userRepository.IsEmailExistsAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            return Result<Guid>.Failure("Email này đã được sử dụng trong hệ thống.");
        }

        var roleIds = request.RoleIds.Distinct().ToList();
        var validRoles = await _roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);
        if (validRoles.Count() != roleIds.Count)
        {
            return Result<Guid>.Failure("Một hoặc nhiều Quyền (Role) được chọn không tồn tại.");
        }

        var hasProtectedRole = validRoles.Any(r => ProtectedRoleNames.Names.Contains(r.NormalizedName));
        if (hasProtectedRole && !_currentUser.Roles.Any(r => ProtectedRoleNames.Names.Contains(r)))
        {
            return Result<Guid>.Failure("Bạn không có quyền tạo tài khoản với vai trò hệ thống cấp cao.");
        }

        try
        {
            // Hash của 1 chuỗi ngẫu nhiên không ai biết - không ai (kể cả Admin) đăng nhập được
            // bằng bất kỳ chuỗi nào cho đến khi user tự thiết lập mật khẩu qua email mời.
            var placeholderPasswordHash = _passwordHasher.Hash(SecureTokenGenerator.GenerateUrlSafeToken());

            var newUser = new User(
                email: request.Email,
                fullName: request.FullName,
                passwordHash: placeholderPasswordHash,
                rawPhone: request.PhoneNumber
            );

            // Admin tự tay nhập email nên coi như đã xác nhận địa chỉ hợp lệ - khác với
            // RegisterUserCommand (tự đăng ký) cần OTP để tự xác thực quyền sở hữu email.
            newUser.VerifyEmailByAdmin();

            // Tái dùng đúng cơ chế PasswordResetToken có sẵn (không cần Domain method mới):
            // "thiết lập mật khẩu lần đầu" và "quên mật khẩu" đều là "có token hợp lệ, đặt mật khẩu mới".
            var invitationToken = SecureTokenGenerator.GenerateUrlSafeToken();
            newUser.SetPasswordResetToken(invitationToken, expiryMinutes: InvitationExpiryDays * 24 * 60, raiseEvent: false);

            newUser.SyncRoles(roleIds);
            newUser.MarkCreatedByAdmin(roleIds);

            newUser.RequestAccountInvitationEmail(invitationToken);

            _userRepository.Add(newUser);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(newUser.Id);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}