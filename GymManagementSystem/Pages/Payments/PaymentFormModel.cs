using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Payments;

public abstract class PaymentFormModel : PageModel
{
    protected readonly AppDbContext DbContext;

    protected PaymentFormModel(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    [BindProperty]
    public PaymentInput Input { get; set; } = new();

    public IList<Member> Members { get; protected set; } = [];
    public IList<MembershipPlan> Plans { get; protected set; } = [];

    protected async Task LoadOptionsAsync()
    {
        Members = await DbContext.Members.AsNoTracking()
            .OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .ToListAsync();
        Plans = await DbContext.MembershipPlans.AsNoTracking()
            .OrderBy(plan => plan.PlanName)
            .ToListAsync();
    }

    protected async Task ValidateReferencesAsync()
    {
        if (!await DbContext.Members.AnyAsync(member => member.MemberId == Input.MemberId))
        {
            ModelState.AddModelError("Input.MemberId", "Select an existing member.");
        }

        if (!await DbContext.MembershipPlans.AnyAsync(plan => plan.PlanId == Input.PlanId))
        {
            ModelState.AddModelError("Input.PlanId", "Select an existing membership plan.");
        }
    }

    protected async Task ValidateReceiptNumberAsync(int? currentPaymentId = null)
    {
        Input.ReceiptNumber = NormalizeOptional(Input.ReceiptNumber);
        if (Input.ReceiptNumber is null)
        {
            return;
        }

        var normalizedReceipt = Input.ReceiptNumber.ToLower();
        var duplicate = await DbContext.Payments.AnyAsync(payment =>
            payment.ReceiptNumber != null &&
            payment.ReceiptNumber.ToLower() == normalizedReceipt &&
            (!currentPaymentId.HasValue || payment.PaymentId != currentPaymentId.Value));
        if (duplicate)
        {
            ModelState.AddModelError("Input.ReceiptNumber", "This receipt number is already in use.");
        }
    }

    protected void NormalizeInput()
    {
        Input.PaymentMethod = Input.PaymentMethod?.Trim() ?? string.Empty;
        Input.Status = Input.Status?.Trim() ?? string.Empty;
        Input.ReceiptNumber = NormalizeOptional(Input.ReceiptNumber);
    }

    protected static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
