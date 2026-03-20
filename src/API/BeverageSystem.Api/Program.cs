using BeverageSystem.Api.Extensions;
using Identity.Infrastructure;
using Catalog.Infrastructure;
using Shared.Infrastructure;
using Catalog.Application;
using Stores.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH HỆ THỐNG CƠ BẢN
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Dạy ASP.NET Core cách đọc/ghi Enum bằng chữ thay vì số trên toàn hệ thống
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }); ;
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig(); // Gọi từ Extension
builder.Services.AddJwtAuthentication(builder.Configuration); // Gọi từ Extension

builder.Services.AddExceptionHandler<BeverageSystem.Api.Middlewares.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Đăng ký Shared Infrastructure (Email, Interceptors...)
builder.Services.AddSharedInfrastructure(builder.Configuration);

// 2. ĐĂNG KÝ CÁC MODULES
builder.Services.AddIdentityModule(builder.Configuration);

builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddCatalogApplication();

builder.Services.AddStoreInfrastructure(builder.Configuration);
// ==========================================================

var app = builder.Build();

app.UseExceptionHandler();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();