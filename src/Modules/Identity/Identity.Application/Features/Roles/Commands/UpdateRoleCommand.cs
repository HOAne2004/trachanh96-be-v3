using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Roles.Commands;

// ==========================================================
// 1. THE COMMAND
// Truyền Id của Role trên URL và Payload chứa thông tin mới
// ==========================================================
public record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string? Description
) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
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

// ==========================================================
// 3. THE HANDLER
// ==========================================================
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
        // 1. Tìm Role trong CSDL
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return Result<string>.Failure("Không tìm thấy Vai trò (Role) cần cập nhật.");
        }

        // 2. Kiểm tra tên mới có bị trùng với một Role KHÁC trong hệ thống không
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
            // 3. Gọi các Domain Behavior trong Entity Role
            role.Rename(request.Name);
            role.ChangeDescription(request.Description);

            // 4. Lưu thay đổi xuống Database
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