using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.Features.Users.Queries
{
    // ==========================================================
    // 1. DTO CHUYÊN DỤNG CHO ADMIN (Chứa nhiều thông tin hơn Khách)
    // ==========================================================
    public record UserAdminDto(
        Guid PublicId,
        string Email,
        string FullName,
        string? Phone,
        string Role,
        string Status,
        DateTime CreatedAt
    );

    // ==========================================================
    // 2. THE QUERY (Chứa các tham số Phân trang & Lọc)
    // ==========================================================
    public record GetPaginatedUsersQuery(
        int PageIndex = 1,
        int PageSize = 10,
        string? SearchTerm = null, // Tìm theo tên hoặc email
        string? Role = null,       // Lọc theo Customer/Staff/Manager/Admin
        string? Status = null      // Lọc theo Active/Locked
    ) : IRequest<Result<PagedResult<UserAdminDto>>>;

    // ==========================================================
    // 3. THE VALIDATOR (Bảo vệ Database khỏi các query vô lý)
    // ==========================================================
    public class GetPaginatedUsersQueryValidator : AbstractValidator<GetPaginatedUsersQuery>
    {
        public GetPaginatedUsersQueryValidator()
        {
            RuleFor(x => x.PageIndex)
                .GreaterThan(0).WithMessage("Trang hiện tại phải lớn hơn 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Số lượng bản ghi trên một trang phải lớn hơn 0.")
                .LessThanOrEqualTo(100).WithMessage("Không được lấy quá 100 bản ghi mỗi lần để tránh sập Server.");
        }
    }

    // ==========================================================
    // 4. THE HANDLER
    // ==========================================================
    public class GetPaginatedUsersQueryHandler : IRequestHandler<GetPaginatedUsersQuery, Result<PagedResult<UserAdminDto>>>
    {
        private readonly IUserRepository _userRepository;

        public GetPaginatedUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<PagedResult<UserAdminDto>>> Handle(GetPaginatedUsersQuery request, CancellationToken cancellationToken)
        {
            // Gọi xuống Repository để lấy dữ liệu ĐÃ ĐƯỢC PHÂN TRANG TỪ DATABASE
            var (users, totalCount) = await _userRepository.GetPaginatedAsync(
                request.PageIndex,
                request.PageSize,
                request.SearchTerm,
                request.Role,
                request.Status,
                cancellationToken);

            // Map sang DTO
            var dtos = users.Select(u => new UserAdminDto(
                PublicId: u.PublicId,
                Email: u.Email.Value, // Nhớ gọi .Value vì đây là ValueObject
                FullName: u.FullName,
                Phone: u.Phone?.Value,
                Role: u.Role.ToString(),
                Status: u.Status.ToString(),
                CreatedAt: u.CreatedAt
            )).ToList();

            // Đóng gói vào PagedResult (Cái class xịn sò bạn đã viết ở tầng Shared)
            var pagedResult = new PagedResult<UserAdminDto>(dtos, totalCount, request.PageIndex, request.PageSize);

            return Result<PagedResult<UserAdminDto>>.Success(pagedResult);
        }
    }
}
