using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Features.Auth;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Labels;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class CardFilterTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private static readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record Setup(HttpClient Client, UserDto Me, UserDto Other, BoardDto Board, LabelDto Bug);

    private async Task<Setup> SetupAsync()
    {
        var (client, me) = await factory.CreateUserAsync("me");
        var (_, other) = await factory.CreateUserAsync("other");
        var workspace = await client.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, other.Id);
        var board = await client.CreateBoardAsync(workspace.Id);
        var bug = await (await client.PostAsJsonAsync($"/api/boards/{board.Id}/labels", new LabelRequest("Bug", "red"))).ReadAsAsync<LabelDto>();
        return new Setup(client, me, other, board, bug);
    }

    private static async Task<CardDto> CardAsync(
        Setup s,
        string title,
        Guid? assignee = null,
        bool bug = false,
        DateOnly? due = null,
        bool completed = false)
    {
        var card = await s.Client.CreateCardAsync(s.Board.Columns[0].Id, title);
        if (assignee is { } userId)
        {
            await s.Client.PutAsJsonAsync($"/api/cards/{card.Id}/assignees", new SetCardAssigneesRequest([userId]));
        }

        if (bug)
        {
            await s.Client.PutAsJsonAsync($"/api/cards/{card.Id}/labels", new SetCardLabelsRequest([s.Bug.Id]));
        }

        if (due is not null || completed)
        {
            await s.Client.PutAsJsonAsync($"/api/cards/{card.Id}", new UpdateCardRequest(title, null, due, completed));
        }

        return card;
    }

    private static async Task<IEnumerable<string>> TitlesAsync(Setup s, string query) =>
        (await s.Client.GetCardsAsync(s.Board.Id, query)).Select(c => c.Title);

    [Fact]
    public async Task Assignee_FiltersByUserMeAndNone()
    {
        var s = await SetupAsync();
        await CardAsync(s, "Mine", assignee: s.Me.Id);
        await CardAsync(s, "Theirs", assignee: s.Other.Id);
        await CardAsync(s, "Nobody's");

        (await TitlesAsync(s, "?assignee=me")).Should().Equal("Mine");
        (await TitlesAsync(s, $"?assignee={s.Other.Id}")).Should().Equal("Theirs");
        (await TitlesAsync(s, "?assignee=none")).Should().Equal("Nobody's");
    }

    [Fact]
    public async Task Label_FiltersCards()
    {
        var s = await SetupAsync();
        await CardAsync(s, "Crash", bug: true);
        await CardAsync(s, "Feature");

        (await TitlesAsync(s, $"?label={s.Bug.Id}")).Should().Equal("Crash");
    }

    [Fact]
    public async Task Due_FiltersByWindow()
    {
        var s = await SetupAsync();
        await CardAsync(s, "Late", due: _today.AddDays(-2));
        await CardAsync(s, "Late but done", due: _today.AddDays(-2), completed: true);
        await CardAsync(s, "Now", due: _today);
        await CardAsync(s, "Soon", due: _today.AddDays(6));
        await CardAsync(s, "Later", due: _today.AddDays(7));
        await CardAsync(s, "Whenever");

        (await TitlesAsync(s, "?due=overdue")).Should().Equal("Late");
        (await TitlesAsync(s, "?due=today")).Should().Equal("Now");
        (await TitlesAsync(s, "?due=week")).Should().Equal("Now", "Soon");
        (await TitlesAsync(s, "?due=none")).Should().Equal("Whenever");
    }

    [Fact]
    public async Task Due_UsesTheCallersDate()
    {
        var s = await SetupAsync();
        await CardAsync(s, "Tomorrow in UTC", due: _today.AddDays(1));

        (await TitlesAsync(s, "?due=today")).Should().BeEmpty();
        (await TitlesAsync(s, $"?due=today&today={_today.AddDays(1):yyyy-MM-dd}")).Should().Equal("Tomorrow in UTC");
    }

    [Fact]
    public async Task Due_IgnoresACallerDateFarFromUtc()
    {
        var s = await SetupAsync();
        await CardAsync(s, "Now", due: _today);

        (await TitlesAsync(s, $"?due=today&today={_today.AddDays(30):yyyy-MM-dd}")).Should().Equal("Now");
    }

    [Fact]
    public async Task Q_MatchesTitleIgnoringCase()
    {
        var s = await SetupAsync();
        await CardAsync(s, "Fix login redirect");
        await CardAsync(s, "Update pricing page");

        (await TitlesAsync(s, "?q=LOGIN")).Should().Equal("Fix login redirect");
    }

    [Fact]
    public async Task Filters_Combine()
    {
        var s = await SetupAsync();
        await CardAsync(s, "My bug", assignee: s.Me.Id, bug: true);
        await CardAsync(s, "My task", assignee: s.Me.Id);
        await CardAsync(s, "Their bug", assignee: s.Other.Id, bug: true);

        (await TitlesAsync(s, $"?assignee=me&label={s.Bug.Id}")).Should().Equal("My bug");
    }

    [Theory]
    [InlineData("?due=someday")]
    [InlineData("?assignee=bob")]
    public async Task InvalidFilter_ReturnsBadRequest(string query)
    {
        var s = await SetupAsync();

        var response = await s.Client.GetAsync($"/api/boards/{s.Board.Id}/cards{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
