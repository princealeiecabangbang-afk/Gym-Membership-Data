using System.Data;
using GymManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.DataManagement;

public sealed class IndexModel(
    AppDbContext dbContext,
    ILogger<IndexModel> logger) : PageModel
{
    private const string ResetConfirmationPhrase = "RESET GYM DATABASE";

    [BindProperty]
    public bool ConfirmAttendanceClear { get; set; }

    [BindProperty]
    public bool ConfirmPaymentClear { get; set; }

    [BindProperty]
    public bool ConfirmMemberClear { get; set; }

    [BindProperty]
    public bool ConfirmPlanClear { get; set; }

    [BindProperty]
    public string ResetPhrase { get; set; } = string.Empty;

    [BindProperty]
    public bool ConfirmReset { get; set; }

    public int AttendanceCount { get; private set; }
    public int PaymentCount { get; private set; }
    public int MemberCount { get; private set; }
    public int PlanCount { get; private set; }
    public int PlanMemberReferenceCount { get; private set; }
    public int PlanPaymentReferenceCount { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadCountsAsync();
    }

    public Task<IActionResult> OnPostClearAttendanceAsync()
    {
        if (!ConfirmAttendanceClear)
        {
            TempData["ErrorMessage"] = "Confirm that you want to permanently clear attendance records.";
            return Task.FromResult<IActionResult>(RedirectToPage());
        }

        return ClearAsync(
            () => dbContext.Attendance.ExecuteDeleteAsync(),
            "attendance record(s)",
            "Attendance records");
    }

    public Task<IActionResult> OnPostClearPaymentsAsync()
    {
        if (!ConfirmPaymentClear)
        {
            TempData["ErrorMessage"] = "Confirm that you want to permanently clear payment records.";
            return Task.FromResult<IActionResult>(RedirectToPage());
        }

        return ClearAsync(
            () => dbContext.Payments.ExecuteDeleteAsync(),
            "payment record(s)",
            "Payment records");
    }

    public async Task<IActionResult> OnPostClearMembersAsync()
    {
        if (!ConfirmMemberClear)
        {
            TempData["ErrorMessage"] = "Confirm that you want to clear members.";
            return RedirectToPage();
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await dbContext.Attendance.AnyAsync() || await dbContext.Payments.AnyAsync())
        {
            TempData["ErrorMessage"] =
                "Members cannot be cleared while attendance or payment history exists. Clear those records first.";
            return RedirectToPage();
        }

        try
        {
            var deletedCount = await dbContext.Members.ExecuteDeleteAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Cleared {deletedCount} member record(s).";
        }
        catch (SqliteException exception)
        {
            logger.LogError(exception, "Could not clear member records.");
            TempData["ErrorMessage"] = "Members could not be cleared. No changes were committed.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearPlansAsync()
    {
        if (!ConfirmPlanClear)
        {
            TempData["ErrorMessage"] = "Confirm that you want to clear membership plans.";
            return RedirectToPage();
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await dbContext.Members.AnyAsync() || await dbContext.Payments.AnyAsync())
        {
            TempData["ErrorMessage"] =
                "Membership plans cannot be cleared while members or payments reference them. Clear those records first.";
            return RedirectToPage();
        }

        try
        {
            var deletedCount = await dbContext.MembershipPlans.ExecuteDeleteAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Cleared {deletedCount} membership plan(s).";
        }
        catch (SqliteException exception)
        {
            logger.LogError(exception, "Could not clear membership plans.");
            TempData["ErrorMessage"] = "Membership plans could not be cleared. No changes were committed.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        if (!string.Equals(ResetPhrase, ResetConfirmationPhrase, StringComparison.Ordinal) || !ConfirmReset)
        {
            TempData["ErrorMessage"] =
                "The full reset requires the exact confirmation phrase and the separate final confirmation.";
            return RedirectToPage();
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var attendanceDeleted = await dbContext.Attendance.ExecuteDeleteAsync();
            var paymentsDeleted = await dbContext.Payments.ExecuteDeleteAsync();
            var membersDeleted = await dbContext.Members.ExecuteDeleteAsync();
            var plansDeleted = await dbContext.MembershipPlans.ExecuteDeleteAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Full reset completed. Removed {attendanceDeleted} attendance record(s), " +
                $"{paymentsDeleted} payment record(s), {membersDeleted} member(s), and {plansDeleted} plan(s).";
        }
        catch (SqliteException exception)
        {
            logger.LogError(exception, "Full system reset failed; the database transaction was rolled back.");
            TempData["ErrorMessage"] = "The reset failed. The database transaction was rolled back and no partial reset was committed.";
        }

        return RedirectToPage();
    }

    private async Task<IActionResult> ClearAsync(
        Func<Task<int>> deleteRecords,
        string recordLabel,
        string sectionName)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var deletedCount = await deleteRecords();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Cleared {deletedCount} {recordLabel}.";
        }
        catch (SqliteException exception)
        {
            logger.LogError(exception, "Could not clear {SectionName}.", sectionName);
            TempData["ErrorMessage"] = $"{sectionName} could not be cleared. No changes were committed.";
        }

        return RedirectToPage();
    }

    private async Task LoadCountsAsync()
    {
        AttendanceCount = await dbContext.Attendance.CountAsync();
        PaymentCount = await dbContext.Payments.CountAsync();
        MemberCount = await dbContext.Members.CountAsync();
        PlanCount = await dbContext.MembershipPlans.CountAsync();
        PlanMemberReferenceCount = await dbContext.Members.CountAsync();
        PlanPaymentReferenceCount = await dbContext.Payments.CountAsync();
    }
}
