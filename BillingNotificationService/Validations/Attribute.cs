using System.ComponentModel.DataAnnotations;

namespace BillingNotificationService.Validations
{
    public class NotEmptyGuidAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is Guid guid && guid == Guid.Empty)
            {
                return new ValidationResult(ErrorMessage ?? "Field cannot be an empty GUID.");
            }
            return ValidationResult.Success;
        }
    }
}
