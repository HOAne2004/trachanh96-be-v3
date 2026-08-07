using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Shared.Application.Models;
using Shared.Application.Interfaces;

namespace Identity.Application.Features.Auth.Commands;

// 1. Command (Trả về Guid là Id của User)
public record RegisterUserCommand(
    string Email,
    string FullName,
    string Password
) : IRequest<Result<Guid>>;

// 2. Validator
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải dài ít nhất 6 ký tự.")
            .Matches(@"[A-Z]+").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ hoa.")
            .Matches(@"[0-9]+").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ số.");
    }
}

// 3. Handler
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository; 
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra Email trùng lặp
        var emailExists = await _userRepository.IsEmailExistsAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            return Result<Guid>.Failure("Email này đã được đăng ký trong hệ thống.");
        }

        // 2. Mã hóa mật khẩu
        var hashedPassword = _passwordHasher.Hash(request.Password);

        // 3. Khởi tạo Domain Entity
        var newUser = new User(
            email: request.Email,
            fullName: request.FullName,
            passwordHash: hashedPassword
        );

        // 4. Gán Role mặc định "CUSTOMER"
        // (Yêu cầu IRoleRepository phải có hàm GetByNormalizedNameAsync)
        var customerRole = await _roleRepository.GetByNormalizedNameAsync("CUSTOMER", cancellationToken);
        if (customerRole != null)
        {
            newUser.AssignRole(customerRole.Id);
        }

        // 5. Sinh mã xác thực (Ví dụ dùng Guid N hoặc tạo OTP 6 số tùy UI/UX)
        var verifyToken = Guid.NewGuid().ToString("N"); // Tạo chuỗi ngẫu nhiên không có dấu gạch ngang
        newUser.SetVerificationToken(verifyToken, expiryHours: 24);

        // 6. Lưu vào Database (Phải lưu thành công mới được gửi Email)
        _userRepository.Add(newUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Gửi Email (Bất đồng bộ)
        // Lưu ý ở Enterprise: Việc gửi Email thường được đẩy vào Message Queue (RabbitMQ)
        // hoặc dùng Outbox Pattern để không làm chậm request. Hiện tại gọi trực tiếp là chấp nhận được.
        try
        {
            await _emailService.SendVerificationEmailAsync(newUser.Email.Value, newUser.FullName, verifyToken);
        }
        catch (Exception)
        {
            // Log lại lỗi gửi email nhưng VẪN trả về Success cho User. 
            // Họ có thể bấm nút "Gửi lại mã xác nhận" sau.
        }

        return Result<Guid>.Success(newUser.Id);
    }
}