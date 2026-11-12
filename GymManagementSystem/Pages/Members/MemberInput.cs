using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Pages.Members;

public class MemberInput
{
    [Required(ErrorMessage = "Enter the member's first name.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the member's last name.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }

    [RegularExpression("^(Male|Female|Other)?$", ErrorMessage = "Select a valid gender.")]
    public string? Gender { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a membership plan.")]
    public int PlanId { get; set; }

    [Required]
    [RegularExpression("^(active|inactive|suspended|cancelled)$", ErrorMessage = "Select a valid member status.")]
    public string Status { get; set; } = "active";
}
