using FluentValidation;
using api.Models;

namespace api.Validators;

public class StoreDtoValidator : AbstractValidator<StoreDto>
{
    public StoreDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
