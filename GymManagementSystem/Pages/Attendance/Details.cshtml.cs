using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Pages.Attendance;

public class DetailsModel(AppDbContext dbContext) : PageModel
{
    public GymManagementSystem.Models.Attendance? Record { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Record = await dbContext.Attendance.AsNoTracking()
            .Include(record => record.Member)
            .SingleOrDefaultAsync(record => record.AttendanceId == id);
        return Record is null ? NotFound() : Page();
    }
}
