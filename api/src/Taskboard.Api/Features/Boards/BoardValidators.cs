using FluentValidation;

namespace Taskboard.Api.Features.Boards;

public sealed class CreateBoardRequestValidator : AbstractValidator<CreateBoardRequest>
{
    public CreateBoardRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Description).MaximumLength(2000);
        RuleFor(r => r.Columns).Must(c => c is null || c.Count <= 20).WithMessage("A board can start with at most 20 columns.");
        RuleForEach(r => r.Columns).NotEmpty().MaximumLength(60);
    }
}

public sealed class UpdateBoardRequestValidator : AbstractValidator<UpdateBoardRequest>
{
    public UpdateBoardRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Description).MaximumLength(2000);
    }
}
