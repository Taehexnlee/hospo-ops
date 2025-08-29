using FluentValidation;
using api.Models;

namespace api.Validators;
public class StoreValidator : AbstractValidator<Store>
{
    public StoreValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2);
    }
}
