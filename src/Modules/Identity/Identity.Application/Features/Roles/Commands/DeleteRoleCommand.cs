using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Roles.Commands;

public record DeleteRoleCommand(Guid RoleId) : IRequest<Result<string>>;

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("ID Vai trò (Role) không được để trống.");
    }
}

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<string>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return Result<string>.Failure("Không tìm thấy Vai trò (Role) cần xóa.");
        }

        // Chỉ so NormalizedName - đây là nguồn duy nhất đáng tin cậy (Name thô không được
        // chuẩn hóa nên có thể trùng khớp sai/thiếu, xem giải thích ở lượt review Role.cs).
        if (SystemReservedRoleNames.Names.Contains(role.NormalizedName))
        {
            return Result<string>.Failure($"Không thể xóa Vai trò hệ thống '{role.Name}'. Đây là Vai trò mặc định của ứng dụng.");
        }

        var isRoleInUse = await _roleRepository.IsRoleInUseAsync(role.Id, cancellationToken);
        if (isRoleInUse)
        {
            return Result<string>.Failure($"Không thể xóa Vai trò '{role.Name}' vì đang có người dùng giữ Vai trò này. Vui lòng thu hồi Vai trò khỏi tất cả người dùng trước khi xóa.");
        }

        try
        {
            _roleRepository.Delete(role);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success($"Đã xóa Vai trò '{role.Name}' thành công.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}