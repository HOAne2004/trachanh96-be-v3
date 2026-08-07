using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Roles.Commands;

// ==========================================================
// 1. THE COMMAND
// Truyền ID của Role cần xóa
// ==========================================================
public record DeleteRoleCommand(Guid RoleId) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("ID Vai trò (Role) không được để trống.");
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<string>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    // Danh sách các Role hệ thống bảo vệ nghiêm ngặt (viết hoa chuẩn hóa)
    private static readonly HashSet<string> SystemProtectedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN",
        "SUPER_ADMIN",
        "CUSTOMER"
    };

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm Role trong Database
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return Result<string>.Failure("Không tìm thấy Vai trò (Role) cần xóa.");
        }

        // 2. BẢO MẬT: Kiểm tra xem có phải Role cốt lõi của hệ thống không
        if (SystemProtectedRoles.Contains(role.NormalizedName) || SystemProtectedRoles.Contains(role.Name))
        {
            return Result<string>.Failure($"Không thể xóa Vai trò hệ thống '{role.Name}'. Đây là Vai trò mặc định của ứng dụng.");
        }

        // 3. RÀNG BUỘC TOÀN VẸN: Kiểm tra xem Role có đang được gán cho User nào không
        var isRoleInUse = await _roleRepository.IsRoleInUseAsync(role.Id, cancellationToken);
        if (isRoleInUse)
        {
            return Result<string>.Failure($"Không thể xóa Vai trò '{role.Name}' vì đang có người dùng giữ Vai trò này. Vui lòng thu hồi Vai trò khỏi tất cả người dùng trước khi xóa.");
        }

        try
        {
            // 4. Xóa khỏi Repository
            _roleRepository.Delete(role);

            // 5. Commit Transaction xuống Database
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success($"Đã xóa Vai trò '{role.Name}' thành công.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}