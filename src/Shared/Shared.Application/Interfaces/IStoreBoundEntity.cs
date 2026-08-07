using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Application.Interfaces
{
    // Dành cho các Entity thuộc về 1 chi nhánh (Order, Inventory...)
    public interface IStoreBoundEntity
    {
        Guid StoreId { get; }
    }
}
