using System.Globalization;
using FluentValidation;
using api.Models;

namespace api.Validators;

public class EodReportDtoValidator : AbstractValidator<EodReportDto>
{
    public EodReportDtoValidator()
    {
        RuleFor(x => x.StoreId)
            .GreaterThan(0)
            .WithMessage("StoreId must be greater than 0.");

        RuleFor(x => x.BizDate)
            .NotEmpty()
            .Must(s => DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("BizDate must be in yyyy-MM-dd format (yyyy-MM-dd).");

        RuleFor(x => x.NetSales)
            .GreaterThanOrEqualTo(0).WithMessage("NetSales cannot be negative.")
            .PrecisionScale(18, 2, true); // 총 18자리, 소수 2자리, overflown 값 반올림 허용

        RuleFor(x => x.Tickets)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Tickets cannot be negative.");
    }
}