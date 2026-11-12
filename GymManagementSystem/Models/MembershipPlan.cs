namespace GymManagementSystem.Models;

public class MembershipPlan
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Member> Members { get; set; } = new List<Member>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}