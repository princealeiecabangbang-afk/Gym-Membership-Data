using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Pages.Payments;

public class PaymentInput : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a member.")]
    public int MemberId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a membership plan.")]
    public int PlanId { get; set; }

    [Range(typeof(decimal), "0", "99999999.99", ErrorMessage = "Enter an amount from 0 to 99,999,999.99.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Enter the payment date.")]
    public DateTime? PaymentDate { get; set; }

    [Required(ErrorMessage = "Select a payment method.")]
    [RegularExpression("^(Cash|Card|Bank Transfer|Mobile Money|Other)$", ErrorMessage = "Select a valid payment method.")]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a payment status.")]
    [RegularExpression("^(paid|pending|failed|refunded)$", ErrorMessage = "Select a valid payment status.")]
    public string Status { get; set; } = "paid";

    public string? ReceiptNumber { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Round(Amount, 2) != Amount)
        {
            yield return new ValidationResult(
                "Amount can have up to two decimal places.",
                [nameof(Amount)]);
        }
    }
}
