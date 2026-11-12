using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Pages.Plans;

public class PlanInput
{
    [Required(ErrorMessage = "Enter a plan name.")]
    public string PlanName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Duration must be greater than zero.")]
    public int DurationDays { get; set; }

    [Range(typeof(decimal), "0", "99999999.99", ErrorMessage = "Price cannot be negative.")]
    public decimal Price { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
