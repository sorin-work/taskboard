using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Export;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class ExportTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    [Fact]
    public async Task Export_ReturnsOneRowPerCard()
    {
        var (client, user) = await factory.CreateUserAsync("Ana");
        var workspace = await client.CreateWorkspaceAsync();
        var board = await client.CreateBoardAsync(workspace.Id, "Q3 Plan!");
        var card = await client.CreateCardAsync(board.Columns[1].Id, "Write \"launch\" post");
        await client.PutAsJsonAsync($"/api/cards/{card.Id}", new UpdateCardRequest(card.Title, "Line one\nline two, with comma", new DateOnly(2026, 5, 1)));
        await client.PutAsJsonAsync($"/api/cards/{card.Id}/assignees", new SetCardAssigneesRequest([user.Id]));
        await client.CreateCardAsync(board.Columns[0].Id, "First column card");

        var response = await client.GetAsync($"/api/boards/{board.Id}/export.csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentDisposition!.FileName.Should().StartWith("q3-plan-");
        var text = await response.Content.ReadAsStringAsync();
        var lines = text.TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        lines[0].Should().Be("Column,Title,Description,Assignees,Labels,Due date,Completed,Archived,Checklist,Created");
        lines[1].Should().StartWith("To do,First column card,");
        text.Should().Contain("In progress,\"Write \"\"launch\"\" post\",\"Line one\nline two, with comma\",Ana,,2026-05-01,no,no,");
    }

    [Fact]
    public async Task Export_ByNonMember_ReturnsNotFound()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (stranger, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var board = await owner.CreateBoardAsync(workspace.Id);

        (await stranger.GetAsync($"/api/boards/{board.Id}/export.csv")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    public void Escape_QuotesWhenNeeded(string value, string expected) =>
        CsvWriter.Escape(value).Should().Be(expected);

    [Theory]
    [InlineData("Team Board", "team-board")]
    [InlineData("  ***  ", "board")]
    public void Slug_MakesSafeFileNames(string name, string expected) =>
        ExportEndpoints.Slug(name).Should().Be(expected);
}
