namespace Taskboard.Api.Features.Checklists;

public sealed record ChecklistRequest(string Title);

public sealed record CreateChecklistItemRequest(string Text);

public sealed record UpdateChecklistItemRequest(string Text, bool Done);

public sealed record ChecklistItemDto(Guid Id, string Text, bool Done, int Position);

public sealed record ChecklistDto(Guid Id, string Title, IReadOnlyList<ChecklistItemDto> Items);
