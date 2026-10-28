using System.ComponentModel.DataAnnotations;

namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class DateTimeGreaterThanNow : ValidationAttribute
{
    public override string FormatErrorMessage(string name)
    {
        return "Date value should be a future date";
    }

    protected override ValidationResult? IsValid(object? objValue,
        ValidationContext validationContext)
    {
        var dateValue = objValue as DateTime? ?? new DateTime();
        
        return dateValue > DateTime.Now ? ValidationResult.Success : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }
}