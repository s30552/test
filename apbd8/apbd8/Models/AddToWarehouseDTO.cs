using System.ComponentModel.DataAnnotations;

namespace apbd8.Models;

public class AddToWarehouseDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public int WarehouseId { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public int Amount { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }
}
