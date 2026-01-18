using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Invitations;

namespace Taskboard.Api.Seeding;

/// <summary>
/// Creates demo@example.com / demo-password with a couple of realistic
/// workspaces. Safe to run on every start: it does nothing if the demo user
/// already exists. Dates are relative to today so there is always something
/// overdue, due today and coming up.
/// </summary>
public sealed partial class DemoSeeder(
    TaskboardDbContext db,
    IPasswordHasher<User> hasher,
    TimeProvider clock,
    ILogger<DemoSeeder> logger)
{
    public const string DemoEmail = "demo@example.com";
    public const string DemoPassword = "demo-password";

    private DateTime _now;
    private DateOnly _today;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Email == DemoEmail, ct))
        {
            LogSkipped();
            return;
        }

        _now = clock.GetUtcNow().UtcDateTime;
        _today = DateOnly.FromDateTime(_now);

        var demo = User(DemoEmail, "Jordan Lee", daysAgo: 60);
        var priya = User("priya@example.com", "Priya Shah", daysAgo: 58);
        var marco = User("marco@example.com", "Marco Rossi", daysAgo: 45);
        var emma = User("emma@example.com", "Emma Clarke", daysAgo: 40);
        var sam = User("sam@example.com", "Sam Okafor", daysAgo: 20);

        var studio = Workspace("Northwind Studio", daysAgo: 58,
            (demo, WorkspaceRole.Owner), (priya, WorkspaceRole.Admin), (marco, WorkspaceRole.Member), (emma, WorkspaceRole.Member));
        var home = Workspace("Home renovation", daysAgo: 20, (demo, WorkspaceRole.Owner), (sam, WorkspaceRole.Member));

        SeedWebsiteBoard(studio, demo, priya, marco, emma);
        SeedMarketingBoard(studio, demo, priya, emma);
        SeedArchivedBoard(studio, priya);
        SeedKitchenBoard(home, demo, sam);

        db.Invitations.Add(new Invitation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = studio.Id,
            Email = "lena@example.com",
            Role = WorkspaceRole.Member,
            TokenHash = InvitationTokens.Generate().Hash,
            InvitedById = priya.Id,
            CreatedAt = _now.AddDays(-2),
            ExpiresAt = _now.AddDays(5),
        });

        await db.SaveChangesAsync(ct);
        LogSeeded(DemoEmail);
    }

    private void SeedWebsiteBoard(Workspace ws, User demo, User priya, User marco, User emma)
    {
        var board = Board(ws, "Website relaunch", "New marketing site on the new design system. Launch target end of next month.", demo, daysAgo: 55,
            "Backlog", "To do", "In progress", "Review", "Done");
        var design = Label(board, "Design", "purple");
        var frontend = Label(board, "Frontend", "blue");
        var backend = Label(board, "Backend", "teal");
        var bug = Label(board, "Bug", "red");
        var content = Label(board, "Content", "yellow");
        var urgent = Label(board, "Urgent", "orange");
        var (backlog, todo, doing, review, done) = (board.Columns[0], board.Columns[1], board.Columns[2], board.Columns[3], board.Columns[4]);

        Card(backlog, "Dark mode for the docs pages", demo, 30, labels: [design, frontend]);
        Card(backlog, "Localise pricing page (DE, FR)", priya, 26, labels: [content],
            description: "Marketing wants German and French first. Currency stays EUR for both.");
        Card(backlog, "Replace hand-rolled carousel with CSS scroll snap", marco, 18, labels: [frontend]);

        Card(todo, "Write case study: Fernhill Logistics", priya, 21, assignees: [emma], due: 6, labels: [content],
            checklist: ("Draft", [("Interview notes", true), ("First draft", false), ("Customer sign-off", false)]));
        Card(todo, "Cookie banner copy and consent modes", demo, 14, assignees: [demo], due: 2, labels: [content, urgent],
            description: "Legal sent the final wording. Needs the three consent levels: necessary, analytics, marketing.");
        Card(todo, "Set up redirects from old blog URLs", marco, 12, assignees: [marco], due: 9, labels: [backend],
            comments: [(marco, "Exported 214 old URLs from the sitemap. About 30 have no obvious new home."), (priya, "Send those 30 to me, I'll map them by hand.")]);

        var hero = Card(doing, "Homepage hero and navigation", demo, 25, assignees: [demo, emma], due: 0, labels: [design, frontend],
            description: "Implement the approved Figma frames. Mobile nav uses the drawer pattern from the design system.",
            checklist: ("Done when", [("Desktop layout", true), ("Mobile drawer", true), ("Keyboard navigation", false), ("Reduced motion", false)]),
            comments: [(emma, "Pushed the mobile drawer. The focus trap still needs work."), (demo, "Thanks! I'll pick up keyboard nav tonight.")]);
        Moved(hero, demo, "To do", "In progress", daysAgo: 6);
        Card(doing, "Contact form spam keeps getting through", marco, 9, assignees: [marco], due: -2, labels: [bug, backend, urgent],
            description: "We're getting 40+ junk submissions a day since the old captcha was removed.",
            comments: [(priya, "Can we try a honeypot field before adding a captcha back?"), (marco, "Yes, adding a honeypot plus rate limit per IP.")]);
        Card(doing, "Pricing page comparison table", emma, 11, assignees: [emma], due: 4, labels: [design, frontend],
            checklist: ("Columns", [("Starter", true), ("Team", true), ("Business", false)]));

        var footer = Card(review, "Footer links and legal pages", priya, 16, assignees: [priya], due: -1, labels: [content],
            comments: [(demo, "Looks good, one broken link to the imprint page.")]);
        Moved(footer, priya, "In progress", "Review", daysAgo: 2);
        Card(review, "Image pipeline: AVIF with WebP fallback", marco, 15, assignees: [marco, demo], due: -3, labels: [frontend, backend],
            description: "Build step generates AVIF and WebP at 3 widths. `<picture>` everywhere above the fold.");

        Card(done, "Pick the new type scale", emma, 40, assignees: [emma], due: -20, completed: true, labels: [design]);
        Card(done, "Audit current site analytics", priya, 38, assignees: [priya], due: -25, completed: true, labels: [content],
            checklist: ("Report", [("Top pages", true), ("Drop-off points", true), ("Share with team", true)]));
        Card(done, "Staging environment on the new host", marco, 33, assignees: [marco], due: -18, completed: true, labels: [backend]);
        Card(done, "Broken anchor links in the FAQ", demo, 10, assignees: [demo], due: -7, completed: true, labels: [bug]);
    }

    private void SeedMarketingBoard(Workspace ws, User demo, User priya, User emma)
    {
        var board = Board(ws, "Q1 marketing", "Campaigns and content for the first quarter.", priya, daysAgo: 35,
            "Ideas", "Planned", "Doing", "Published");
        var blog = Label(board, "Blog", "green");
        var social = Label(board, "Social", "pink");
        var email = Label(board, "Email", "blue");
        var (ideas, planned, doingCol, published) = (board.Columns[0], board.Columns[1], board.Columns[2], board.Columns[3]);

        Card(ideas, "Webinar with a customer panel", priya, 20);
        Card(ideas, "Year-in-review numbers post", emma, 12, labels: [blog, social]);
        Card(planned, "February newsletter", priya, 15, assignees: [priya], due: 8, labels: [email],
            checklist: ("Sections", [("Product updates", false), ("Customer story", false), ("Events", false)]));
        Card(planned, "LinkedIn series: behind the redesign", emma, 9, assignees: [emma, demo], due: 12, labels: [social]);
        Card(doingCol, "Blog: how we plan sprints", demo, 7, assignees: [demo], due: 1, labels: [blog],
            description: "Outline is in the shared drive. Aim for 1200 words, two diagrams.",
            comments: [(priya, "Happy to review the draft whenever it's ready.")]);
        Card(published, "January newsletter", priya, 30, assignees: [priya], due: -14, completed: true, labels: [email]);
        Card(published, "Launch announcement thread", emma, 28, assignees: [emma], due: -16, completed: true, labels: [social]);
    }

    private void SeedArchivedBoard(Workspace ws, User priya)
    {
        var board = Board(ws, "Old onboarding flow", "Replaced by the new signup flow in the relaunch.", priya, daysAgo: 57, "To do", "Done");
        board.ArchivedAt = _now.AddDays(-30);
        Card(board.Columns[1], "Welcome email sequence", priya, 50, completed: true);
    }

    private void SeedKitchenBoard(Workspace ws, User demo, User sam)
    {
        var board = Board(ws, "Kitchen", "Cabinets, worktop and a new floor before the spring.", demo, daysAgo: 19,
            "To do", "Waiting on others", "Done");
        var buy = Label(board, "To buy", "green");
        var trades = Label(board, "Trades", "orange");
        var (todo, waiting, done) = (board.Columns[0], board.Columns[1], board.Columns[2]);

        Card(todo, "Choose worktop material", demo, 15, assignees: [demo, sam], due: 3, labels: [buy],
            description: "Quartz vs. oak. Samples are in the hallway.",
            comments: [(sam, "Oak looks great but the quartz is much easier to keep clean.")]);
        Card(todo, "Measure for the new floor", sam, 10, assignees: [sam], due: -1);
        Card(waiting, "Electrician quote for extra sockets", demo, 12, assignees: [demo], due: 5, labels: [trades],
            checklist: ("Quotes", [("Spark & Co", true), ("Brightwire", false)]));
        Card(waiting, "Cabinet delivery date", sam, 8, labels: [trades]);
        Card(done, "Clear out the old cupboards", demo, 17, assignees: [demo, sam], due: -10, completed: true);
    }

    private User User(string email, string name, int daysAgo)
    {
        var user = new User { Id = Guid.NewGuid(), Email = email, DisplayName = name, CreatedAt = _now.AddDays(-daysAgo) };
        user.PasswordHash = hasher.HashPassword(user, DemoPassword);
        db.Users.Add(user);
        return user;
    }

    private Workspace Workspace(string name, int daysAgo, params (User User, WorkspaceRole Role)[] members)
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = name, CreatedAt = _now.AddDays(-daysAgo) };
        foreach (var (user, role) in members)
        {
            workspace.Members.Add(new WorkspaceMember { UserId = user.Id, Role = role, JoinedAt = _now.AddDays(-daysAgo) });
        }

        db.Workspaces.Add(workspace);
        return workspace;
    }

    private Board Board(Workspace ws, string name, string description, User creator, int daysAgo, params string[] columns)
    {
        var board = new Board
        {
            Id = Guid.NewGuid(),
            WorkspaceId = ws.Id,
            Name = name,
            Description = description,
            CreatedAt = _now.AddDays(-daysAgo),
            Columns = [.. columns.Select((c, i) => new BoardColumn { Id = Guid.NewGuid(), Name = c, Position = i })],
        };
        db.Boards.Add(board);
        Log(board.Id, null, creator, ActivityTypes.BoardCreated, board.CreatedAt, new() { ["board"] = name });
        return board;
    }

    private Label Label(Board board, string name, string color)
    {
        var label = new Label { Id = Guid.NewGuid(), BoardId = board.Id, Name = name, Color = color };
        db.Labels.Add(label);
        return label;
    }

    private Card Card(
        BoardColumn column,
        string title,
        User creator,
        int daysAgo,
        User[]? assignees = null,
        int? due = null,
        bool completed = false,
        Label[]? labels = null,
        string description = "",
        (string Title, (string Text, bool Done)[] Items)? checklist = null,
        (User Author, string Body)[]? comments = null)
    {
        var created = _now.AddDays(-daysAgo).AddHours(-3);
        var position = (db.ChangeTracker.Entries<Card>().Count(e => e.Entity.ColumnId == column.Id) + 1) * 1024L;
        var card = new Card
        {
            Id = Guid.NewGuid(),
            BoardId = column.BoardId,
            ColumnId = column.Id,
            Title = title,
            Description = description,
            Position = position,
            DueDate = due is { } d ? _today.AddDays(d) : null,
            CompletedAt = completed ? _now.AddDays(-Math.Max(1, daysAgo / 3)) : null,
            CreatedById = creator.Id,
            CreatedAt = created,
            UpdatedAt = created,
        };
        db.Cards.Add(card);
        Log(card.BoardId, card.Id, creator, ActivityTypes.CardCreated, created, new() { ["title"] = title, ["column"] = column.Name });

        foreach (var user in assignees ?? [])
        {
            db.CardAssignees.Add(new CardAssignee { CardId = card.Id, UserId = user.Id });
            Log(card.BoardId, card.Id, creator, ActivityTypes.CardAssigned, created.AddMinutes(5), new() { ["title"] = title, ["user"] = user.DisplayName });
        }

        foreach (var label in labels ?? [])
        {
            db.CardLabels.Add(new CardLabel { CardId = card.Id, LabelId = label.Id });
        }

        if (checklist is { } list)
        {
            db.Checklists.Add(new Checklist
            {
                Id = Guid.NewGuid(),
                CardId = card.Id,
                Title = list.Title,
                Items = [.. list.Items.Select((item, i) => new ChecklistItem { Id = Guid.NewGuid(), Text = item.Text, IsDone = item.Done, Position = i })],
            });
        }

        var commentTime = created.AddHours(2);
        foreach (var (author, body) in comments ?? [])
        {
            commentTime = commentTime.AddHours(5);
            db.Comments.Add(new Comment { Id = Guid.NewGuid(), CardId = card.Id, AuthorId = author.Id, Body = body, CreatedAt = commentTime });
            Log(card.BoardId, card.Id, author, ActivityTypes.CommentAdded, commentTime, new() { ["title"] = title, ["excerpt"] = body });
        }

        if (card.CompletedAt is { } completedAt)
        {
            Log(card.BoardId, card.Id, creator, ActivityTypes.CardCompleted, completedAt, new() { ["title"] = title });
        }

        return card;
    }

    private void Moved(Card card, User actor, string from, string to, int daysAgo) =>
        Log(card.BoardId, card.Id, actor, ActivityTypes.CardMoved, _now.AddDays(-daysAgo), new() { ["title"] = card.Title, ["from"] = from, ["to"] = to });

    private void Log(Guid boardId, Guid? cardId, User actor, string type, DateTime at, Dictionary<string, string> data) =>
        db.Activities.Add(new Domain.Activity
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            CardId = cardId,
            ActorId = actor.Id,
            Type = type,
            Data = data,
            CreatedAt = at,
        });

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo data already present, skipping seed")]
    private partial void LogSkipped();

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo data; sign in as {Email}")]
    private partial void LogSeeded(string email);
}
