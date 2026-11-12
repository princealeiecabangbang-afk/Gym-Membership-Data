using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Members;

public class IndexModel(AppDbContext dbContext) : PageModel
{
    private static readonly string[] ValidStatuses = ["active", "inactive", "suspended", "cancelled"];

    public IList<Member> Members { get; private set; } = [];
    public IList<MembershipPlan> Plans { get; private set; } = [];
    public IReadOnlyList<string> Statuses => ValidStatuses;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PlanFilter { get; set; }

    public async Task OnGetAsync()
    {
        Plans = await dbContext.MembershipPlans.AsNoTracking()
            .OrderBy(plan => plan.PlanName)
            .ToListAsync();

        IQueryable<Member> members = dbContext.Members.AsNoTracking().Include(member => member.Plan);
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim().ToLower();
            members = members.Where(member =>
                member.FirstName.ToLower().Contains(search) ||
                member.LastName.ToLower().Contains(search) ||
                (member.FirstName + " " + member.LastName).ToLower().Contains(search) ||
                member.Email.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter) && ValidStatuses.Contains(StatusFilter))
        {
            members = members.Where(member => member.Status == StatusFilter);
        }

        if (PlanFilter.HasValue)
        {
            members = members.Where(member => member.PlanId == PlanFilter.Value);
        }

        Members = await members.OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var member = await dbContext.Members.FindAsync(id);
        if (member is null)
        {
            TempData["ErrorMessage"] = "That member could not be found.";
            return RedirectToPage();
        }

        member.Status = member.Status == "active" ? "inactive" : "active";
        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{member.FirstName} {member.LastName} is now {member.Status}.";

        return RedirectToPage(new { SearchTerm, StatusFilter, PlanFilter });
    }
}
