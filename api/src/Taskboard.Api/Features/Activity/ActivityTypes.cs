namespace Taskboard.Api.Features.Activity;

public static class ActivityTypes
{
    public const string BoardCreated = "board.created";
    public const string ColumnCreated = "column.created";
    public const string ColumnRenamed = "column.renamed";
    public const string ColumnDeleted = "column.deleted";
    public const string CardCreated = "card.created";
    public const string CardRenamed = "card.renamed";
    public const string CardDescriptionChanged = "card.description_changed";
    public const string CardMoved = "card.moved";
    public const string CardDueDateSet = "card.due_date_set";
    public const string CardDueDateCleared = "card.due_date_cleared";
    public const string CardCompleted = "card.completed";
    public const string CardReopened = "card.reopened";
    public const string CardAssigned = "card.assigned";
    public const string CardUnassigned = "card.unassigned";
    public const string CardLabelAdded = "card.label_added";
    public const string CardLabelRemoved = "card.label_removed";
    public const string CardArchived = "card.archived";
    public const string CardRestored = "card.restored";
    public const string CardDeleted = "card.deleted";
    public const string CommentAdded = "comment.added";
    public const string ChecklistItemCompleted = "checklist.item_completed";
}
