using System.Data;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementSystem.Pages.Plans;

public class DeleteModel(
    AppDbContext dbContext,
    ILogger<DeleteModel> logger) : PageModel
{
    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public MembershipPlan? Plan { get; private set; }
    public int MemberCount { get; private set; }
    public int PaymentCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadPlanAsync(id))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ConfirmDelete)
        {
            TempData["ErrorMessage"] = "Confirm that you want to delete this membership plan.";
            return RedirectToPage(new { id });
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await LoadPlanAsync(id))
        {
            return NotFound();
        }

        if (MemberCount > 0 || PaymentCount > 0)
        {
            TempData["ErrorMessage"] = "This plan cannot be deleted because it is referenced by members or payments.";
            return RedirectToPage("Index");
        }

        dbContext.MembershipPlans.Remove(Plan!);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "This plan can no longer be deleted because a member or payment now uses it.";
            return RedirectToPage("Index");
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            logger.LogError(exception, "Could not delete membership plan {PlanId}.", id);
            TempData["ErrorMessage"] = "The membership plan could not be deleted. No changes were committed.";
            return RedirectToPage("Index");
        }

        await transaction.CommitAsync();
        TempData["SuccessMessage"] = $"'{Plan!.PlanName}' was deleted successfully.";
        return RedirectToPage("Index");
    }

    private async Task<bool> LoadPlanAsync(int id)
    {
        Plan = await dbContext.MembershipPlans.SingleOrDefaultAsync(plan => plan.PlanId == id);
        if (Plan is null)
        {
            return false;
        }

        MemberCount = await dbContext.Members.CountAsync(member => member.PlanId == id);
        PaymentCount = await dbContext.Payments.CountAsync(payment => payment.PlanId == id);
        return true;
    }

    private static bool IsForeignKeyViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            Message: var message
        } && message.Contains("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase);
}
