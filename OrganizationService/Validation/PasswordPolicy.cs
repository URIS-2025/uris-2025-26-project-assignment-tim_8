using System.Text.RegularExpressions;

namespace OrganizationService.Validation;

public static class PasswordPolicy
{
    private const int MinLength = 8;
    private const int MaxLength = 64;

    private static readonly Regex ComplexityRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,64}$",
        RegexOptions.Compiled);

    public static void Validate(string password)
    {
        if (password is null || password.Length < MinLength)
            throw new ArgumentException("Password must be at least 8 characters.");

        if (password.Length > MaxLength)
            throw new ArgumentException("Password must be at most 64 characters.");

        if (!ComplexityRegex.IsMatch(password))
            throw new ArgumentException("Password must include uppercase, lowercase, a number, and a special character.");
    }
}
