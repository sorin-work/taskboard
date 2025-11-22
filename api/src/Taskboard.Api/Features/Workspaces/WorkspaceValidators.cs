using FluentValidation;

namespace Taskboard.Api.Features.Workspaces;

public sealed class CreateWorkspaceRequestValidator : AbstractValidator<CreateWorkspaceRequest>
{
    public CreateWorkspaceRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(80);
    }
}

public sealed class UpdateWorkspaceRequestValidator : AbstractValidator<UpdateWorkspaceRequest>
{
    public UpdateWorkspaceRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(80);
    }
}
