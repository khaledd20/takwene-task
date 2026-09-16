using System.Text.RegularExpressions;
using FluentValidation;
using Takwene.Application.DTOs.Tracks;
using Takwene.Domain.Enums;

namespace Takwene.Application.Validators;

public class CreateTrackRequestValidator : AbstractValidator<CreateTrackRequest>
{
    // ISRC regex allows either 12 alphanumeric characters (e.g. USRC17607839) or hyphenated (US-RC1-76-07839)
    private static readonly Regex IsrcRegex = new(@"^[A-Z]{2}-?[A-Z0-9]{3}-?[0-9]{2}-?[0-9]{5}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CreateTrackRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Track title is required.")
            .MaximumLength(200).WithMessage("Track title cannot exceed 200 characters.");

        RuleFor(x => x.ArtistId)
            .NotEmpty().WithMessage("ArtistId is required.");

        RuleFor(x => x.Isrc)
            .NotEmpty().WithMessage("ISRC code is required.")
            .Must(isrc => !string.IsNullOrWhiteSpace(isrc) && IsrcRegex.IsMatch(isrc.Trim()))
            .WithMessage("ISRC must be a valid 12-character alphanumeric code (e.g., 'USRC17607839' or 'US-RC1-76-07839').");

        RuleFor(x => x.ReleaseDate)
            .NotEmpty().WithMessage("Release date is required.");

        RuleFor(x => x.Genre)
            .NotEmpty().WithMessage("Genre is required.")
            .MaximumLength(80).WithMessage("Genre cannot exceed 80 characters.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || Enum.TryParse<TrackStatus>(s.Trim(), true, out _))
            .WithMessage("Status must be either 'draft', 'submitted', or 'distributed'.");
    }
}

public class UpdateTrackStatusRequestValidator : AbstractValidator<UpdateTrackStatusRequest>
{
    public UpdateTrackStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => Enum.TryParse<TrackStatus>(s.Trim(), true, out _))
            .WithMessage("Status must be one of: 'draft', 'submitted', 'distributed'.");
    }
}

public class DistributeTrackRequestValidator : AbstractValidator<DistributeTrackRequest>
{
    public DistributeTrackRequestValidator()
    {
        RuleFor(x => x.DspIds)
            .NotNull().WithMessage("DSP IDs list is required.")
            .Must(ids => ids != null && ids.Count > 0).WithMessage("At least one DSP ID must be provided.");

        RuleForEach(x => x.DspIds)
            .NotEmpty().WithMessage("DSP ID cannot be empty.");
    }
}

public class UpdateDistributionStatusRequestValidator : AbstractValidator<UpdateDistributionStatusRequest>
{
    public UpdateDistributionStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => s != null && (s.Trim().Equals("live", StringComparison.OrdinalIgnoreCase) || s.Trim().Equals("rejected", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Distribution status must be either 'live' or 'rejected'.");

        When(x => x.Status != null && x.Status.Trim().Equals("rejected", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty().WithMessage("Rejection reason is required when rejecting a distribution.")
                .MaximumLength(500).WithMessage("Rejection reason cannot exceed 500 characters.");
        });
    }
}
