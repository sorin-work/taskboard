using FluentValidation;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Labels;

public sealed class LabelRequestValidator : AbstractValidator<LabelRequest>
{
    public LabelRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(40);
        RuleFor(r => r.Color)
            .Must(color => LabelColors.All.Contains(color))
            .WithMessage($"Color must be one of: {string.Join(", ", LabelColors.All)}.");
    }
}
