using System.ComponentModel.DataAnnotations;

namespace Web_API.Validation;

/// <summary>Перевіряє, що рядок містить хоча б один символ, який не є пробілом.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    public NotWhiteSpaceAttribute() : base("Поле не може бути порожнім або містити лише пробіли.") { }

    public override bool IsValid(object? value) => value is not string text || !string.IsNullOrWhiteSpace(text);
}
