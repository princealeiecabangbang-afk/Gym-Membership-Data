namespace GymManagementSystem.Models;

public class Attendance
{
    public int AttendanceId { get; set; }
    public int MemberId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public string CheckInTime { get; set; } = string.Empty;
    public string? CheckOutTime { get; set; }
    public string? Notes { get; set; }

    public Member Member { get; set; } = null!;
}