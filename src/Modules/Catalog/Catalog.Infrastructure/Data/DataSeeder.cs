
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using Shared.Domain.ValueObjects;

namespace Catalog.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(CatalogDbContext context)
        {
            // Đảm bảo Database đã được tạo & chạy hết Migration
            await context.Database.MigrateAsync();

            // 1. SEED CATEGORIES
            var categoriesToSeed = new List<string>
        {
            "Trà Chanh",
            "Trà Sữa",
            "Trà Trái Cây",
            "Cà Phê"
        };

            // Lấy số DisplayOrder lớn nhất hiện đang có trong Database
            // Dùng ép kiểu (int?) để phòng trường hợp bảng đang rỗng hoàn toàn thì trả về 0
            int currentMaxOrder = await context.Categories.MaxAsync(c => (int?)c.DisplayOrder) ?? 0;

            foreach (var catName in categoriesToSeed)
            {
                // Kiểm tra: Nếu chưa có Category mang tên này thì mới Add
                if (!await context.Categories.AnyAsync(c => c.Name == catName))
                {
                    currentMaxOrder++; // Tự động tăng thứ tự lên 1

                    // Khởi tạo Category với số Order mới nhất
                    var newCategory = new Category(catName, currentMaxOrder);
                    context.Categories.Add(newCategory);
                }
            }
            await context.SaveChangesAsync();


            // 2. SEED TOPPINGS
            var toppings = new List<Topping>
        {
            new Topping("Trân châu đen", Money.Create(5000, "VND"), "https://placehold.co/100x100/16a34a/white?text=TC+Den"),
            new Topping("Thạch nha đam", Money.Create(5000, "VND"), "https://placehold.co/100x100/16a34a/white?text=Nha+Dam"),
            new Topping("Trân châu trắng", Money.Create(7000, "VND"), "https://placehold.co/100x100/16a34a/white?text=TC+Trang")
        };

            foreach (var topping in toppings)
            {
                if (!await context.Toppings.AnyAsync(t => t.Name == topping.Name))
                {
                    context.Toppings.Add(topping);
                }
            }
            await context.SaveChangesAsync();


            // 3. SEED PRODUCTS
            // Lấy CategoryId thật từ DB để tránh hardcode '1'
            var traChanhCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Trà Chanh");

            if (traChanhCat != null)
            {
                // THÊM ĐIỀU KIỆN CHECK: Chỉ add nếu tên sản phẩm chưa tồn tại
                if (!await context.Products.AnyAsync(p => p.Name == "Trà Chanh Truyền Thống"))
                {
                    var traChanh = new Product(
                        Guid.NewGuid(),
                        traChanhCat.Id, // Sử dụng Id vừa lấy từ DB cho chính xác
                        "Trà Chanh Truyền Thống",
                        ProductTypeEnum.Drink,
                        Money.Create(20000, "VND"),
                        5
                    );

                    // Bổ sung thông tin chi tiết
                    traChanh.UpdateDetails(
                        traChanh.Name,
                        "Vị chua thanh mát từ chanh tươi kết hợp cốt trà hảo hạng.",
                        "Trà, Chanh, Đường",
                        "https://link-anh.com/tra-chanh.jpg",
                        5
                    );

                    context.Products.Add(traChanh);
                    await context.SaveChangesAsync();
                }
            }

            if (traChanhCat != null && !await context.Products.AnyAsync(p => p.Name == "Trà Chanh Sả Nha Đam"))
            {
                var traChanhSa = new Product(
                    Guid.NewGuid(),
                    traChanhCat.Id,
                    "Trà Chanh Sả Nha Đam",
                    ProductTypeEnum.Drink,
                    Money.Create(25000, "VND"),
                    3);
                traChanhSa.UpdateDetails(
                    traChanhSa.Name,
                    "Thơm lừng mùi sả tươi, kết hợp thạch nha đam giòn sật thanh mát.",
                    "Trà đen, chanh tươi, sả tươi, thạch nha đam",
                    "https://link-anh.com/tra-chanh-sa.jpg",
                    5
                );

                // 1. Thêm tuỳ chọn Đá / Đường
                traChanhSa.AllowedIceLevels.AddRange(new[] { IceLevelEnum.None, IceLevelEnum.I50, IceLevelEnum.I100 });
                traChanhSa.AllowedSugarLevels.AddRange(new[] { SugarLevelEnum.S50, SugarLevelEnum.S70, SugarLevelEnum.S100 });

                // 2. Thêm Kích cỡ (Size)
                traChanhSa.AddOrUpdateSize(SizeEnum.M, Money.Create(0, "VND"));       // Size M không cộng tiền
                traChanhSa.AddOrUpdateSize(SizeEnum.L, Money.Create(10000, "VND"));   // Size L + 10k

                // 3. Thêm Topping
                var thachNhaDam = await context.Toppings.FirstOrDefaultAsync(t => t.Name == "Thạch nha đam");
                if (thachNhaDam != null)
                {
                    traChanhSa.AddOrUpdateTopping(thachNhaDam.Id, Money.Create(5000, "VND"), maxQuantity: 2);
                }

                var tranChauTrang = await context.Toppings.FirstOrDefaultAsync(t => t.Name == "Trân châu trắng");
                if (tranChauTrang != null)
                {
                    traChanhSa.AddOrUpdateTopping(tranChauTrang.Id, Money.Create(8000, "VND"), maxQuantity: 1);
                }

                context.Products.Add(traChanhSa);
                await context.SaveChangesAsync();
            }
        }
    }
}
