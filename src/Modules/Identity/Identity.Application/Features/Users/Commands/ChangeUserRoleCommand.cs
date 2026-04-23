using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.Enums;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Commands
{
    public record ChangeUserRoleCommand(Guid TargetUserPublicId, string NewRole) : ICommand<Result<string>>;

    public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
    {
        public ChangeUserRoleCommandValidator()
        {
            RuleFor(x => x.TargetUserPublicId).NotEmpty();
            RuleFor(x => x.NewRole)
                .NotEmpty()
                .IsEnumName(typeof(UserRoleEnum), caseSensitive: false)
                .WithMessage("Quyền (Role) không hợp lệ.");
        }
    }

    public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;

        public ChangeUserRoleCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<string>> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByPublicIdAsync(request.TargetUserPublicId, cancellationToken);
            if (user == null)
                return Result<string>.Failure("Không tìm thấy người dùng.");

            // Chuyển string sang Enum
            var roleEnum = Enum.Parse<UserRoleEnum>(request.NewRole, ignoreCase: true);

            try
            {
                user.ChangeRole(roleEnum);
                await _userRepository.UpdateAsync(user, cancellationToken);
                return Result<string>.Success($"Đã thay đổi quyền của người dùng thành {roleEnum}.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ex.Message);
            }
        }
    }
}
