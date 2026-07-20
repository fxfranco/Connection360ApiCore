namespace Connection360.Application.DTOs
{
    public sealed record ProductDto(
        Guid Id,
        String Name,
        String Description,
        Decimal Price,
        String Currency,
        Int32 StockQuantity,
        String Sku,
        Boolean IsActive,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
