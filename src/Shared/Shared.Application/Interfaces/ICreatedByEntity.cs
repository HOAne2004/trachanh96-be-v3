using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Application.Interfaces
{
    // Dành cho các Entity có người tạo (để check rule "Là chủ sở hữu")
    public interface ICreatedByEntity
    {
        Guid CreatedBy { get; }
    }
}
