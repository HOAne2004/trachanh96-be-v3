using System;
using System.Collections.Generic;
using System.Text;

namespace AI.Domain.Enums
{
    public enum MessageStatusEnum
    {
        Processing = 1, // Đang gửi/chờ AI phản hồi
        Success = 2,    // AI trả lời thành công
        Failed = 3,     // Lỗi API, Timeout
        Blocked = 4     // Bị chặn do vi phạm chính sách an toàn (Safety Filter)
    }
}
