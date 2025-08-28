namespace api.Models;

public class EodReportDto
{
    public int StoreId { get; set; }
    public string BizDate { get; set; } = string.Empty;
    public decimal NetSales { get; set; }
    public int Tickets { get; set; }
}
