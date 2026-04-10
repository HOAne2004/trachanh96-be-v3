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

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Application.Models;
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

            // ĐÃ SỬA: Đoạn Events phải nằm ở ĐÂY (bên trong AddJwtBearer)
            options.Events = new JwtBearerEvents
            {
                // Bắt lỗi Token hết hạn hoặc sai chữ ký
                OnAuthenticationFailed = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var errorCode = context.Exception is SecurityTokenExpiredException
                        ? "AUTH_TOKEN_EXPIRED"
                        : "AUTH_TOKEN_INVALID";

                    var message = context.Exception is SecurityTokenExpiredException
                        ? "Phiên đăng nhập đã hết hạn."
                        : "Token xác thực không hợp lệ.";

                    var result = JsonSerializer.Serialize(new ErrorResponse(errorCode, message), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    return context.Response.WriteAsync(result);
                },

                // Sự kiện khi user chưa có Token hợp lệ (401)
                OnChallenge = context =>
                {
                    if (context.Response.HasStarted) return Task.CompletedTask;

                    context.HandleResponse(); 
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new ErrorResponse("AUTH_UNAUTHORIZED", "Hãy đăng nhập để thực hiện chức năng này."), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }); return context.Response.WriteAsync(result);
                },
                // Sự kiện khi user đã đăng nhập nhưng không đủ quyền (VD: Khách hàng gọi API của Admin) (403)
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new ErrorResponse("AUTH_FORBIDDEN", "Bạn không có quyền thực hiện chức năng này."), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }); return context.Response.WriteAsync(result);
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