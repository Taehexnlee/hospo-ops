using FluentValidation;
using api.Models;

namespace api.Validators;

public class EmployeeDtoValidator : AbstractValidator<EmployeeDto>
{
    public EmployeeDtoValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).MaximumLength(50);

        When(x => !string.IsNullOrWhiteSpace(x.HireDate), () =>
        {
            RuleFor(x => x.HireDate!)
                .Must(d => DateOnly.TryParse(d, out _))
                .WithMessage("HireDate must be yyyy-MM-dd.");
        });
    }
}
