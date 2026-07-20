using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Application.DTOs
{
    public sealed record UpdateProductRequest(
        String Name,
        String Description,
        Decimal Price,
        String Currency);
}
