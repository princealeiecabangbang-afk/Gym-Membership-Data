using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Members;

public class CreateModel : MemberFormModel
{
    public CreateModel(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task OnGetAsync()
    {
        await LoadPlansAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadPlansAsync();
        NormalizeInput();
        ValidateRequiredText();

        if (!await IsEmailAvailableAsync())
        {
            ModelState.AddModelError("Input.Email", "A member with this email address already exists.");
        }

        if (!await IsSelectedPlanValidAsync(Input.PlanId))
        {
            ModelState.AddModelError("Input.PlanId", "Select an existing active membership plan.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var member = new Member
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            Email = Input.Email,
            Phone = NormalizeOptional(Input.Phone),
            DateOfBirth = Input.DateOfBirth?.Date,
            Gender = NormalizeOptional(Input.Gender),
            JoinDate = PhilippineTime.Now.Date,
            Status = Input.Status,
            PlanId = Input.PlanId
        };
        DbContext.Members.Add(member);

        try
        {
            await DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsEmailConflict(exception))
        {
            ModelState.AddModelError("Input.Email", "A member with this email address already exists.");
            return Page();
        }

        TempData["SuccessMessage"] = $"{member.FirstName} {member.LastName} was registered successfully.";
        return RedirectToPage("Index");
    }

    private async Task<bool> IsEmailAvailableAsync() =>
        string.IsNullOrWhiteSpace(Input.Email) ||
        !await DbContext.Members.AnyAsync(member => member.Email.ToLower() == Input.Email.ToLower());

    private async Task<bool> IsSelectedPlanValidAsync(int planId) =>
        await DbContext.MembershipPlans.AnyAsync(plan => plan.PlanId == planId && plan.IsActive);

    private void NormalizeInput()
    {
        Input.FirstName = Input.FirstName?.Trim() ?? string.Empty;
        Input.LastName = Input.LastName?.Trim() ?? string.Empty;
        Input.Email = Input.Email?.Trim() ?? string.Empty;
        Input.Phone = NormalizeOptional(Input.Phone);
        Input.Gender = NormalizeOptional(Input.Gender);
    }

    private void ValidateRequiredText()
    {
        if (Input.FirstName.Length == 0)
        {
            ModelState.AddModelError("Input.FirstName", "Enter the member's first name.");
        }

        if (Input.LastName.Length == 0)
        {
            ModelState.AddModelError("Input.LastName", "Enter the member's last name.");
        }

        if (Input.Email.Length == 0)
        {
            ModelState.AddModelError("Input.Email", "Enter an email address.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsEmailConflict(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            Message: var message
        } && message.Contains("members.email", StringComparison.OrdinalIgnoreCase);
}

public abstract class MemberFormModel : PageModel
{
    protected readonly AppDbContext DbContext;

    protected MemberFormModel(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    [BindProperty]
    public MemberInput Input { get; set; } = new();

    public IList<MembershipPlan> Plans { get; protected set; } = [];

    protected async Task LoadPlansAsync(int? includePlanId = null)
    {
        var plans = DbContext.MembershipPlans.AsNoTracking()
            .Where(plan => plan.IsActive || (includePlanId.HasValue && plan.PlanId == includePlanId.Value));
        Plans = await plans.OrderBy(plan => plan.PlanName).ToListAsync();
    }
}
