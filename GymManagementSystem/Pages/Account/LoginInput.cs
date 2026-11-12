using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Pages.Account;

public sealed class LoginInput
{
    [Required(ErrorMessage = "Enter your username.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
