using System.Security.Claims;
using GymManagementSystem.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GymManagementSystem.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel(
    IConfiguration configuration,
    PasswordHasher<AdminCredential> passwordHasher,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ConfigurationError { get; private set; }
    public string? LoginError { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        CheckConfiguration();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ModelState.Remove("Input.Password");
            CheckConfiguration();
            return Page();
        }

        var configuredUsername = configuration["AdminAuthentication:Username"];
        var configuredPasswordHash = configuration["AdminAuthentication:PasswordHash"];
        if (string.IsNullOrWhiteSpace(configuredUsername) || string.IsNullOrWhiteSpace(configuredPasswordHash))
        {
            LogMissingConfiguration();
            ConfigurationError = "Administrator login is not configured. Configure the admin username and password hash before signing in.";
            ModelState.Remove("Input.Password");
            return Page();
        }

        var admin = new AdminCredential(configuredUsername);
        PasswordVerificationResult passwordVerification;
        try
        {
            passwordVerification = passwordHasher.VerifyHashedPassword(
                admin,
                configuredPasswordHash,
                Input.Password);
        }
        catch (FormatException)
        {
            logger.LogError("Administrator authentication is unavailable because the configured password hash is invalid.");
            ConfigurationError = "Administrator login configuration is invalid. Check the configured password hash.";
            ModelState.Remove("Input.Password");
            return Page();
        }

        if (!string.Equals(Input.Username.Trim(), configuredUsername, StringComparison.OrdinalIgnoreCase) ||
            passwordVerification == PasswordVerificationResult.Failed)
        {
            LoginError = "Invalid username or password.";
            ModelState.Remove("Input.Password");
            return Page();
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, configuredUsername)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    private void CheckConfiguration()
    {
        if (string.IsNullOrWhiteSpace(configuration["AdminAuthentication:Username"]) ||
            string.IsNullOrWhiteSpace(configuration["AdminAuthentication:PasswordHash"]))
        {
            LogMissingConfiguration();
            ConfigurationError = "Administrator login is not configured. Configure the admin username and password hash before signing in.";
        }
    }

    private void LogMissingConfiguration() =>
        logger.LogError(
            "Administrator authentication is unavailable because AdminAuthentication:Username or AdminAuthentication:PasswordHash is missing.");
}
