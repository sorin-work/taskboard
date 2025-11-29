namespace Taskboard.Api.Features.Labels;

public sealed record LabelRequest(string Name, string Color);

public sealed record LabelDto(Guid Id, string Name, string Color);
