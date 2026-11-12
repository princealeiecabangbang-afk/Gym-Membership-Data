using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages;

public class IndexModel(AppDbContext dbContext) : PageModel
{
    public DateTime Today { get; private set; }
    public int TotalMembers { get; private set; }
    public int ActiveMembers { get; private set; }
    public int InactiveMembers { get; private set; }
    public int TotalPlans { get; private set; }
    public int ActivePlans { get; private set; }
    public int InactivePlans { get; private set; }
    public int TodayAttendanceCount { get; private set; }
    public int CurrentlyCheckedIn { get; private set; }
    public int TodayCheckedOut { get; private set; }
    public decimal MonthlyCollectedRevenue { get; private set; }
    public int MonthlyPaymentCount { get; private set; }
    public IList<Member> RecentMembers { get; private set; } = [];
    public IList<Payment> RecentPayments { get; private set; } = [];
    public IList<GymManagementSystem.Models.Attendance> RecentAttendance { get; private set; } = [];
    public IList<PlanSummary> PlanDistribution { get; private set; } = [];
    public IList<PaymentStatusSummary> MonthlyPaymentStatuses { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Today = PhilippineTime.Now.Date;
        var monthStart = new DateTime(Today.Year, Today.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var memberStatusCounts = await dbContext.Members.AsNoTracking()
            .GroupBy(member => member.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();
        TotalMembers = memberStatusCounts.Sum(group => group.Count);
        ActiveMembers = memberStatusCounts.FirstOrDefault(group => group.Status == "active")?.Count ?? 0;
        InactiveMembers = memberStatusCounts.FirstOrDefault(group => group.Status == "inactive")?.Count ?? 0;

        var attendanceCounts = await dbContext.Attendance.AsNoTracking()
            .GroupBy(record => record.CheckOutTime == null)
            .Select(group => new
            {
                IsOpen = group.Key,
                Count = group.Count(),
                TodayCount = group.Count(record => record.AttendanceDate.Date == Today)
            })
            .ToListAsync();
        TodayAttendanceCount = attendanceCounts.Sum(group => group.TodayCount);
        CurrentlyCheckedIn = attendanceCounts.FirstOrDefault(group => group.IsOpen)?.Count ?? 0;
        TodayCheckedOut = attendanceCounts.FirstOrDefault(group => !group.IsOpen)?.TodayCount ?? 0;

        var monthlyPayments = dbContext.Payments.AsNoTracking()
            .Where(payment => payment.PaymentDate.Date >= monthStart &&
                              payment.PaymentDate.Date < nextMonthStart);
        MonthlyPaymentStatuses = await monthlyPayments
            .GroupBy(payment => payment.Status)
            .Select(group => new PaymentStatusSummary
            {
                Status = group.Key,
                Count = group.Count(),
                Amount = group.Sum(payment => payment.Amount)
            })
            .OrderBy(summary => summary.Status)
            .ToListAsync();
        MonthlyPaymentCount = MonthlyPaymentStatuses.Sum(summary => summary.Count);
        MonthlyCollectedRevenue = MonthlyPaymentStatuses
            .FirstOrDefault(summary => summary.Status == "paid")?.Amount ?? 0m;

        RecentMembers = await dbContext.Members.AsNoTracking()
            .Include(member => member.Plan)
            .OrderByDescending(member => member.JoinDate)
            .ThenByDescending(member => member.MemberId)
            .Take(5)
            .ToListAsync();

        RecentPayments = await dbContext.Payments.AsNoTracking()
            .Include(payment => payment.Member)
            .Include(payment => payment.Plan)
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.PaymentId)
            .Take(5)
            .ToListAsync();

        RecentAttendance = await dbContext.Attendance.AsNoTracking()
            .Include(record => record.Member)
            .OrderByDescending(record => record.AttendanceDate)
            .ThenByDescending(record => record.CheckInTime)
            .ThenByDescending(record => record.AttendanceId)
            .Take(5)
            .ToListAsync();

        PlanDistribution = await dbContext.MembershipPlans.AsNoTracking()
            .OrderBy(plan => plan.PlanName)
            .Select(plan => new PlanSummary
            {
                PlanName = plan.PlanName,
                MemberCount = plan.Members.Count(),
                IsActive = plan.IsActive
            })
            .ToListAsync();

        TotalPlans = PlanDistribution.Count;
        ActivePlans = PlanDistribution.Count(plan => plan.IsActive);
        InactivePlans = TotalPlans - ActivePlans;
        foreach (var plan in PlanDistribution)
        {
            plan.Percentage = TotalMembers == 0 ? 0 : plan.MemberCount * 100d / TotalMembers;
        }
    }

    public string DisplayStatus(string status) =>
        string.IsNullOrEmpty(status) ? status : char.ToUpperInvariant(status[0]) + status[1..];

    public class PlanSummary
    {
        public string PlanName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public double Percentage { get; set; }
        public bool IsActive { get; set; }
    }

    public class PaymentStatusSummary
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}
