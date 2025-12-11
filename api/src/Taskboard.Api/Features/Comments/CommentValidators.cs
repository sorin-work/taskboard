using FluentValidation;

namespace Taskboard.Api.Features.Comments;

public sealed class CommentRequestValidator : AbstractValidator<CommentRequest>
{
    public CommentRequestValidator()
    {
        RuleFor(r => r.Body).NotEmpty().MaximumLength(5000);
    }
}
