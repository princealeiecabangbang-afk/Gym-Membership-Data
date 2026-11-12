using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Reports;

public class IndexModel(AppDbContext dbContext) : PageModel
{
    private const int TransactionListLimit = 100;
    private const int AttendanceMemberLimit = 10;

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public string? DateRangeError { get; private set; }
    public int PaymentCount { get; private set; }
    public decimal CollectedRevenue { get; private set; }
    public decimal PendingAmount { get; private set; }
    public bool ShowingLatestPayments { get; private set; }
    public IList<PaymentBreakdown> PaymentStatuses { get; private set; } = [];
    public IList<PaymentMethodBreakdown> PaymentMethods { get; private set; } = [];
    public IList<Payment> Payments { get; private set; } = [];
    public int AttendanceCount { get; private set; }
    public int CheckedInCount { get; private set; }
    public int CheckedOutCount { get; private set; }
    public IList<AttendanceDateSummary> AttendanceByDate { get; private set; } = [];
    public IList<AttendanceMemberSummary> AttendanceByMember { get; private set; } = [];
    public int TotalMembers { get; private set; }
    public int NewMembers { get; private set; }
    public int ActiveMembers { get; private set; }
    public int InactiveMembers { get; private set; }
    public IList<MemberDateSummary> NewMembersByDate { get; private set; } = [];
    public IList<PlanSummary> PlanDistribution { get; private set; } = [];
    public int TotalPlans { get; private set; }
    public int ActivePlans { get; private set; }
    public int InactivePlans { get; private set; }

    public async Task OnGetAsync()
    {
        if (!ModelState.IsValid)
        {
            DateRangeError = "Enter valid start and end dates.";
            return;
        }

        var today = PhilippineTime.Now.Date;
        if (!StartDate.HasValue && !EndDate.HasValue)
        {
            StartDate = new DateTime(today.Year, today.Month, 1);
            EndDate = today;
        }
        else if (!StartDate.HasValue)
        {
            var endDate = EndDate!.Value.Date;
            StartDate = new DateTime(endDate.Year, endDate.Month, 1);
        }
        else if (!EndDate.HasValue)
        {
            EndDate = today;
        }

        var start = StartDate!.Value.Date;
        var end = EndDate!.Value.Date;
        if (start > end)
        {
            DateRangeError = "The start date must be on or before the end date.";
            return;
        }

        var payments = dbContext.Payments.AsNoTracking()
            .Where(payment => payment.PaymentDate.Date >= start && payment.PaymentDate.Date <= end);

        PaymentStatuses = await payments
            .GroupBy(payment => payment.Status)
            .Select(group => new PaymentBreakdown
            {
                Status = group.Key,
                Count = group.Count(),
                Amount = group.Sum(payment => payment.Amount)
            })
            .OrderBy(summary => summary.Status)
            .ToListAsync();
        PaymentCount = PaymentStatuses.Sum(summary => summary.Count);
        CollectedRevenue = PaymentStatuses
            .FirstOrDefault(summary => summary.Status == "paid")?.Amount ?? 0m;
        PendingAmount = PaymentStatuses
            .FirstOrDefault(summary => summary.Status == "pending")?.Amount ?? 0m;
        PaymentMethods = await payments
            .GroupBy(payment => payment.PaymentMethod)
            .Select(group => new PaymentMethodBreakdown
            {
                Method = group.Key,
                Count = group.Count(),
                Amount = group.Sum(payment => payment.Amount)
            })
            .OrderBy(summary => summary.Method)
            .ToListAsync();

        Payments = await payments
            .Include(payment => payment.Member)
            .Include(payment => payment.Plan)
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.PaymentId)
            .Take(TransactionListLimit)
            .ToListAsync();
        ShowingLatestPayments = PaymentCount > TransactionListLimit;

        var attendance = dbContext.Attendance.AsNoTracking()
            .Where(record => record.AttendanceDate.Date >= start && record.AttendanceDate.Date <= end);
        AttendanceByDate = await attendance
            .GroupBy(record => record.AttendanceDate.Date)
            .Select(group => new AttendanceDateSummary
            {
                Date = group.Key,
                RecordCount = group.Count(),
                CheckedInCount = group.Count(record => record.CheckOutTime == null),
                CheckedOutCount = group.Count(record => record.CheckOutTime != null)
            })
            .OrderBy(summary => summary.Date)
            .ToListAsync();
        AttendanceCount = AttendanceByDate.Sum(summary => summary.RecordCount);
        CheckedInCount = AttendanceByDate.Sum(summary => summary.CheckedInCount);
        CheckedOutCount = AttendanceByDate.Sum(summary => summary.CheckedOutCount);
        AttendanceByMember = await attendance
            .GroupBy(record => new
            {
                record.MemberId,
                record.Member.FirstName,
                record.Member.LastName,
                record.Member.Email
            })
            .Select(group => new AttendanceMemberSummary
            {
                MemberId = group.Key.MemberId,
                FullName = group.Key.FirstName + " " + group.Key.LastName,
                Email = group.Key.Email,
                RecordCount = group.Count()
            })
            .OrderByDescending(summary => summary.RecordCount)
            .ThenBy(summary => summary.FullName)
            .Take(AttendanceMemberLimit)
            .ToListAsync();

        var memberStatusCounts = await dbContext.Members.AsNoTracking()
            .GroupBy(member => member.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();
        TotalMembers = memberStatusCounts.Sum(group => group.Count);
        ActiveMembers = memberStatusCounts.FirstOrDefault(group => group.Status == "active")?.Count ?? 0;
        InactiveMembers = memberStatusCounts.FirstOrDefault(group => group.Status == "inactive")?.Count ?? 0;
        var membersInPeriod = dbContext.Members.AsNoTracking()
            .Where(member => member.JoinDate.Date >= start && member.JoinDate.Date <= end);
        NewMembersByDate = await membersInPeriod
            .GroupBy(member => member.JoinDate.Date)
            .Select(group => new MemberDateSummary
            {
                Date = group.Key,
                MemberCount = group.Count()
            })
            .OrderBy(summary => summary.Date)
            .ToListAsync();
        NewMembers = NewMembersByDate.Sum(summary => summary.MemberCount);

        var planRows = await dbContext.MembershipPlans.AsNoTracking()
            .OrderBy(plan => plan.PlanName)
            .Select(plan => new PlanSummary
            {
                PlanName = plan.PlanName,
                IsActive = plan.IsActive,
                MemberCount = plan.Members.Count()
            })
            .ToListAsync();
        TotalPlans = planRows.Count;
        ActivePlans = planRows.Count(plan => plan.IsActive);
        InactivePlans = planRows.Count - ActivePlans;
        foreach (var plan in planRows)
        {
            plan.Percentage = TotalMembers == 0 ? 0 : plan.MemberCount * 100d / TotalMembers;
        }
        PlanDistribution = planRows;
    }

    public string DisplayStatus(string status) =>
        string.IsNullOrEmpty(status) ? status : char.ToUpperInvariant(status[0]) + status[1..];

    public class PaymentBreakdown
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaymentMethodBreakdown
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class AttendanceDateSummary
    {
        public DateTime Date { get; set; }
        public int RecordCount { get; set; }
        public int CheckedInCount { get; set; }
        public int CheckedOutCount { get; set; }
    }

    public class AttendanceMemberSummary
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RecordCount { get; set; }
    }

    public class MemberDateSummary
    {
        public DateTime Date { get; set; }
        public int MemberCount { get; set; }
    }

    public class PlanSummary
    {
        public string PlanName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int MemberCount { get; set; }
        public double Percentage { get; set; }
    }
}
