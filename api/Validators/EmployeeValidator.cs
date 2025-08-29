using FluentValidation;
using api.Models;

namespace api.Validators;
public class EmployeeValidator : AbstractValidator<Employee>
{
    public EmployeeValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Role).NotEmpty();
        // HireDate는 null 허용, 값이 있으면 유효한 날짜여야 함
        RuleFor(x => x.HireDate).Must(_ => true);
    }
}
