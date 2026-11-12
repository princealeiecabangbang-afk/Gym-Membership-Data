using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Plans;

public class EditModel(AppDbContext dbContext) : PageModel
{
    [BindProperty]
    public PlanInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var plan = await dbContext.MembershipPlans.AsNoTracking().SingleOrDefaultAsync(item => item.PlanId == id);
        if (plan is null)
        {
            return NotFound();
        }

        Input = new PlanInput
        {
            PlanName = plan.PlanName,
            DurationDays = plan.DurationDays,
            Price = plan.Price,
            Description = plan.Description,
            IsActive = plan.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Input.PlanName = Input.PlanName?.Trim() ?? string.Empty;
        var plan = await dbContext.MembershipPlans.SingleOrDefaultAsync(item => item.PlanId == id);
        if (plan is null)
        {
            return NotFound();
        }

        if (Input.PlanName.Length > 0 &&
            await dbContext.MembershipPlans.AnyAsync(item =>
                item.PlanId != id && item.PlanName.ToLower() == Input.PlanName.ToLower()))
        {
            ModelState.AddModelError("Input.PlanName", "A plan with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        plan.PlanName = Input.PlanName;
        plan.DurationDays = Input.DurationDays;
        plan.Price = Input.Price;
        plan.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        plan.IsActive = Input.IsActive;

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsPlanNameConflict(exception))
        {
            ModelState.AddModelError("Input.PlanName", "A plan with this name already exists.");
            return Page();
        }

        TempData["SuccessMessage"] = $"'{plan.PlanName}' was updated successfully.";
        return RedirectToPage("Index");
    }

    private static bool IsPlanNameConflict(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            Message: var message
        } && message.Contains("membership_plans.plan_name", StringComparison.OrdinalIgnoreCase);
}
