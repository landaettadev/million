using FluentValidation;
using MongoDB.Bson;

namespace RealEstate.Application.Validators;

public class PropertyIdValidator : AbstractValidator<string>
{
    public PropertyIdValidator()
    {
        RuleFor(id => id)
            .NotEmpty()
            .WithMessage("Property ID is required")
            .Must(BeValidObjectId)
            .WithMessage("Property ID must be a valid MongoDB ObjectId format");
    }

    private static bool BeValidObjectId(string id)
    {
        return ObjectId.TryParse(id, out _);
    }
}

public class PropertyIdRequest
{
    public string Id { get; set; } = string.Empty;
}
