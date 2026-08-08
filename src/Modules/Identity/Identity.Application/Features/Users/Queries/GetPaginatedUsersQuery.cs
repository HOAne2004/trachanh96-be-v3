using FluentValidation;
using Identity.Application.DTOs.Request;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Queries
{
    // ==========================================================
    // 1. THE QUERY
    // ==========================================================
    public record GetPaginatedUsersQuery(
        int PageIndex = 1,
        int PageSize = 10,
        string? SearchTerm = null,
        Guid? RoleId = null, 
        string? Status = null
    ) : IRequest<Result<PagedResult<UserAdminDto>>>;

    // ==========================================================
    // 2. THE VALIDATOR
    // ==========================================================
    public class GetPaginatedUsersQueryValidator : AbstractValidator<GetPaginatedUsersQuery>
    {
        public GetPaginatedUsersQueryValidator()
        {
            RuleFor(x => x.PageIndex).GreaterThan(0).WithMessage("Trang hiện tại phải lớn hơn 0.");
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Số bản ghi mỗi trang phải lớn hơn 0.")
                .LessThanOrEqualTo(100).WithMessage("Không được lấy quá 100 bản ghi mỗi lần.");
        }
    }

    // ==========================================================
    // 3. THE HANDLER
    // ==========================================================
    public class GetPaginatedUsersQueryHandler : IRequestHandler<GetPaginatedUsersQuery, Result<PagedResult<UserAdminDto>>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public GetPaginatedUsersQueryHandler(
            IUserRepository userRepository,
            IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<Result<PagedResult<UserAdminDto>>> Handle(GetPaginatedUsersQuery request, CancellationToken cancellationToken)
        {
            // 1. Lấy dữ liệu phân trang từ DB (Repository đã dùng AsNoTracking và Include UserRoles)
            var (users, totalCount) = await _userRepository.GetPaginatedAsync(
                request.PageIndex,
                request.PageSize,
                request.SearchTerm,
                request.RoleId,
                request.Status,
                cancellationToken);

            // 2. TỐI ƯU HÓA CQRS: Lấy toàn bộ tên Role trong 1 Query duy nhất để tránh N+1 Problem
            var allRoleIds = users.SelectMany(u => u.UserRoles).Select(ur => ur.RoleId).Distinct().ToList();
            var roles = await _roleRepository.GetRolesByIdsAsync(allRoleIds, cancellationToken);
            var roleDictionary = roles.ToDictionary(r => r.Id, r => r.Name);

            // 3. Map sang DTO
            var dtos = users.Select(u => new UserAdminDto(
                Id: u.Id,
                Email: u.Email.Value,
                FullName: u.FullName,
                Phone: u.Phone?.Value,
                Roles: u.UserRoles
                        .Where(ur => roleDictionary.ContainsKey(ur.RoleId))
                        .Select(ur => roleDictionary[ur.RoleId])
                        .ToList(),
                Status: u.Status.ToString(),
                CreatedAt: u.CreatedAt
            )).ToList();

            var pagedResult = new PagedResult<UserAdminDto>(dtos, totalCount, request.PageIndex, request.PageSize);

            return Result<PagedResult<UserAdminDto>>.Success(pagedResult);
        }
    }
}