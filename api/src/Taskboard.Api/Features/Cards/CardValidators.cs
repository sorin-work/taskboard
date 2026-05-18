using FluentValidation;

namespace Taskboard.Api.Features.Cards;

public sealed class CreateCardRequestValidator : AbstractValidator<CreateCardRequest>
{
    public CreateCardRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateCardRequestValidator : AbstractValidator<UpdateCardRequest>
{
    public UpdateCardRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Description).MaximumLength(10000);

        // A typo in the year (0202, 20266) otherwise sorts to the top of my tasks forever.
        RuleFor(r => r.DueDate)
            .InclusiveBetween(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31))
            .When(r => r.DueDate is not null)
            .WithMessage("Due date must be between the years 2000 and 2100.");
    }
}

public sealed class MoveCardRequestValidator : AbstractValidator<MoveCardRequest>
{
    public MoveCardRequestValidator()
    {
        RuleFor(r => r.ColumnId).NotEmpty();
    }
}

public sealed class SetCardLabelsRequestValidator : AbstractValidator<SetCardLabelsRequest>
{
    public SetCardLabelsRequestValidator()
    {
        RuleFor(r => r.LabelIds).NotNull().Must(ids => ids.Count <= 20).WithMessage("A card can have at most 20 labels.");
    }
}

public sealed class SetCardAssigneesRequestValidator : AbstractValidator<SetCardAssigneesRequest>
{
    public SetCardAssigneesRequestValidator()
    {
        RuleFor(r => r.UserIds).NotNull().Must(ids => ids.Count <= 20).WithMessage("A card can have at most 20 assignees.");
    }
}
