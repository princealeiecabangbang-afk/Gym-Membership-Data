using System.Data;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementSystem.Pages.Members;

public class DeleteModel(
    AppDbContext dbContext,
    ILogger<DeleteModel> logger) : PageModel
{
    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public Member? Member { get; private set; }
    public int AttendanceCount { get; private set; }
    public int PaymentCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadMemberAsync(id))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ConfirmDelete)
        {
            TempData["ErrorMessage"] = "Confirm that you want to delete this member.";
            return RedirectToPage(new { id });
        }

        await using var transaction =
            await DbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await LoadMemberAsync(id))
        {
            return NotFound();
        }

        if (AttendanceCount > 0 || PaymentCount > 0)
        {
            TempData["ErrorMessage"] = "This member cannot be deleted because attendance or payment records reference them.";
            return RedirectToPage("Index");
        }

        DbContext.Members.Remove(Member!);
        try
        {
            await DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "This member can no longer be deleted because an attendance or payment record was added.";
            return RedirectToPage("Index");
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            logger.LogError(exception, "Could not delete member record {MemberId}.", id);
            TempData["ErrorMessage"] = "The member could not be deleted. No changes were committed.";
            return RedirectToPage("Index");
        }

        await transaction.CommitAsync();
        TempData["SuccessMessage"] = $"{Member!.FirstName} {Member.LastName} was deleted successfully.";
        return RedirectToPage("Index");
    }

    private AppDbContext DbContext { get; } = dbContext;

    private async Task<bool> LoadMemberAsync(int id)
    {
        Member = await DbContext.Members.Include(member => member.Plan)
            .SingleOrDefaultAsync(member => member.MemberId == id);
        if (Member is null)
        {
            return false;
        }

        AttendanceCount = await DbContext.Attendance.CountAsync(record => record.MemberId == id);
        PaymentCount = await DbContext.Payments.CountAsync(payment => payment.MemberId == id);
        return true;
    }

    private static bool IsForeignKeyViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            Message: var message
        } && message.Contains("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase);
}
