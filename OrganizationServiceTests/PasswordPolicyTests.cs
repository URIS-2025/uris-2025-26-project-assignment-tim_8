using System;
using OrganizationService.Validation;
using Xunit;

namespace OrganizationService.Tests.Validation
{
    public class PasswordPolicyTests
    {
        [Fact]
        public void Validate_TooShort_ThrowsCanonicalMessage()
        {
            var ex = Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate("Aa1!aaa")); // 7 chars
            Assert.Equal("Password must be at least 8 characters.", ex.Message);
        }

        [Fact]
        public void Validate_TooLong_ThrowsCanonicalMessage()
        {
            // 65 chars, otherwise valid complexity
            var longPassword = "Aa1!" + new string('a', 61);
            Assert.Equal(65, longPassword.Length);
            var ex = Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate(longPassword));
            Assert.Equal("Password must be at most 64 characters.", ex.Message);
        }

        [Fact]
        public void Validate_MissingLowercase_ThrowsComplexityMessage()
        {
            var ex = Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate("ZX9$MQ2!VK7W"));
            Assert.Equal("Password must include uppercase, lowercase, a number, and a special character.", ex.Message);
        }

        [Fact]
        public void Validate_MissingUppercase_ThrowsComplexityMessage()
        {
            var ex = Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate("zx9$mq2!vk7w"));
            Assert.Equal("Password must include uppercase, lowercase, a number, and a special character.", ex.Message);
        }

        [Fact]
        public void Validate_MissingDigit_ThrowsComplexityMessage()
        {
            var ex = Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate("Zxc$mQw!vKbw"));
            Assert.Equal("Password must include uppercase, lowercase, a number, and a special character.", ex.Message);
        }

        [Fact]
        public void Validate_MissingSpecialCharacter_ThrowsComplexityMessage()
        {
            var ex = Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate("Zx9amQ2bvK7w"));
            Assert.Equal("Password must include uppercase, lowercase, a number, and a special character.", ex.Message);
        }

        [Fact]
        public void Validate_ValidPassword_DoesNotThrow()
        {
            var ex = Record.Exception(() => PasswordPolicy.Validate("Zx9$mQ2!vK7w"));
            Assert.Null(ex);
        }

        [Fact]
        public void Validate_DoesNotTrimInternalSpaces()
        {
            // Internal space counts as a special character and toward length; valid here.
            var ex = Record.Exception(() => PasswordPolicy.Validate("Zx9 mQ2aVK7w"));
            Assert.Null(ex);
        }
    }
}
