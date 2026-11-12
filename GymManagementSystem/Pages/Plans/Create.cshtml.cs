using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Plans;

public class CreateModel(AppDbContext dbContext) : PageModel
{
    [BindProperty]
    public PlanInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.PlanName = Input.PlanName?.Trim() ?? string.Empty;
        if (Input.PlanName.Length > 0 &&
            await dbContext.MembershipPlans.AnyAsync(plan => plan.PlanName.ToLower() == Input.PlanName.ToLower()))
        {
            ModelState.AddModelError("Input.PlanName", "A plan with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var plan = new MembershipPlan
        {
            PlanName = Input.PlanName,
            DurationDays = Input.DurationDays,
            Price = Input.Price,
            Description = NormalizeDescription(Input.Description),
            IsActive = Input.IsActive
        };
        dbContext.MembershipPlans.Add(plan);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsPlanNameConflict(exception))
        {
            ModelState.AddModelError("Input.PlanName", "A plan with this name already exists.");
            return Page();
        }

        TempData["SuccessMessage"] = $"'{plan.PlanName}' was added successfully.";
        return RedirectToPage("Index");
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static bool IsPlanNameConflict(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            Message: var message
        } && message.Contains("membership_plans.plan_name", StringComparison.OrdinalIgnoreCase);
}
