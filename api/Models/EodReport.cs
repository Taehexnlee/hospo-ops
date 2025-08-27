namespace api.Models;

public class EodReport
{
    public long Id { get; set; }
    public int StoreId { get; set; }
    public DateOnly BizDate { get; set; }
    public decimal NetSales { get; set; }
    public int Tickets { get; set; }
    public DateTime CreatedAt { get; set; }
}
