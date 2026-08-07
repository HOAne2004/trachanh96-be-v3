using AI.Application.DTOs;
using AI.Application.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace AI.Infrastructure.Services
{
    public class GeminiService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string ModelUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["AI:GeminiApiKey"] ?? throw new ArgumentNullException("Thiếu AI:GeminiApiKey");
        }

        public async Task<AIConversationResult> SendMessageAsync(
            string sessionId,
            List<MessageDto> history,
            string systemContext,
            CancellationToken cancellationToken = default)
        {
            var formattedContents = history.Select(msg => new
            {
                role = msg.Role.ToLower() == "user" ? "user" : "model",
                parts = new[] { new { text = msg.Content } }
            }).ToArray();

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = $"Bạn là nhân viên chốt đơn của Trà Chanh 1996. Giọng điệu thân thiện, nhiệt tình. Đây là Menu hiện tại (dạng JSON): {systemContext}. Khách có thể đặt đồ uống, đặt bàn, hoặc yêu cầu thanh toán. Hãy sử dụng công cụ phù hợp khi nhận diện được nhu cầu." } }
                },
                contents = formattedContents,
                tools = new[]
                {
                    new
                    {
                        // Thêm các Tools (Order, Đặt bàn, Checkout)
                        function_declarations = new object[]
                        {
                            // 1. Tool Đặt món
                            new
                            {
                                name = "AddToCart",
                                description = "Thêm đồ uống vào giỏ hàng. Gọi hàm này ngay khi khách nói muốn mua/đặt món.",
                                parameters = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        Items = new
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
                                    required = new[] { "Items" }
                                }
                            },
                            // 2. Tool Đặt bàn
                            new
                            {
                                name = "ReserveTable",
                                description = "Kiểm tra bàn trống và đặt bàn khi khách có nhu cầu muốn ngồi lại quán.",
                                parameters = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        Time = new { type = "STRING", description = "Thời gian khách muốn đặt (vd: 20:00 tối nay)" },
                                        Capacity = new { type = "INTEGER", description = "Số lượng người" },
                                        Location = new { type = "STRING", description = "Yêu cầu khu vực (ví dụ: ban công, trong nhà)" }
                                    },
                                    required = new[] { "Time", "Capacity" }
                                }
                            },
                            // 3. Tool Thanh toán / Chốt đơn
                            new
                            {
                                name = "ShowCheckoutSummary",
                                description = "Gọi hàm này khi khách nói muốn thanh toán, chốt đơn, hoặc xem lại tổng tiền.",
                                // API của Google bắt buộc phải có object parameters dù hàm không cần tham số
                                parameters = new
                                {
                                    type = "OBJECT",
                                    properties = new { dummy = new { type = "STRING", description = "Bỏ qua tham số này" } }
                                }
                            }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // Truyền cancellationToken vào HTTP Client
            var response = await _httpClient.PostAsync($"{ModelUrl}?key={_apiKey}", jsonContent, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API Error: {responseString}");
            }

            var jsonDoc = JsonNode.Parse(responseString);
            var result = new AIConversationResult();

            // CẬP NHẬT: Trích xuất Token Usage
            if (jsonDoc?["usageMetadata"] != null)
            {
                result.PromptTokens = jsonDoc["usageMetadata"]?["promptTokenCount"]?.GetValue<int>() ?? 0;
                result.CompletionTokens = jsonDoc["usageMetadata"]?["candidatesTokenCount"]?.GetValue<int>() ?? 0;
            }

            var candidate = jsonDoc?["candidates"]?[0];

            // CẬP NHẬT: Kiểm tra nếu bị bộ lọc an toàn của Google chặn
            var finishReason = candidate?["finishReason"]?.ToString();
            if (finishReason == "SAFETY" || finishReason == "RECITATION")
            {
                result.IsSuccess = false;
                result.BlockReason = $"Phản hồi bị từ chối do chính sách an toàn ({finishReason}).";
                return result; // Trả về luôn, không parse tiếp
            }

            var part = candidate?["content"]?["parts"]?[0];

            // Phân tích kết quả
            if (part?["functionCall"] != null)
            {
                result.RequiresAction = true;
                result.ActionName = part["functionCall"]?["name"]?.ToString();
                var args = part["functionCall"]?["args"]?.AsObject();
                result.ActionArguments = args?.ToJsonString();
            }
            else if (part?["text"] != null)
            {
                result.RequiresAction = false;
                result.TextResponse = part["text"]?.ToString();
            }

            return result;
        }
    }
}