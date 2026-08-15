using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Roles.Commands;

public record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string? Description
) : IRequest<Result<string>>;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("ID Vai trò (Role) không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên Vai trò không được để trống.")
            .MaximumLength(50).WithMessage("Tên Vai trò không được vượt quá 50 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Mô tả không được vượt quá 250 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<string>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return Result<string>.Failure("Không tìm thấy Vai trò (Role) cần cập nhật.");
        }

        // CHẶN ĐỔI TÊN Role hệ thống: hệ thống tra cứu các Role này bằng NormalizedName ở nơi
        // khác (VD: RegisterUserCommand tra "CUSTOMER") - đổi tên sẽ làm gãy chức năng liên quan
        // ÂM THẦM, không có exception nào cảnh báo tại thời điểm gãy. Vẫn cho phép sửa Description.
        if (SystemReservedRoleNames.Names.Contains(role.NormalizedName))
        {
            var newNormalizedName = Role.NormalizeName(request.Name);
            if (newNormalizedName != role.NormalizedName)
            {
                return Result<string>.Failure($"Không thể đổi tên Vai trò hệ thống '{role.Name}'. Bạn vẫn có thể cập nhật Mô tả.");
            }
        }

        var isNameExists = await _roleRepository.IsNameExistsAsync(
            request.Name,
            excludeRoleId: request.RoleId,
            cancellationToken);

        if (isNameExists)
        {
            return Result<string>.Failure($"Tên Vai trò '{request.Name}' đã được sử dụng bởi một Vai trò khác.");
        }

        try
        {
            role.Rename(request.Name);
            role.ChangeDescription(request.Description);

            await _roleRepository.UpdateAsync(role, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Cập nhật thông tin Vai trò thành công.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}