using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Payments;

public class IndexModel(AppDbContext dbContext) : PageModel
{
    private static readonly string[] ValidStatuses = ["paid", "pending", "failed", "refunded"];
    private static readonly string[] ValidMethods = ["Cash", "Card", "Bank Transfer", "Mobile Money", "Other"];

    public IList<Payment> Payments { get; private set; } = [];
    public IReadOnlyList<string> Statuses => ValidStatuses;
    public IReadOnlyList<string> Methods => ValidMethods;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? MethodFilter { get; set; }

    public async Task OnGetAsync()
    {
        IQueryable<Payment> payments = dbContext.Payments.AsNoTracking()
            .Include(payment => payment.Member)
            .Include(payment => payment.Plan);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim().ToLower();
            payments = payments.Where(payment =>
                payment.Member.FirstName.ToLower().Contains(search) ||
                payment.Member.LastName.ToLower().Contains(search) ||
                (payment.Member.FirstName + " " + payment.Member.LastName).ToLower().Contains(search) ||
                payment.Member.Email.ToLower().Contains(search) ||
                (payment.ReceiptNumber != null && payment.ReceiptNumber.ToLower().Contains(search)));
        }

        if (DateFilter.HasValue)
        {
            payments = payments.Where(payment => payment.PaymentDate.Date == DateFilter.Value.Date);
        }

        if (ValidStatuses.Contains(StatusFilter))
        {
            payments = payments.Where(payment => payment.Status == StatusFilter);
        }

        if (ValidMethods.Contains(MethodFilter))
        {
            payments = payments.Where(payment => payment.PaymentMethod == MethodFilter);
        }

        Payments = await payments
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.PaymentId)
            .ToListAsync();
    }
}
