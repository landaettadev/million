using FluentValidation;
using MongoDB.Bson;

namespace RealEstate.Application.Validators;

public class PropertyIdValidator : AbstractValidator<string>
{
    public PropertyIdValidator()
    {
        RuleFor(id => id)
            .NotEmpty().WithMessage("Property ID cannot be empty.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("Invalid property ID format.");
    }
}
