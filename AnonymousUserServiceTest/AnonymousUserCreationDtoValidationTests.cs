using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using Xunit;

namespace AnonymousUserService.Tests.Validation
{
    public class AnonymousUserCreationDtoValidationTests
    {
        private static IList<ValidationResult> Validate(object model)
        {
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        private static bool HasErrorFor(IEnumerable<ValidationResult> results, string member) =>
            results.Any(r => r.MemberNames.Contains(member));

        [Theory]
        [InlineData("aaaaaaaa")]   // no upper, digit, special
        [InlineData("Aaaaaaaa")]   // no digit, special
        [InlineData("Aaaaaaa1")]   // no special
        [InlineData("aaaaaaa1!")]  // no uppercase
        public void Password_MissingACharacterClass_IsInvalid(string password)
        {
            var dto = new AnonymousUserCreationDTO { Username = "validuser", Password = password };

            var results = Validate(dto);

            Assert.True(HasErrorFor(results, nameof(AnonymousUserCreationDTO.Password)));
        }

        [Fact]
        public void Password_MeetingPolicy_IsValid()
        {
            var dto = new AnonymousUserCreationDTO { Username = "validuser", Password = "Abcdef1!" };

            var results = Validate(dto);

            Assert.False(HasErrorFor(results, nameof(AnonymousUserCreationDTO.Password)));
        }
    }
}
