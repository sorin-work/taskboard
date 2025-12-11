using FluentValidation;

namespace Taskboard.Api.Features.Checklists;

public sealed class ChecklistRequestValidator : AbstractValidator<ChecklistRequest>
{
    public ChecklistRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateChecklistItemRequestValidator : AbstractValidator<CreateChecklistItemRequest>
{
    public CreateChecklistItemRequestValidator()
    {
        RuleFor(r => r.Text).NotEmpty().MaximumLength(300);
    }
}

public sealed class UpdateChecklistItemRequestValidator : AbstractValidator<UpdateChecklistItemRequest>
{
    public UpdateChecklistItemRequestValidator()
    {
        RuleFor(r => r.Text).NotEmpty().MaximumLength(300);
    }
}
