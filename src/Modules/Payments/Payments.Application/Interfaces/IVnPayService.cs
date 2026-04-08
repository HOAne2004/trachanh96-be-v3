using Payments.Domain.Entities;

namespace Payments.Application.Interfaces
{
    public interface IVnPayService
    {
        //Tạo link  thanh toán VNPay
        string CreatePaymentUrl(PaymentTransaction transaction, string ipAddress);
        // Kiểm tra tính hợp lệ của chữ ký trả về từ VNPay để đảm bảo dữ liệu không bị giả mạo
        bool IsValidSignature(IDictionary<string, string> responseData);
    }
}
