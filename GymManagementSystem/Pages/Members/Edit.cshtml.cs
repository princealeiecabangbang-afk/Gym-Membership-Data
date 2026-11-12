using GymManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Members;

public class EditModel(AppDbContext dbContext) : MemberFormModel(dbContext)
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var member = await DbContext.Members.AsNoTracking()
            .SingleOrDefaultAsync(item => item.MemberId == id);
        if (member is null)
        {
            return NotFound();
        }

        Input = new MemberInput
        {
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Phone = member.Phone,
            DateOfBirth = member.DateOfBirth,
            Gender = member.Gender,
            PlanId = member.PlanId,
            Status = member.Status
        };
        await LoadPlansAsync(member.PlanId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var member = await DbContext.Members.SingleOrDefaultAsync(item => item.MemberId == id);
        if (member is null)
        {
            return NotFound();
        }

        await LoadPlansAsync(member.PlanId);
        NormalizeInput();
        ValidateRequiredText();

        if (!string.IsNullOrWhiteSpace(Input.Email) &&
            await DbContext.Members.AnyAsync(item =>
                item.MemberId != id && item.Email.ToLower() == Input.Email.ToLower()))
        {
            ModelState.AddModelError("Input.Email", "A member with this email address already exists.");
        }

        if (!await DbContext.MembershipPlans.AnyAsync(plan =>
                plan.PlanId == Input.PlanId &&
                (plan.IsActive || plan.PlanId == member.PlanId)))
        {
            ModelState.AddModelError("Input.PlanId", "Select an existing active membership plan.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        member.FirstName = Input.FirstName;
        member.LastName = Input.LastName;
        member.Email = Input.Email;
        member.Phone = NormalizeOptional(Input.Phone);
        member.DateOfBirth = Input.DateOfBirth?.Date;
        member.Gender = NormalizeOptional(Input.Gender);
        member.PlanId = Input.PlanId;
        member.Status = Input.Status;

        try
        {
            await DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsEmailConflict(exception))
        {
            ModelState.AddModelError("Input.Email", "A member with this email address already exists.");
            return Page();
        }

        TempData["SuccessMessage"] = $"{member.FirstName} {member.LastName} was updated successfully.";
        return RedirectToPage("Index");
    }

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
