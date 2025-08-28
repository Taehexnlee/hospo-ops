namespace api.Models;

public class EmployeeDto
{
    public int StoreId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? HireDate { get; set; }  // yyyy-MM-dd 또는 null
    public bool Active { get; set; } = true;
}
