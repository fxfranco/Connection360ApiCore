using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Application.DTOs
{
    public sealed record UpdateStockRequest(Int16 Quantity, Int16 Operation);
}
