using Microsoft.Extensions.Options;
using Payments.Application.Interfaces;
using Payments.Domain.Entities;
using Payments.Infrastructure.Database.Configurations;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Payments.Infrastructure.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayConfig _config;

        public VnPayService(IOptions<VnPayConfig> config)
        {
            _config = config.Value;
        }

        private class VnPayCompare : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                var compare = string.Compare(x, y, StringComparison.Ordinal);
                if (compare == 0) return string.Compare(x, y, StringComparison.Ordinal);
                return compare;
            }
        }

        public string CreatePaymentUrl(PaymentTransaction transaction, string ipAddress)
        {
            string tmnCode = _config.TmnCode?.Trim() ?? string.Empty;
            string hashSecret = _config.HashSecret?.Trim() ?? string.Empty;
            string returnUrl = _config.ReturnUrl?.Trim() ?? string.Empty;
            string paymentUrl = _config.VnPayUrl?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            var vnp_Params = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Locale", "vn" },
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", transaction.IdempotencyKey.ToString("N") },
                { "vnp_OrderInfo", $"ThanhToanDonHang_{transaction.OrderCode}" },
                { "vnp_OrderType", "other" },
                { "vnp_Amount", ((long)(transaction.Amount * 100)).ToString() },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_IpAddr", ipAddress },
                { "vnp_CreateDate", transaction.CreatedAt.AddHours(7).ToString("yyyyMMddHHmmss") },
                { "vnp_ExpireDate", transaction.ExpiredAt.AddHours(7).ToString("yyyyMMddHHmmss") }
            };

            var hashData = new StringBuilder();
            var query = new StringBuilder();

            foreach (var kvp in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    // BẮT BUỘC mã hóa bằng Uri.EscapeDataString cho CẢ 2 chuỗi
                    string key = Uri.EscapeDataString(kvp.Key);
                    string value = Uri.EscapeDataString(kvp.Value);

                    hashData.Append(key).Append('=').Append(value).Append('&');
                    query.Append(key).Append('=').Append(value).Append('&');
                }
            }

            if (hashData.Length > 0) hashData.Length--;
            if (query.Length > 0) query.Length--;

            // ============================================================
            // DÒNG IN LOG ĐỂ BẮT BỆNH - HÃY NHÌN VÀO CỬA SỔ CONSOLE / OUTPUT
            // ============================================================
            Console.WriteLine("\n============= VNPAY DEBUG =============");
            Console.WriteLine($"1. HashSecret đang dùng: '{hashSecret}'");
            Console.WriteLine($"2. Chuỗi HashData (để băm): '{hashData.ToString()}'");
            Console.WriteLine($"3. Chuỗi QueryString (gửi đi): '{query.ToString()}'");
            Console.WriteLine("=======================================\n");

            var secureHash = HmacSha512(hashSecret, hashData.ToString());

            return $"{paymentUrl}?{query}&vnp_SecureHashType=HmacSHA512&vnp_SecureHash={secureHash}";
        }

        public bool IsValidSignature(IDictionary<string, string> responseData)
        {
            string hashSecret = _config.HashSecret?.Trim() ?? string.Empty;
            var vnp_Params = new SortedList<string, string>(new VnPayCompare());
            string inputHash = string.Empty;

            foreach (var kvp in responseData)
            {
                if (kvp.Key.StartsWith("vnp_") && !string.IsNullOrEmpty(kvp.Value))
                {
                    if (kvp.Key == "vnp_SecureHash")
                    {
                        inputHash = kvp.Value;
                    }
                    else if (kvp.Key != "vnp_SecureHashType")
                    {
                        vnp_Params.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            var hashData = new StringBuilder();
            foreach (var kvp in vnp_Params)
            {
                string value = HttpUtility.UrlEncode(kvp.Value);
                hashData.Append(kvp.Key).Append('=').Append(value).Append('&');
            }

            if (hashData.Length > 0) hashData.Length--;

            var myHash = HmacSha512(hashSecret, hashData.ToString());

            return myHash.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}