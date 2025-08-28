// Models/Store.cs
namespace api.Models;

public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // 역참조
    public List<EodReport> EodReports { get; set; } = new();
}