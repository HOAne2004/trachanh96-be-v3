using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureExplicitKeyGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Không có DDL nào cần thiết: ValueGeneratedNever() chỉ thay đổi cách EF Core
            // (runtime) tự suy luận trạng thái Added/Modified cho entity - không ảnh hưởng
            // cấu trúc cột "uuid NOT NULL" đã có sẵn dưới PostgreSQL.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}