namespace api.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string Sku { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Unit { get; set; }
        public decimal? ParLevel { get; set; }
        public bool Active { get; set; } = true;
    }
}
