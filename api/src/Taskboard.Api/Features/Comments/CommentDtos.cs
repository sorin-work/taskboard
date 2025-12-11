using Taskboard.Api.Features.Cards;

namespace Taskboard.Api.Features.Comments;

public sealed record CommentRequest(string Body);

public sealed record CommentDto(Guid Id, Guid CardId, CardUserDto Author, string Body, DateTime CreatedAt, DateTime? EditedAt);
