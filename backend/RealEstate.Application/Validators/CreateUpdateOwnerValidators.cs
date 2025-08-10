using FluentValidation;

namespace RealEstate.Application.Validators;

public sealed class CreateOwnerValidator : AbstractValidator<CreateOwnerDto>
{
    public CreateOwnerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateOwnerValidator : AbstractValidator<UpdateOwnerDto>
{
    public UpdateOwnerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200);
    }
}


