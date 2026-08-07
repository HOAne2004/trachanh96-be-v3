using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Services;

public class BusinessAuthorizationService : IBusinessAuthorizationService
{
    private readonly IAuthorizationService _microsoftAuthService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BusinessAuthorizationService(
        IAuthorizationService microsoftAuthService,
        IHttpContextAccessor httpContextAccessor)
    {
        _microsoftAuthService = microsoftAuthService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> AuthorizeAsync<TResource>(TResource resource, string operationName)
    {
        // 1. Lấy Principal hiện tại từ HTTP Context (Chỉ hạ tầng mới được phép làm việc này)
        var principal = _httpContextAccessor.HttpContext?.User;

        if (principal == null) return false;

        // 2. Bọc tên hành động vào Requirement của Microsoft
        var requirement = new OperationAuthorizationRequirement { Name = operationName };

        // 3. Đẩy cho Microsoft xử lý
        var result = await _microsoftAuthService.AuthorizeAsync(principal, resource, requirement);

        return result.Succeeded;
    }
}