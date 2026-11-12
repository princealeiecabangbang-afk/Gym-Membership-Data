using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Payments;

public class DetailsModel(GymManagementSystem.Data.AppDbContext dbContext) : PageModel
{
    public Payment? Payment { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Payment = await dbContext.Payments.AsNoTracking()
            .Include(payment => payment.Member)
            .Include(payment => payment.Plan)
            .SingleOrDefaultAsync(payment => payment.PaymentId == id);
        return Payment is null ? NotFound() : Page();
    }
}
