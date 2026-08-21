using BeverageSystem.Api.Extensions;
using Identity.Infrastructure;
using Catalog.Infrastructure;
using Shared.Infrastructure;
using Shared.Application;
using Catalog.Application;
using Stores.Infrastructure;
using Orders.Infrastructure;
using Payments.Infrastructure;
using Payments.Application;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Stores.Application;
using Orders.Application;
using AI.Infrastructure;
using AI.Application;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH HỆ THỐNG CƠ BẢN
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddExceptionHandler<BeverageSystem.Api.Middlewares.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Đọc đúng IP/Scheme thật của client khi chạy sau reverse proxy (nginx trong Docker).
// KnownNetworks CẦN cập nhật đúng subnet của Docker network nội bộ khi triển khai thật -
// để trống tạm thời trong giai đoạn dev (chưa có docker-compose), NHƯNG bắt buộc phải khai báo
// đúng trước khi lên Production, nếu không ai cũng có thể giả mạo IP qua header X-Forwarded-For.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // TODO: sau khi có docker-compose, thêm dòng dưới với đúng subnet của network nginx<->api:
    // options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.28.0.0"), 16));
});

// Rate Limiting cho nhóm endpoint không cần đăng nhập (login, register, forgot-password...) -
// chống brute-force/spam ở tầng HTTP, bổ sung cho cơ chế giới hạn thử sai đã có ở tầng Domain
// (MaxFailedLoginAttempts, MaxPasswordResetAttempts...) vốn chỉ chặn ĐƯỢC SAU KHI request đã
// tới được Handler - Rate Limiting chặn TRƯỚC khi request tốn tài nguyên xử lý.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueLimit = 0
            }));
});

builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddSharedApplication();

// 2. ĐĂNG KÝ CÁC MODULES
builder.Services.AddAIApplication();
builder.Services.AddAIInfrastructure(builder.Configuration);

builder.Services.AddIdentityModule(builder.Configuration, builder.Environment);

builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddCatalogApplication();

builder.Services.AddStoreInfrastructure(builder.Configuration);
builder.Services.AddStoresApplication();

builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddOrdersApplication();

builder.Services.AddPaymentsInfrastructure(builder.Configuration);
builder.Services.AddPaymentsApplication();

// ==========================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Đặt NGAY ĐẦU pipeline, trước mọi middleware khác đọc IP/Scheme.
app.UseForwardedHeaders();

app.UseExceptionHandler();

app.UseCors("AllowViteApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Beverage API V3");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    await app.Services.SeedCatalogDataAsync();
    // await app.Services.SeedIdentityDataAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Lỗi Seed Data toàn hệ thống.");
}

app.Run();