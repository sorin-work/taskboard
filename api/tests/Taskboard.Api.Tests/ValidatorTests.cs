using Taskboard.Api.Domain;
using Taskboard.Api.Features.Auth;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Invitations;
using Taskboard.Api.Features.Labels;

namespace Taskboard.Api.Tests;

/// <summary>Validators on their own, without going through http.</summary>
public class ValidatorTests
{
    [Theory]
    [InlineData("ok@example.com", "long-enough", "Ok", true)]
    [InlineData("ok@example.com", "1234567", "Ok", false)]
    [InlineData("nope", "long-enough", "Ok", false)]
    [InlineData("ok@example.com", "long-enough", "   ", false)]
    public void Signup(string email, string password, string name, bool valid) =>
        new SignupRequestValidator().Validate(new SignupRequest(email, password, name)).IsValid.Should().Be(valid);

    [Fact]
    public void Signup_PasswordOver128_IsRejected() =>
        new SignupRequestValidator()
            .Validate(new SignupRequest("a@example.com", new string('x', 129), "A"))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData(WorkspaceRole.Member, true)]
    [InlineData(WorkspaceRole.Admin, true)]
    [InlineData(WorkspaceRole.Owner, false)]
    public void Invitation_Role(WorkspaceRole role, bool valid) =>
        new CreateInvitationRequestValidator().Validate(new CreateInvitationRequest("a@example.com", role)).IsValid.Should().Be(valid);

    [Fact]
    public void Board_WithTooManyColumns_IsRejected()
    {
        var columns = Enumerable.Range(1, 21).Select(i => $"Column {i}").ToList();

        var result = new CreateBoardRequestValidator().Validate(new CreateBoardRequest("Big", null, columns));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Contain("at most 20");
    }

    [Theory]
    [InlineData("blue", true)]
    [InlineData("Blue", false)]
    [InlineData("#0000ff", false)]
    public void Label_Color(string color, bool valid) =>
        new LabelRequestValidator().Validate(new LabelRequest("Bug", color)).IsValid.Should().Be(valid);

    [Fact]
    public void Card_TitleOver200_IsRejected() =>
        new CreateCardRequestValidator().Validate(new CreateCardRequest(new string('t', 201))).IsValid.Should().BeFalse();

    [Fact]
    public void Card_DueDateIsOptional() =>
        new UpdateCardRequestValidator().Validate(new UpdateCardRequest("Title", null)).IsValid.Should().BeTrue();

    [Fact]
    public void WipLimit_NullClearsIt() =>
        new SetWipLimitRequestValidator().Validate(new SetWipLimitRequest(null)).IsValid.Should().BeTrue();
}
