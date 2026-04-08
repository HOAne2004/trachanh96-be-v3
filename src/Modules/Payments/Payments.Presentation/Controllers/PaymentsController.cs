using MediatR;
using Payments.Application.Features.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Payments.Presentation.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        // Chỉ tiêm ISender của MediatR vào đây, không tiêm DbContext hay Service nào khác!
        private readonly ISender _sender;

        public PaymentsController(ISender sender)
        {
            _sender = sender;
        }

        // DTO (Data Transfer Object) để hứng dữ liệu từ Body của request
        public record CreatePaymentRequest(Guid OrderId, string OrderCode, decimal Amount);

        /// <summary>
        /// API tạo link thanh toán VNPay
        /// </summary>
        [HttpPost("vnpay")]
        public async Task<IActionResult> CreateVnPayLink([FromBody] CreatePaymentRequest request)
        {
            // 1. Lấy IP Address của người dùng (VNPay bắt buộc phải có để chống gian lận)
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // 2. Lắp ráp dữ liệu thành cái Command mà chúng ta đã thiết kế
            var command = new CreatePaymentLinkCommand(
                request.OrderId,
                request.OrderCode,
                request.Amount,
                ipAddress
            );

            // 3. Quăng cho Handler xử lý
            var result = await _sender.Send(command);

            // 4. Kiểm tra Result (Cái mà bạn đã tinh ý phát hiện ra ở bước trước)
            if (!result.IsSuccess)
            {
                // Trả về lỗi 400 nếu Validation thất bại hoặc lỗi nghiệp vụ
                return BadRequest(new
                {
                    success = false,
                    message = result.Error
                });
            }

            // Trả về 200 OK kèm theo URL thanh toán
            return Ok(new
            {
                success = true,
                message = "Tạo link thanh toán VNPay thành công",
                paymentUrl = result.Value
            });
        }

        /// <summary>
        /// API đón kết quả trả về từ VNPay (Return URL)
        /// </summary>
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            // 1. Gom toàn bộ Query String thành Dictionary
            var responseData = new Dictionary<string, string>();
            foreach (var (key, value) in Request.Query)
            {
                if (!string.IsNullOrEmpty(key) && value.Count > 0)
                {
                    responseData.Add(key, value.ToString());
                }
            }

            // 2. Tạo Command và quăng cho MediatR
            var command = new ProcessVnPayCallbackCommand(responseData);
            var result = await _sender.Send(command);

            // 3. Trả về kết quả cho Frontend (hoặc Redirect thẳng về trang web của khách hàng)
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = false, message = result.Error });
            }

            return Ok(new { success = true, message = result.Value });
        }

        /// <summary>
        /// API IPN (Webhook) - Server VNPay sẽ tự động gọi ngầm vào đây
        /// </summary>
        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VnPayIpn()
        {
            try
            {
                // 1. Gom toàn bộ Query String thành Dictionary
                var responseData = new Dictionary<string, string>();
                foreach (var (key, value) in Request.Query)
                {
                    if (!string.IsNullOrEmpty(key) && value.Count > 0)
                    {
                        responseData.Add(key, value.ToString());
                    }
                }

                // 2. Tái sử dụng lại Command lúc nãy!
                var command = new ProcessVnPayCallbackCommand(responseData);
                var result = await _sender.Send(command);

                // 3. Trả kết quả ĐÚNG CHUẨN mà VNPay yêu cầu
                if (result.IsSuccess)
                {
                    // VNPay yêu cầu mã "00" để xác nhận server bạn đã ghi nhận thành công
                    return Ok(new { RspCode = "00", Message = "Confirm Success" });
                }

                // Nếu lỗi (Sai chữ ký, không tìm thấy đơn...)
                return Ok(new { RspCode = "97", Message = "Invalid Checksum or Error" });
            }
            catch (Exception)
            {
                // Bắt lỗi Exception không mong muốn
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
        }
    }
}
