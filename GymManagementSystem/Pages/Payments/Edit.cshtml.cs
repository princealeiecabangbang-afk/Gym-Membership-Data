using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Payments;

public class EditModel(GymManagementSystem.Data.AppDbContext dbContext) : PaymentFormModel(dbContext)
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var payment = await DbContext.Payments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.PaymentId == id);
        if (payment is null)
        {
            return NotFound();
        }

        Input = new PaymentInput
        {
            MemberId = payment.MemberId,
            PlanId = payment.PlanId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            ReceiptNumber = payment.ReceiptNumber
        };
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var payment = await DbContext.Payments.SingleOrDefaultAsync(item => item.PaymentId == id);
        if (payment is null)
        {
            return NotFound();
        }

        NormalizeInput();
        await LoadOptionsAsync();
        await ValidateReferencesAsync();
        await ValidateReceiptNumberAsync(id);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        payment.MemberId = Input.MemberId;
        payment.PlanId = Input.PlanId;
        payment.Amount = Input.Amount;
        payment.PaymentDate = Input.PaymentDate!.Value.Date;
        payment.PaymentMethod = Input.PaymentMethod;
        payment.Status = Input.Status;
        payment.ReceiptNumber = Input.ReceiptNumber;

        try
        {
            await DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsReceiptNumberConflict(exception))
        {
            ModelState.AddModelError("Input.ReceiptNumber", "This receipt number is already in use.");
            return Page();
        }

        TempData["SuccessMessage"] = $"Payment #{payment.PaymentId} was updated successfully.";
        return RedirectToPage("Index");
    }

    private static bool IsReceiptNumberConflict(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            Message: var message
        } && message.Contains("payments.receipt_number", StringComparison.OrdinalIgnoreCase);
}
