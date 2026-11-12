using System.Data;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementSystem.Pages.Attendance;

public sealed class DeleteModel(
    AppDbContext dbContext,
    ILogger<DeleteModel> logger) : PageModel
{
    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public GymManagementSystem.Models.Attendance? Record { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Record = await LoadRecordAsync(id);
        return Record is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ConfirmDelete)
        {
            TempData["ErrorMessage"] = "Confirm that you want to delete this attendance record.";
            return RedirectToPage(new { id });
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Record = await LoadRecordAsync(id);
        if (Record is null)
        {
            return NotFound();
        }

        dbContext.Attendance.Remove(Record);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            logger.LogError(exception, "Could not delete attendance record {AttendanceId}.", id);
            TempData["ErrorMessage"] = "The attendance record could not be deleted. No changes were committed.";
            return RedirectToPage("Index");
        }

        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Attendance record #{Record.AttendanceId} was deleted.";
        return RedirectToPage("Index");
    }

    private Task<GymManagementSystem.Models.Attendance?> LoadRecordAsync(int id) =>
        dbContext.Attendance.AsNoTracking()
            .Include(record => record.Member)
            .SingleOrDefaultAsync(record => record.AttendanceId == id);
}
