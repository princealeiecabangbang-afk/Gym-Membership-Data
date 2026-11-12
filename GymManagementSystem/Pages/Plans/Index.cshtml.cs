using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Plans;

public class IndexModel(AppDbContext dbContext) : PageModel
{
    public IList<MembershipPlan> Plans { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var plans = dbContext.MembershipPlans.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim().ToLower();
            plans = plans.Where(plan => plan.PlanName.ToLower().Contains(search));
        }

        Plans = await plans.OrderBy(plan => plan.PlanName).ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var plan = await dbContext.MembershipPlans.FindAsync(id);
        if (plan is null)
        {
            TempData["ErrorMessage"] = "That membership plan could not be found.";
            return RedirectToPage();
        }

        plan.IsActive = !plan.IsActive;
        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = $"'{plan.PlanName}' is now {(plan.IsActive ? "active" : "inactive")}.";

        return RedirectToPage(new { SearchTerm });
    }
}
