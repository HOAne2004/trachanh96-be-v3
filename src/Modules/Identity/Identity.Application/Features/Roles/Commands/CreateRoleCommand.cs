using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Roles.Commands;

public record CreateRoleCommand(
    string Name,
    string? Description,
    List<string>? PermissionCodes
) : IRequest<Result<Guid>>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên Vai trò (Role) không được để trống.")
            .MaximumLength(50).WithMessage("Tên Vai trò không được vượt quá 50 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Mô tả không được vượt quá 250 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var isNameExists = await _roleRepository.IsNameExistsAsync(request.Name, excludeRoleId: null, cancellationToken);
        if (isNameExists)
        {
            return Result<Guid>.Failure($"Tên Vai trò '{request.Name}' đã tồn tại trong hệ thống.");
        }

        var distinctCodes = request.PermissionCodes?.Distinct().ToList() ?? new List<string>();
        var validPermissions = new List<Permission>();

        if (distinctCodes.Any())
        {
            validPermissions = (await _permissionRepository.GetByCodesAsync(distinctCodes, cancellationToken)).ToList();
            if (validPermissions.Count != distinctCodes.Count)
            {
                return Result<Guid>.Failure("Có một hoặc nhiều mã Quyền (Permission) không hợp lệ.");
            }
        }

        try
        {
            var newRole = new Role(request.Name, request.Description);

            foreach (var permission in validPermissions)
            {
                newRole.AddPermission(permission);
            }

            _roleRepository.Add(newRole);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(newRole.Id);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}