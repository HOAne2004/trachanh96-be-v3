namespace Shared.Domain.Exceptions;

/// <summary>
/// Exception độc quyền dành riêng cho tầng Domain.
/// Chỉ được ném ra khi vi phạm các Invariants (Quy tắc toàn vẹn nghiệp vụ cốt lõi).
/// </summary>
public class DomainException : Exception
{
    // Có thể mở rộng thêm mã lỗi (ErrorCode) nếu dự án cần đa ngôn ngữ (i18n) sau này
    // public string ErrorCode { get; }

    public DomainException(string message)
        : base(message)
    {
    }

    // Constructor dùng khi có ErrorCode
    // public DomainException(string errorCode, string message) : base(message)
    // {
    //     ErrorCode = errorCode;
    // }
}