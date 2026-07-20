namespace Connection360.Application.DTOs
{
    public sealed record CreateProductRequest(
        String Name,
        String Description,
        Decimal Price,
        String Currency,
        Int16 StockQuantity,
        String Sku);
}
