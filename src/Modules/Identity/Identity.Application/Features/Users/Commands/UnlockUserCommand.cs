using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.Features.Users.Commands
{
    // ==========================================================
    // COMMAND MỞ KHÓA TÀI KHOẢN
    // ==========================================================
    public record UnlockUserCommand(Guid TargetUserId) : IRequest<Result<string>>;

    public class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IIdentityUnitOfWork _unitOfWork;

        public UnlockUserCommandHandler(IUserRepository userRepository, IIdentityUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<string>> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
            if (user == null) return Result<string>.Failure("Không tìm thấy người dùng.");

            try
            {
                user.UnlockAccount();

                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<string>.Success("Đã mở khóa tài khoản thành công. Người dùng có thể đăng nhập lại.");
            }
            catch (DomainException ex) { return Result<string>.Failure(ex.Message); }
        }
    }
}
