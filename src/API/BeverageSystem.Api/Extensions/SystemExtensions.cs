/// <summary>
/// [API EXTENSIONS: CẤU HÌNH BẢO MẬT JWT & SWAGGER]
/// Chức năng: Tách biệt logic cấu hình các service của framework ra khỏi Program.cs để dễ bảo trì.
/// 
/// Đặc điểm nổi bật (Rất quan trọng cho Frontend):
/// - AddJwtAuthentication: Không chỉ xác thực Token, mà còn Ghi đè (Override) các sự kiện cốt lõi (OnAuthenticationFailed, OnChallenge, OnForbidden).
/// - Ép các lỗi của hệ thống bảo mật (mặc định không có body) phải trả về chuẩn JSON 'ErrorResponse'.
/// - Giúp Frontend (Axios Interceptors) đồng nhất được định dạng nhận lỗi (luôn là đối tượng có ErrorCode, Message) dù lỗi xảy ra ở Middleware hay trong Controller.
/// 
/// - AddSwaggerConfig: Tích hợp nút "Authorize" (ổ khóa) trên giao diện Swagger UI để Dev/QA dễ dàng dán Token vào test API.
/// </summary>

using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BeverageSystem.Api.Extensions;

public static class SystemExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("Thiếu cấu hình JWT Key");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                // BƯỚC 1 & BƯỚC 2: LOG CHI TIẾT ĐỂ SOI NGUYÊN NHÂN LỆCH STAMP
                OnTokenValidated = async context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("JwtAuthentication");

                    var userIdString = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                    ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    var stampInToken = context.Principal?.FindFirst("SecurityStamp")?.Value;

                    logger.LogInformation("================ [JWT VALIDATING] ================");
                    logger.LogInformation("👉 1. UserId in Token: {UserId}", userIdString);
                    logger.LogInformation("👉 2. SecurityStamp in Token: {Stamp}", stampInToken);

                    if (string.IsNullOrEmpty(userIdString) || string.IsNullOrEmpty(stampInToken))
                    {
                        logger.LogWarning("❌ [FAIL] Token thiếu UserId hoặc SecurityStamp");
                        context.Fail("Token không chứa thông tin bảo mật hợp lệ.");
                        return;
                    }

                    if (Guid.TryParse(userIdString, out var userId))
                    {
                        var cacheService = context.HttpContext.RequestServices.GetRequiredService<ISecurityCacheService>();
                        var cachedStamp = await cacheService.GetSecurityStampAsync(userId);
                        logger.LogInformation("👉 3. SecurityStamp in Cache: {Stamp}", cachedStamp ?? "NULL (Chưa có trong Cache)");

                        if (cachedStamp == null)
                        {
                            // Cold Start: Cache trống, đọc từ DB ra mồi lại
                            var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                            var securityStamp = await userRepository.GetSecurityStampAsync(userId, context.HttpContext.RequestAborted);

                            logger.LogInformation("👉 4. SecurityStamp in DB: {Stamp}", securityStamp.Value.ToString() ?? "NULL (User not found)");

                            if (securityStamp == null || securityStamp.Value.ToString() != stampInToken)
                            {
                                logger.LogWarning("❌ [FAIL] Stamp DB ({DbStamp}) KHÔNG KHỚP với Stamp Token ({TokenStamp})", securityStamp, stampInToken);
                                context.Fail("Tài khoản không tồn tại hoặc Security Stamp đã thay đổi.");
                                return;
                            }

                            // Stamp khớp -> Mồi lại cache
                            await cacheService.SetSecurityStampAsync(userId, securityStamp.Value.ToString(), TimeSpan.FromMinutes(15));
                            logger.LogInformation("✅ [SUCCESS] Stamp hợp lệ. Đã mồi Stamp vào Cache!");
                        }
                        else if (cachedStamp != stampInToken)
                        {
                            logger.LogWarning("❌ [FAIL] Stamp Cache ({CacheStamp}) KHÔNG KHỚP với Stamp Token ({TokenStamp})", cachedStamp, stampInToken);
                            context.Fail("Tài khoản vừa có thay đổi bảo mật. Vui lòng đăng nhập lại.");
                        }
                        else
                        {
                            logger.LogInformation("✅ [SUCCESS] Stamp trong Cache trùng khớp tuyệt đối!");
                        }
                    }
                },

                // BƯỚC 3 & BƯỚC 4: CHỈ LOG VÀ LƯU EXCEPTION, KHÔNG GHI RESPONSE TẠI ĐÂY
                OnAuthenticationFailed = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("JwtAuthentication");

                    logger.LogError(context.Exception, "❌ [JWT_AUTH_FAILED] Lỗi xác thực Token: {Message}", context.Exception.Message);

                    // Lưu Exception vào HttpContext.Items để nhường OnChallenge trả JSON
                    context.HttpContext.Items["AuthError"] = context.Exception;

                    return Task.CompletedTask;
                },

                // DUY NHẤT ONCHALLENGE ĐỨNG RA TRẢ RESPONSE JSON (401)
                OnChallenge = context =>
                {
                    // 1. Chặn đứng ASP.NET Core không cho tự trả lỗi mặc định (Tránh đụng độ HTTP/2)
                    context.HandleResponse();

                    if (context.Response.HasStarted) return Task.CompletedTask;

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var errorCode = "AUTH_UNAUTHORIZED";
                    var message = "Hãy đăng nhập để thực hiện chức năng này.";

                    // Nếu có lỗi truyền sang từ OnAuthenticationFailed
                    if (context.HttpContext.Items.TryGetValue("AuthError", out var errorObj) && errorObj is Exception ex)
                    {
                        errorCode = ex is SecurityTokenExpiredException ? "AUTH_TOKEN_EXPIRED" : "AUTH_TOKEN_INVALID";
                        message = ex is SecurityTokenExpiredException ? "Phiên đăng nhập đã hết hạn." : (ex.Message ?? "Token xác thực không hợp lệ.");
                    }

                    var result = JsonSerializer.Serialize(
                        new ErrorResponse(errorCode, message),
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                    );

                    return context.Response.WriteAsync(result);
                },

                // XỬ LÝ TRẢ RESPONSE KHI BỊ CẤM QUYỀN (403)
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(
                        new ErrorResponse("AUTH_FORBIDDEN", "Bạn không có quyền thực hiện chức năng này."),
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                    );

                    return context.Response.WriteAsync(result);
                }
            };
        });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddSwaggerGen(option =>
        {
            option.SwaggerDoc("v1", new OpenApiInfo { Title = "Beverage API V3", Version = "v1" });

            // Fix duplicate schemaIds error when multiple classes share the same name
            option.CustomSchemaIds(type => type.FullName ?? type.Name);

            option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Vui lòng nhập Token vào ô bên dưới",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            option.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type=ReferenceType.SecurityScheme,
                            Id="Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}