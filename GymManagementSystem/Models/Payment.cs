namespace GymManagementSystem.Models;

public class Payment
{
    public int PaymentId { get; set; }
    public int MemberId { get; set; }
    public int PlanId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = "paid";
    public string? ReceiptNumber { get; set; }

    public Member Member { get; set; } = null!;
    public MembershipPlan Plan { get; set; } = null!;
}