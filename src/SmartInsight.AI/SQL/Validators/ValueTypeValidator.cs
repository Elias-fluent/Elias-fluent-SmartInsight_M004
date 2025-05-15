using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Validators
{
    /// <summary>
    /// Specialized validator for handling specific value types with custom validation logic
    /// </summary>
    public class ValueTypeValidator
    {
        private readonly IParameterValidator _baseValidator;
        private readonly ILogger<ValueTypeValidator> _logger;
        
        /// <summary>
        /// Creates a new instance of ValueTypeValidator
        /// </summary>
        /// <param name="baseValidator">The base parameter validator</param>
        /// <param name="logger">Logger instance</param>
        public ValueTypeValidator(
            IParameterValidator baseValidator,
            ILogger<ValueTypeValidator> logger)
        {
            _baseValidator = baseValidator ?? throw new ArgumentNullException(nameof(baseValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Validates parameters of specific value types
        /// </summary>
        /// <param name="parameters">The parameters to validate</param>
        /// <param name="template">The SQL template</param>
        /// <returns>The validation result</returns>
        public async Task<Models.ParameterValidationResult> ValidateValueTypesAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template)
        {
            // Start with base validation
            var result = await _baseValidator.ValidateParametersAsync(parameters, template);
            
            // If already invalid, no need for additional validation
            if (!result.IsValid)
            {
                return result;
            }
            
            // Process each parameter based on detected value type
            foreach (var param in parameters)
            {
                var paramName = param.Key;
                var paramValue = param.Value;
                
                if (paramValue.Value is string strValue)
                {
                    // Detect value type from name and content
                    var valueType = DetectValueType(paramName, strValue);
                    
                    // Apply specific validation based on detected type
                    switch (valueType)
                    {
                        case ValueType.Email:
                            ValidateEmail(strValue, paramName, result);
                            break;
                            
                        case ValueType.Url:
                            ValidateUrl(strValue, paramName, result);
                            break;
                            
                        case ValueType.PhoneNumber:
                            ValidatePhoneNumber(strValue, paramName, result);
                            break;
                            
                        case ValueType.CreditCard:
                            ValidateCreditCard(strValue, paramName, result);
                            break;
                            
                        case ValueType.SocialSecurityNumber:
                            ValidateSsn(strValue, paramName, result);
                            break;
                            
                        case ValueType.IpAddress:
                            ValidateIpAddress(strValue, paramName, result);
                            break;
                            
                        case ValueType.ZipCode:
                            ValidateZipCode(strValue, paramName, result);
                            break;
                            
                        case ValueType.Currency:
                            ValidateCurrency(strValue, paramName, result);
                            break;
                    }
                }
                else if (paramValue.Value is DateTime dateValue)
                {
                    ValidateDateTime(dateValue, paramName, result);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Detects the value type based on parameter name and content
        /// </summary>
        private ValueType DetectValueType(string paramName, string value)
        {
            // First try to infer from parameter name
            if (paramName.Contains("email", StringComparison.OrdinalIgnoreCase))
                return ValueType.Email;
                
            if (paramName.Contains("url", StringComparison.OrdinalIgnoreCase) || 
                paramName.Contains("website", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("link", StringComparison.OrdinalIgnoreCase))
                return ValueType.Url;
                
            if (paramName.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("mobile", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("cell", StringComparison.OrdinalIgnoreCase))
                return ValueType.PhoneNumber;
                
            if (paramName.Contains("credit", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("card", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("cc", StringComparison.OrdinalIgnoreCase))
                return ValueType.CreditCard;
                
            if (paramName.Contains("ssn", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("social", StringComparison.OrdinalIgnoreCase))
                return ValueType.SocialSecurityNumber;
                
            if (paramName.Contains("ip", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("address", StringComparison.OrdinalIgnoreCase))
                return ValueType.IpAddress;
                
            if (paramName.Contains("zip", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("postal", StringComparison.OrdinalIgnoreCase))
                return ValueType.ZipCode;
                
            if (paramName.Contains("price", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("cost", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                paramName.Contains("currency", StringComparison.OrdinalIgnoreCase))
                return ValueType.Currency;
                
            // Then try to infer from content pattern
            if (value.Contains("@") && value.Contains("."))
                return ValueType.Email;
                
            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                return ValueType.Url;
                
            if (Regex.IsMatch(value, @"^\d{3}[-.]?\d{3}[-.]?\d{4}$"))
                return ValueType.PhoneNumber;
                
            if (Regex.IsMatch(value, @"^\d{13,16}$") || Regex.IsMatch(value, @"^\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}$"))
                return ValueType.CreditCard;
                
            if (Regex.IsMatch(value, @"^\d{3}[-]?\d{2}[-]?\d{4}$"))
                return ValueType.SocialSecurityNumber;
                
            if (Regex.IsMatch(value, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
                return ValueType.IpAddress;
                
            if (Regex.IsMatch(value, @"^\d{5}(?:[-\s]\d{4})?$"))
                return ValueType.ZipCode;
                
            if (Regex.IsMatch(value, @"^\$?\d+(?:\.\d{2})?$"))
                return ValueType.Currency;
                
            return ValueType.Unknown;
        }
        
        /// <summary>
        /// Validates an email address
        /// </summary>
        private void ValidateEmail(string value, string paramName, Models.ParameterValidationResult result)
        {
            try
            {
                var address = new MailAddress(value);
                if (address.Address != value)
                {
                    result.AddIssue(new ParameterValidationIssue
                    {
                        ParameterName = paramName,
                        RuleName = "Format.Email",
                        Description = $"Email address '{value}' for parameter '{paramName}' has invalid format",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = value,
                        Recommendation = "Provide a valid email address (e.g., user@example.com)"
                    });
                }
            }
            catch (FormatException)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.Email",
                    Description = $"Email address '{value}' for parameter '{paramName}' has invalid format",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid email address (e.g., user@example.com)"
                });
            }
        }
        
        /// <summary>
        /// Validates a URL
        /// </summary>
        private void ValidateUrl(string value, string paramName, Models.ParameterValidationResult result)
        {
            bool isValid = Uri.TryCreate(value, UriKind.Absolute, out Uri? uriResult) && 
                          (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                          
            if (!isValid)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.Url",
                    Description = $"URL '{value}' for parameter '{paramName}' has invalid format",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid URL (e.g., https://example.com)"
                });
            }
        }
        
        /// <summary>
        /// Validates a phone number
        /// </summary>
        private void ValidatePhoneNumber(string value, string paramName, Models.ParameterValidationResult result)
        {
            var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.PhoneNumber",
                    Description = $"Phone number '{value}' for parameter '{paramName}' has invalid format",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid phone number with area code"
                });
            }
        }
        
        /// <summary>
        /// Validates a credit card number using Luhn algorithm
        /// </summary>
        private void ValidateCreditCard(string value, string paramName, Models.ParameterValidationResult result)
        {
            var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
            
            // Credit card numbers should have 13-16 digits
            if (digitsOnly.Length < 13 || digitsOnly.Length > 16)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.CreditCard",
                    Description = $"Credit card number for parameter '{paramName}' has invalid length",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid credit card number (13-16 digits)"
                });
                return;
            }
            
            // Use Luhn algorithm to validate
            int sum = 0;
            bool alternate = false;
            for (int i = digitsOnly.Length - 1; i >= 0; i--)
            {
                int n = int.Parse(digitsOnly[i].ToString());
                if (alternate)
                {
                    n *= 2;
                    if (n > 9)
                    {
                        n = (n % 10) + 1;
                    }
                }
                sum += n;
                alternate = !alternate;
            }
            
            if (sum % 10 != 0)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.CreditCard",
                    Description = $"Credit card number for parameter '{paramName}' failed checksum validation",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid credit card number"
                });
            }
            
            // Security warning for credit card data
            result.AddIssue(new ParameterValidationIssue
            {
                ParameterName = paramName,
                RuleName = "Security.SensitiveData",
                Description = $"Parameter '{paramName}' appears to contain credit card data",
                Severity = ValidationSeverity.Critical,
                OriginalValue = value,
                Recommendation = "Storing unencrypted credit card data is not recommended and may violate PCI DSS requirements"
            });
        }
        
        /// <summary>
        /// Validates a Social Security Number (SSN)
        /// </summary>
        private void ValidateSsn(string value, string paramName, Models.ParameterValidationResult result)
        {
            var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
            
            if (digitsOnly.Length != 9)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.SSN",
                    Description = $"Social Security Number for parameter '{paramName}' has invalid format",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid SSN (9 digits)"
                });
            }
            
            // Security warning for SSN data
            result.AddIssue(new ParameterValidationIssue
            {
                ParameterName = paramName,
                RuleName = "Security.SensitiveData",
                Description = $"Parameter '{paramName}' appears to contain a Social Security Number",
                Severity = ValidationSeverity.Critical,
                OriginalValue = value,
                Recommendation = "Storing unencrypted SSNs is not recommended and may violate data protection regulations"
            });
        }
        
        /// <summary>
        /// Validates an IP address
        /// </summary>
        private void ValidateIpAddress(string value, string paramName, Models.ParameterValidationResult result)
        {
            var segments = value.Split('.');
            if (segments.Length != 4)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.IPAddress",
                    Description = $"IP address '{value}' for parameter '{paramName}' has invalid format",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid IPv4 address (e.g., 192.168.1.1)"
                });
                return;
            }
            
            foreach (var segment in segments)
            {
                if (!int.TryParse(segment, out int value1) || value1 < 0 || value1 > 255)
                {
                    result.AddIssue(new ParameterValidationIssue
                    {
                        ParameterName = paramName,
                        RuleName = "Format.IPAddress",
                        Description = $"IP address '{value}' for parameter '{paramName}' has invalid segment values",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = value,
                        Recommendation = "Each segment of an IPv4 address must be between 0 and 255"
                    });
                    break;
                }
            }
        }
        
        /// <summary>
        /// Validates a ZIP code
        /// </summary>
        private void ValidateZipCode(string value, string paramName, Models.ParameterValidationResult result)
        {
            var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
            
            if (digitsOnly.Length != 5 && digitsOnly.Length != 9)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.ZipCode",
                    Description = $"ZIP code '{value}' for parameter '{paramName}' has invalid format",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid ZIP code (5 digits or ZIP+4 format)"
                });
            }
        }
        
        /// <summary>
        /// Validates a currency value
        /// </summary>
        private void ValidateCurrency(string value, string paramName, Models.ParameterValidationResult result)
        {
            // Remove currency symbol if present
            var valueWithoutSymbol = value.TrimStart('$', '€', '£', '¥');
            
            if (!decimal.TryParse(valueWithoutSymbol, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal amount))
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.Currency",
                    Description = $"Currency value '{value}' for parameter '{paramName}' has invalid format",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid currency amount (e.g., $10.99)"
                });
                return;
            }
            
            if (amount < 0)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Business.NegativeCurrency",
                    Description = $"Currency value '{value}' for parameter '{paramName}' is negative",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Check if a negative currency value is intended"
                });
            }
            
            if (amount > 1000000)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Business.LargeCurrency",
                    Description = $"Currency value '{value}' for parameter '{paramName}' is unusually large",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Verify this large currency amount is correct"
                });
            }
        }
        
        /// <summary>
        /// Validates a date and time value
        /// </summary>
        private void ValidateDateTime(DateTime value, string paramName, Models.ParameterValidationResult result)
        {
            var now = DateTime.UtcNow;
            var century = 100 * 365.25 * 24 * 60 * 60;
            
            // Check for dates too far in the past (more than 100 years)
            if (value < now.AddSeconds(-century))
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Range.Date",
                    Description = $"Date value '{value:yyyy-MM-dd}' for parameter '{paramName}' is too far in the past",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a more recent date"
                });
            }
            
            // Check for dates too far in the future (more than 100 years)
            if (value > now.AddSeconds(century))
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Range.Date",
                    Description = $"Date value '{value:yyyy-MM-dd}' for parameter '{paramName}' is too far in the future",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a more reasonable future date"
                });
            }
            
            // Check for default/uninitialized dates
            if (value == DateTime.MinValue || value == DateTime.MaxValue || value == default)
            {
                result.AddIssue(new ParameterValidationIssue
                {
                    ParameterName = paramName,
                    RuleName = "Format.Date",
                    Description = $"Date value for parameter '{paramName}' appears to be uninitialized",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = value,
                    Recommendation = "Provide a valid date"
                });
            }
        }
    }
    
    /// <summary>
    /// Types of values that can be validated
    /// </summary>
    public enum ValueType
    {
        /// <summary>
        /// Unknown value type
        /// </summary>
        Unknown,
        
        /// <summary>
        /// Email address
        /// </summary>
        Email,
        
        /// <summary>
        /// URL
        /// </summary>
        Url,
        
        /// <summary>
        /// Phone number
        /// </summary>
        PhoneNumber,
        
        /// <summary>
        /// Credit card number
        /// </summary>
        CreditCard,
        
        /// <summary>
        /// Social Security Number
        /// </summary>
        SocialSecurityNumber,
        
        /// <summary>
        /// IP address
        /// </summary>
        IpAddress,
        
        /// <summary>
        /// ZIP code
        /// </summary>
        ZipCode,
        
        /// <summary>
        /// Currency value
        /// </summary>
        Currency
    }
} 