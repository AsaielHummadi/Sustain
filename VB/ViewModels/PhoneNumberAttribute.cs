using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
namespace Sustain.ViewModels.Validation
{
    public class PhoneNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }
            string phoneNumber = value.ToString()!;
            // Pattern: +9665XXXXXXXX or 05XXXXXXXX
            string pattern = @"^((\+9665\d{8})|(05\d{8}))$";
            if (Regex.IsMatch(phoneNumber, pattern))
            {
                return ValidationResult.Success;
            }
            return new ValidationResult(
                ErrorMessage ?? $"{validationContext.DisplayName} must be a valid phone number. Example: +966566193395 or 0566193395"
            );
        }
    }
    public class SaudiNationalIdAttribute : ValidationAttribute
    {
        private static readonly string[] ValidRegionCodes = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }
            string nationalId = value.ToString()!;
            if (nationalId.Length != 10)
            {
                return new ValidationResult(
                    ErrorMessage ?? $"{validationContext.DisplayName} must be 10 digits long."
                );
            }
            if (!nationalId.All(char.IsDigit))
            {
                return new ValidationResult(
                    ErrorMessage ?? $"{validationContext.DisplayName} must contain only digits."
                );
            }
            string regionCode = nationalId.Substring(0, 1);
            // Validate the region code
            if (!ValidRegionCodes.Contains(regionCode))
            {
                return new ValidationResult(
                    ErrorMessage ?? $"{validationContext.DisplayName} is not a valid Saudi National ID."
                );
            }
            return ValidationResult.Success;
        }
    }
    public class PasswordConfirmationAttribute : ValidationAttribute
    {
        private readonly string _passwordPropertyName;
        public PasswordConfirmationAttribute(string passwordPropertyName = "Password")
        {
            _passwordPropertyName = passwordPropertyName;
        }
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var passwordProperty = validationContext.ObjectType.GetProperty(_passwordPropertyName);
            if (passwordProperty == null)
            {
                return new ValidationResult($"Unknown property: {_passwordPropertyName}");
            }
            var passwordValue = passwordProperty.GetValue(validationContext.ObjectInstance)?.ToString();
            var confirmationValue = value?.ToString();
            if (passwordValue != confirmationValue)
            {
                return new ValidationResult(
                    ErrorMessage ?? "Password and confirmation password do not match."
                );
            }
            return ValidationResult.Success;
        }
    }
}