using FluentValidation;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Invitations;

public sealed class CreateInvitationRequestValidator : AbstractValidator<CreateInvitationRequest>
{
    public CreateInvitationRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(r => r.Role)
            .Must(role => role is WorkspaceRole.Member or WorkspaceRole.Admin)
            .WithMessage("Invitations can be for members or admins. Promote someone to owner after they join.");
    }
}
