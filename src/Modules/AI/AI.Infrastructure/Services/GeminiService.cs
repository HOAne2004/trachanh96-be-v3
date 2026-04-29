using AI.Application.DTOs;
using AI.Application.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

/*File này sẽ có 3 nhiệm vụ cực kỳ quan trọng:
* 1. Nhúng Menu (System Context) vào cho AI đọc.
* 2. Định nghĩa công cụ AddToCart cho AI biết hình thù của giỏ hàng ra sao.
* 3. Phân tích câu trả lời của AI xem nó muốn "nói chuyện" hay "gọi hàm".
 */
namespace AI.Infrastructure.Services
{
    public class GeminiService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        // Sử dụng model Gemini 1.5 Flash vì nó cực nhanh, miễn phí và hỗ trợ Function Calling xuất sắc
        private const string ModelUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent"; public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["AI:GeminiApiKey"] ?? throw new ArgumentNullException("Thiếu AI:GeminiApiKey");
        }

        public async Task<AIConversationResult> SendMessageAsync(string sessionId, List<MessageDto> history, string systemContext)
        {
            // Chuyển đổi lịch sử chat của bạn thành mảng JSON cho Google
            var formattedContents = history.Select(msg => new
            {
                // Google dùng "user" và "model", nên ta map cho chuẩn
                role = msg.Role.ToLower() == "user" ? "user" : "model",
                parts = new[] { new { text = msg.Content } }
            }).ToArray();

            // 1. TẠO PAYLOAD GỬI LÊN GOOGLE
            var requestBody = new
            {
                // Hướng dẫn hệ thống + Bơm Menu vào đây
                system_instruction = new
                {
                    parts = new[] { new { text = $"Bạn là nhân viên chốt đơn của Trà Chanh 1996. Giọng điệu thân thiện, nhiệt tình. Đây là Menu hiện tại (dạng JSON): {systemContext}. Bạn chỉ tư vấn các món trong Menu này. Nếu khách muốn đặt món, hãy bắt buộc sử dụng công cụ AddToCart." } }
                },
                // Tin nhắn của khách
                contents = formattedContents,

                // Định nghĩa "đồ nghề" cho AI: Hàm AddToCart (Khớp với OrderItemRequest của bạn)
                tools = new[]
                {
                    new
                    {
                        function_declarations = new[]
                        {
                            new
                            {
                                name = "AddToCart",
                                description = "Thêm đồ uống vào giỏ hàng. Gọi hàm này ngay khi khách nói muốn mua/đặt món.",
                                parameters = new
                                {
                                    type = "OBJECT", // Bắt buộc phải là OBJECT
                                    properties = new
                                    {
                                        Items = new // Bọc mảng vào một property tên là 'Items'
                                        {
                                            type = "ARRAY",
                                            items = new {
                                                type = "OBJECT",
                                                properties = new {
                                                    ProductId = new { type = "STRING", description = "ID của sản phẩm trong Menu" },
                                                    Quantity = new { type = "INTEGER", description = "Số lượng món" },
                                                    SizeName = new { type = "STRING", description = "Giá trị bắt buộc là: Small, Medium, hoặc Large" },
                                                    Notes = new { type = "STRING", description = "Ghi chú thêm như ít đá, ít đường" }
                                                },
                                                required = new[] { "ProductId", "Quantity", "SizeName" }
                                            }
                                        }
                                    },
                                    required = new[] { "Items" } // Yêu cầu AI phải trả về property này
                                }
                            }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // 2. GỌI API
            var response = await _httpClient.PostAsync($"{ModelUrl}?key={_apiKey}", jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API Error: {responseString}");
            }

            // 3. PHÂN TÍCH KẾT QUẢ TRẢ VỀ (PARSE JSON)
            var jsonDoc = JsonNode.Parse(responseString);
            var part = jsonDoc?["candidates"]?[0]?["content"]?["parts"]?[0];

            var result = new AIConversationResult();

            // Trường hợp 1: AI yêu cầu gọi hàm AddToCart
            if (part?["functionCall"] != null)
            {
                result.RequiresAction = true;
                result.ActionName = part["functionCall"]?["name"]?.ToString();

                // Trích xuất Arguments (các tham số AI lấy được từ lời nói khách)
                var args = part["functionCall"]?["args"]?.AsObject();
                result.ActionArguments = args?.ToJsonString();

                // Ở bước trước, ta dùng JsonSerializer.Deserialize<List<OrderItemRequest>>(aiResult.ActionArguments) 
                // Nó sẽ ăn khớp hoàn hảo với chuỗi JSON này!
            }
            // Trường hợp 2: AI chỉ trả lời bằng văn bản bình thường
            else if (part?["text"] != null)
            {
                result.RequiresAction = false;
                result.TextResponse = part["text"]?.ToString();
            }

            return result;
        }
    }
}
