using System.ComponentModel.DataAnnotations;

namespace AnonymousAPI.Validation
{
    public class RequiresParentAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var suggestionIdProperty = validationContext.ObjectType.GetProperty("SuggestionId");
            var problemIdProperty = validationContext.ObjectType.GetProperty("ProblemId");

            var suggestionId = suggestionIdProperty?.GetValue(validationContext.ObjectInstance);
            var problemId = problemIdProperty?.GetValue(validationContext.ObjectInstance);

            if (suggestionId == null && problemId == null)
            {
                return new ValidationResult("Attachment must belong to either a Suggestion or a Problem.");
            }

            return ValidationResult.Success;
        }
    }
}