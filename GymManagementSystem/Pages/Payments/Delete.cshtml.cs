using System.Data;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementSystem.Pages.Payments;

public class DeleteModel(
    GymManagementSystem.Data.AppDbContext dbContext,
    ILogger<DeleteModel> logger) : PageModel
{
    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public Payment? Payment { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Payment = await LoadPaymentAsync(id);
        return Payment is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ConfirmDelete)
        {
            TempData["ErrorMessage"] = "Confirm that you want to delete this payment.";
            return RedirectToPage(new { id });
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Payment = await LoadPaymentAsync(id);
        if (Payment is null)
        {
            return NotFound();
        }

        dbContext.Payments.Remove(Payment);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            logger.LogError(exception, "Could not delete payment record {PaymentId}.", id);
            TempData["ErrorMessage"] = "The payment could not be deleted. No changes were committed.";
            return RedirectToPage("Index");
        }

        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Payment #{Payment.PaymentId} was deleted.";
        return RedirectToPage("Index");
    }

    private Task<Payment?> LoadPaymentAsync(int id) =>
        dbContext.Payments
            .Include(payment => payment.Member)
            .Include(payment => payment.Plan)
            .SingleOrDefaultAsync(payment => payment.PaymentId == id);
}
