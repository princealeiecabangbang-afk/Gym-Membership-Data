using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Members;

public class DetailsModel(AppDbContext dbContext) : PageModel
{
    public Member? Member { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Member = await dbContext.Members.AsNoTracking()
            .Include(member => member.Plan)
            .SingleOrDefaultAsync(member => member.MemberId == id);
        return Member is null ? NotFound() : Page();
    }
}
