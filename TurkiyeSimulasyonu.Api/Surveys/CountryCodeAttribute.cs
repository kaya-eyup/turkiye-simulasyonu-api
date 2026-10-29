using System.ComponentModel.DataAnnotations;

namespace TurkiyeSimulasyonu.Api.Surveys;

// [StringLength] gibi: geçersizse [ApiController] otomatik 400 + errors.Country döner
public sealed class CountryCodeAttribute : ValidationAttribute
{
    public CountryCodeAttribute()
        : base("Must be an ISO 3166-1 alpha-2 country code (e.g. DE).")
    {
    }

    public override bool IsValid(object? value) =>
        value is string code && Countries.IsValid(code);
}
