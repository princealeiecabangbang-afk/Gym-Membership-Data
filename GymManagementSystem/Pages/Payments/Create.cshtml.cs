using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Payments;

public class CreateModel(AppDbContext dbContext) : PaymentFormModel(dbContext)
{
    public async Task OnGetAsync()
    {
        Input.PaymentDate = PhilippineTime.Now.Date;
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        NormalizeInput();
        await LoadOptionsAsync();
        await ValidateReferencesAsync();
        await ValidateReceiptNumberAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var payment = new Payment
        {
            MemberId = Input.MemberId,
            PlanId = Input.PlanId,
            Amount = Input.Amount,
            PaymentDate = Input.PaymentDate!.Value.Date,
            PaymentMethod = Input.PaymentMethod,
            Status = Input.Status,
            ReceiptNumber = Input.ReceiptNumber
        };
        DbContext.Payments.Add(payment);

        try
        {
            await DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsReceiptNumberConflict(exception))
        {
            ModelState.AddModelError("Input.ReceiptNumber", "This receipt number is already in use.");
            return Page();
        }

        TempData["SuccessMessage"] = $"Payment #{payment.PaymentId} was recorded successfully.";
        return RedirectToPage("Index");
    }

    private static bool IsReceiptNumberConflict(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            Message: var message
        } && message.Contains("payments.receipt_number", StringComparison.OrdinalIgnoreCase);
}
