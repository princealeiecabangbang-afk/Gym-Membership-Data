using System.Data;
using System.Globalization;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AttendanceRecord = GymManagementSystem.Models.Attendance;

namespace GymManagementSystem.Pages.Attendance;

public class IndexModel(AppDbContext dbContext) : PageModel
{
    private const string CheckedIn = "checked-in";
    private const string CheckedOut = "checked-out";

    public IList<AttendanceRecord> Records { get; private set; } = [];
    public IList<Member> EligibleMembers { get; private set; } = [];
    public IList<AttendanceRecord> OpenRecords { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty]
    public int CheckInMemberId { get; set; }

    [BindProperty]
    public int CheckOutAttendanceId { get; set; }

    public async Task OnGetAsync()
    {
        await LoadPageDataAsync();
    }

    public async Task<IActionResult> OnPostCheckInAsync()
    {
        if (CheckInMemberId <= 0)
        {
            TempData["ErrorMessage"] = "Select an active member to check in.";
            return RedirectToPage();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var member = await dbContext.Members.SingleOrDefaultAsync(item => item.MemberId == CheckInMemberId);
        if (member is null)
        {
            TempData["ErrorMessage"] = "The selected member does not exist.";
            return RedirectToPage();
        }

        if (member.Status != "active")
        {
            TempData["ErrorMessage"] = "Only active members can start a new check-in.";
            return RedirectToPage();
        }

        var hasOpenRecord = await dbContext.Attendance.AnyAsync(record =>
            record.MemberId == member.MemberId && record.CheckOutTime == null);
        if (hasOpenRecord)
        {
            TempData["ErrorMessage"] = $"{member.FirstName} {member.LastName} is already checked in.";
            return RedirectToPage();
        }

        var now = PhilippineTime.Now;
        dbContext.Attendance.Add(new AttendanceRecord
        {
            MemberId = member.MemberId,
            AttendanceDate = now.Date,
            CheckInTime = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
        });
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"{member.FirstName} {member.LastName} checked in at {now:HH:mm}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCheckOutAsync()
    {
        if (CheckOutAttendanceId <= 0)
        {
            TempData["ErrorMessage"] = "Select a member with an open check-in to check out.";
            return RedirectToPage();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var record = await dbContext.Attendance
            .Include(item => item.Member)
            .SingleOrDefaultAsync(item =>
                item.AttendanceId == CheckOutAttendanceId && item.CheckOutTime == null);
        if (record is null)
        {
            TempData["ErrorMessage"] = "No open check-in was found. The member may already be checked out.";
            return RedirectToPage();
        }

        if (!TimeSpan.TryParse(record.CheckInTime, CultureInfo.InvariantCulture, out var checkInTime))
        {
            TempData["ErrorMessage"] = "This attendance record has an invalid check-in time and cannot be checked out.";
            return RedirectToPage();
        }

        var now = PhilippineTime.Now;
        var checkInDateTime = record.AttendanceDate.Date.Add(checkInTime);
        if (checkInDateTime > now)
        {
            TempData["ErrorMessage"] = "Check-out cannot be earlier than the recorded check-in time.";
            return RedirectToPage();
        }

        record.CheckOutTime = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"{record.Member.FirstName} {record.Member.LastName} checked out at {now:HH:mm}.";
        return RedirectToPage();
    }

    private async Task LoadPageDataAsync()
    {
        var membersWithOpenRecords = dbContext.Attendance
            .Where(record => record.CheckOutTime == null)
            .Select(record => record.MemberId);
        EligibleMembers = await dbContext.Members.AsNoTracking()
            .Where(member => member.Status == "active" && !membersWithOpenRecords.Contains(member.MemberId))
            .OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .ToListAsync();

        OpenRecords = await dbContext.Attendance.AsNoTracking()
            .Include(record => record.Member)
            .Where(record => record.CheckOutTime == null)
            .OrderBy(record => record.AttendanceDate)
            .ThenBy(record => record.CheckInTime)
            .ToListAsync();

        IQueryable<AttendanceRecord> records = dbContext.Attendance.AsNoTracking()
            .Include(record => record.Member);
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim().ToLower();
            records = records.Where(record =>
                record.Member.FirstName.ToLower().Contains(search) ||
                record.Member.LastName.ToLower().Contains(search) ||
                (record.Member.FirstName + " " + record.Member.LastName).ToLower().Contains(search) ||
                record.Member.Email.ToLower().Contains(search));
        }

        if (DateFilter.HasValue)
        {
            records = records.Where(record => record.AttendanceDate.Date == DateFilter.Value.Date);
        }

        if (StatusFilter == CheckedIn)
        {
            records = records.Where(record => record.CheckOutTime == null);
        }
        else if (StatusFilter == CheckedOut)
        {
            records = records.Where(record => record.CheckOutTime != null);
        }

        Records = await records
            .OrderByDescending(record => record.AttendanceDate)
            .ThenByDescending(record => record.CheckInTime)
            .ToListAsync();
    }
}
