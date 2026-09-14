using FluentValidation;
using Takwene.Application.DTOs.Artists;

namespace Takwene.Application.Validators;

public class CreateArtistRequestValidator : AbstractValidator<CreateArtistRequest>
{
    public CreateArtistRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Artist name is required.")
            .MaximumLength(150).WithMessage("Artist name cannot exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Artist email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Artist email cannot exceed 200 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Artist country is required.")
            .MaximumLength(100).WithMessage("Artist country cannot exceed 100 characters.");
    }
}
