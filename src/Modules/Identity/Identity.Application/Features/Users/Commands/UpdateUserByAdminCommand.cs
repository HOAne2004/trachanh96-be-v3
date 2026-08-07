using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.Features.Users.Commands
{
    public record UpdateUserByAdminCommand(
        Guid TargetUserId,
        string FullName,
        string Email,
        string? PhoneNumber
    ) : IRequest<Result<string>>;
    public class UpdateUserByAdminCommandValidator : AbstractValidator<UpdateUserByAdminCommand>
    {
        public UpdateUserByAdminCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Email không hợp lệ.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống.")
                .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$").WithMessage("Số điện thoại không đúng định dạng VN.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }
    }

    public class UpdateUserByAdminCommandHandler : IRequestHandler<UpdateUserByAdminCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IIdentityUnitOfWork _unitOfWork;

        public UpdateUserByAdminCommandHandler(IUserRepository userRepository, IIdentityUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<string>> Handle(UpdateUserByAdminCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm User cần sửa
            var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
            if (user == null)
                return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");

            // 2. Kiểm tra trùng lặp Email nếu Email bị thay đổi
            if (!user.Email.Value.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
            {
                var isEmailExists = await _userRepository.IsEmailExistsAsync(request.Email, cancellationToken);
                if (isEmailExists)
                    return Result<string>.Failure("Email này đã được sử dụng bởi một tài khoản khác.");
            }

            try
            {
                // 3. Gọi Domain Logic
                user.AdminUpdateUser(request.FullName, request.Email, request.PhoneNumber);

                // 4. Lưu thay đổi
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<string>.Success("Cập nhật thông tin người dùng thành công.");
            }
            catch (DomainException ex)
            {
                return Result<string>.Failure(ex.Message);
            }
        }
    }
}
