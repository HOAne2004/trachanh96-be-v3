using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Commands
{
    // --- COMMAND KHÓA TÀI KHOẢN ---
    // Trừ phi khóa vĩnh viễn, còn không thì ta truyền vào số ngày khóa (LockoutDays)
    public record LockUserCommand(Guid TargetUserPublicId, int LockoutDays) : ICommand<Result<string>>;

    public class LockUserCommandValidator : AbstractValidator<LockUserCommand>
    {
        public LockUserCommandValidator()
        {
            RuleFor(x => x.TargetUserPublicId).NotEmpty();
            RuleFor(x => x.LockoutDays).GreaterThan(0).WithMessage("Số ngày khóa phải lớn hơn 0.");
        }
    }

    public class LockUserCommandHandler : IRequestHandler<LockUserCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;

        public LockUserCommandHandler(IUserRepository userRepository) => _userRepository = userRepository;

        public async Task<Result<string>> Handle(LockUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByPublicIdAsync(request.TargetUserPublicId, cancellationToken);
            if (user == null) return Result<string>.Failure("Không tìm thấy người dùng.");

            try
            {
                var lockoutEndTime = DateTime.UtcNow.AddDays(request.LockoutDays);
                user.LockAccount(lockoutEndTime);
                await _userRepository.UpdateAsync(user, cancellationToken);
                return Result<string>.Success($"Đã khóa tài khoản đến ngày {lockoutEndTime:dd/MM/yyyy}.");
            }
            catch (Exception ex) { return Result<string>.Failure(ex.Message); }
        }
    }


    // --- COMMAND MỞ KHÓA TÀI KHOẢN ---
    public record UnlockUserCommand(Guid TargetUserPublicId) : IRequest<Result<string>>;

    public class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;

        public UnlockUserCommandHandler(IUserRepository userRepository) => _userRepository = userRepository;

        public async Task<Result<string>> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByPublicIdAsync(request.TargetUserPublicId, cancellationToken);
            if (user == null) return Result<string>.Failure("Không tìm thấy người dùng.");

            try
            {
                user.UnlockAccount();
                await _userRepository.UpdateAsync(user, cancellationToken);
                return Result<string>.Success("Đã mở khóa tài khoản thành công.");
            }
            catch (Exception ex) { return Result<string>.Failure(ex.Message); }
        }
    }
}
