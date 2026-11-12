using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Plans;

public class DetailsModel(AppDbContext dbContext) : PageModel
{
    public MembershipPlan? Plan { get; private set; }
    public int MemberCount { get; private set; }
    public int PaymentCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Plan = await dbContext.MembershipPlans.AsNoTracking()
            .SingleOrDefaultAsync(plan => plan.PlanId == id);
        if (Plan is null)
        {
            return NotFound();
        }

        MemberCount = await dbContext.Members.CountAsync(member => member.PlanId == id);
        PaymentCount = await dbContext.Payments.CountAsync(payment => payment.PlanId == id);
        return Page();
    }
}
