using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// ==========================================================
// 1. THE COMMAND
// ==========================================================
public record CreateUserByAdminCommand(
    string Email,
    string FullName,
    string Password,
    string? PhoneNumber,
    List<Guid> RoleIds
) : IRequest<Result<Guid>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
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

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải dài ít nhất 6 ký tự.");

        RuleFor(x => x.RoleIds)
            .NotNull()
            .NotEmpty().WithMessage("Phải gán ít nhất 1 quyền (Role) cho tài khoản mới.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$").WithMessage("Số điện thoại không đúng định dạng VN.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class CreateUserByAdminCommandHandler : IRequestHandler<CreateUserByAdminCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserByAdminCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(CreateUserByAdminCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra Email tồn tại
        var emailExists = await _userRepository.IsEmailExistsAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            return Result<Guid>.Failure("Email này đã được sử dụng trong hệ thống.");
        }

        // 2. Validate danh sách Roles
        var validRoles = await _roleRepository.GetRolesByIdsAsync(request.RoleIds, cancellationToken);
        if (validRoles.Count() != request.RoleIds.Distinct().Count())
        {
            return Result<Guid>.Failure("Một hoặc nhiều Quyền (Role) được chọn không tồn tại.");
        }

        try
        {
            // 3. Khởi tạo User mới
            var hashedPassword = _passwordHasher.Hash(request.Password);
            var newUser = new User(
                email: request.Email,
                fullName: request.FullName,
                passwordHash: hashedPassword,
                rawPhone: request.PhoneNumber
            );

            // 4. Admin tạo -> Tự động đánh dấu EmailVerified = true
            newUser.VerifyEmailByAdmin();

            // 5. Gán danh sách Role
            newUser.SyncRoles(request.RoleIds);

            // 6. Lưu Database
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