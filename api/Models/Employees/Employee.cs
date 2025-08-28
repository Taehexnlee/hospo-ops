using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace api.Models;

[Index(nameof(StoreId), nameof(FullName), IsUnique = true)]
public class Employee
{
    public int Id { get; set; }

    [Required]
    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    public DateOnly? HireDate { get; set; }

    public bool Active { get; set; } = true;
}
