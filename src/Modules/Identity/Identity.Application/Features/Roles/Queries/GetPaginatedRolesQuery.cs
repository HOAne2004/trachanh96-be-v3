using FluentValidation;
using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Roles.Queries;

// ==========================================================
// 1. THE QUERY
// ==========================================================
public record GetPaginatedRolesQuery(
    int PageIndex = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : IRequest<Result<PagedResult<RoleAdminDto>>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class GetPaginatedRolesQueryValidator : AbstractValidator<GetPaginatedRolesQuery>
{
    public GetPaginatedRolesQueryValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThan(0).WithMessage("Trang hiện tại phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Số bản ghi mỗi trang phải lớn hơn 0.")
            .LessThanOrEqualTo(100).WithMessage("Không được lấy quá 100 bản ghi mỗi lần.");
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class GetPaginatedRolesQueryHandler : IRequestHandler<GetPaginatedRolesQuery, Result<PagedResult<RoleAdminDto>>>
{
    private readonly IRoleRepository _roleRepository;

    public GetPaginatedRolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<PagedResult<RoleAdminDto>>> Handle(GetPaginatedRolesQuery request, CancellationToken cancellationToken)
    {
        var (roles, totalCount) = await _roleRepository.GetPaginatedAsync(
            request.PageIndex,
            request.PageSize,
            request.SearchTerm,
            cancellationToken);

        var pagedResult = new PagedResult<RoleAdminDto>(roles.ToList(), totalCount, request.PageIndex, request.PageSize);

        return Result<PagedResult<RoleAdminDto>>.Success(pagedResult);
    }
}