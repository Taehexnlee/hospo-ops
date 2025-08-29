using FluentValidation;
using api.Models;

namespace api.Validators;
public class EodReportValidator : AbstractValidator<EodReport>
{
    public EodReportValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.NetSales).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BizDate).NotEmpty().Must(d => d != default);
    }
}
