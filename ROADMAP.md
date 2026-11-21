# Roadmap

Things I might add to taskboard. Since 1.0.0 the boards cover what a small
team needs day to day (workspaces, roles, boards, cards, checklists, comments,
my tasks, search), so nothing here is promised or scheduled. These are ideas,
written down with enough detail that I (or anyone else) can pick one up later
without starting from zero. Items may change, move between groups or be
dropped.

What the app does today is in the [README](README.md).

- **Next**: small or well understood. I would start with these.
- **Later**: worth doing, but bigger or waiting on a decision.
- **Maybe**: not sure yet that they are worth the extra complexity.

Sizes are rough: **S** is an evening or two, **M** a few evenings up to a
couple of weekends, **L** several weeks of spare time.

Each item is meant to fit in one pull request and to be covered by the tests.
There are two test suites and no browser tests. `api/tests/Taskboard.Api.Tests`
calls the real endpoints through `WebApplicationFactory` on SQLite, one file
per area under `Features/`, with `PermissionMatrixTests` covering who may do
what. `web` runs Vitest with Testing Library: components are tested next to
themselves, and plain logic lives in `web/src/lib` with its own tests. So
logic worth testing on the web side (ordering, dates, filters, text) belongs
in `lib/`, not inside a component, and on the api side in a small static
class next to the feature (like `CardPositions` or `CardFilter`), not inside
an endpoint. Schema changes come with an EF Core migration under
`api/src/Taskboard.Api/Data/Migrations`. File names below are relative to
`api/src/Taskboard.Api/` when they are api code and to `web/src/` when they
are web code.

"Depends on" lists real prerequisites only. Items that merely touch the same
file are not listed there; the bigger overlaps are mentioned in the items.

When an item ships, its row in the Overview says `Shipped` in the Group column
and its section moves under a `## Shipped` heading at the end, shortened to one
paragraph on what was built and where it differs from the text here. The same
pull request updates the README and adds an entry under `## Unreleased` in the
CHANGELOG.

## Overview

| #   | Item                                        | Group | Area          | Size | Depends on |
| --- | ------------------------------------------- | ----- | ------------- | ---- | ---------- |
| 1   | Signing out that actually signs out         | Next  | auth          | M    | -          |
| 2   | Card edits that do not overwrite each other | Next  | cards         | M    | -          |
| 3   | Catch missing migrations in CI              | Next  | ops           | S    | -          |
| 4   | Invitation emails over SMTP                 | Next  | invitations   | M    | -          |
| 5   | Boards that update by themselves            | Next  | boards        | M    | -          |
| 6   | Password reset                              | Later | auth          | S    | 1, 4       |
| 7   | Api tests on PostgreSQL                     | Later | tests         | M    | -          |
| 8   | Search that scales and finds comments       | Later | search        | M    | 7          |
| 9   | Move a card to another board                | Later | cards         | M    | -          |
| 10  | Mentions and notifications                  | Later | people        | L    | -          |
| 11  | Due date reminders by email                 | Later | people        | M    | 4          |
| 12  | Markdown in descriptions and comments       | Later | cards         | M    | -          |
| 13  | Attachments                                 | Later | cards         | L    | -          |
| 14  | Start dates and a calendar view             | Later | boards        | M    | -          |
| 15  | Table view of a board                       | Later | boards        | M    | -          |
| 16  | Card templates                              | Later | cards         | S    | -          |
| 17  | Recurring cards                             | Later | cards         | M    | -          |
| 18  | Blocked by: links between cards             | Later | cards         | M    | -          |
| 19  | Bulk actions on cards                       | Later | boards        | M    | -          |
| 20  | Import from Trello and CSV                  | Later | import/export | M    | -          |
| 21  | Calendar feed of my tasks                   | Later | people        | S    | -          |
| 22  | Personal api tokens                         | Later | api           | M    | -          |
| 23  | Webhooks                                    | Later | api           | M    | -          |
| 24  | Delete my account and export my data        | Later | auth          | M    | -          |
| 25  | Read-only members                           | Later | workspaces    | M    | -          |
| 26  | Private boards                              | Later | workspaces    | L    | -          |
| 27  | Screen reader announcements on the board    | Later | ui            | M    | -          |
| 28  | Rate limits beyond sign in                  | Later | ops           | S    | -          |
| 29  | Backups and restore                         | Later | ops           | S    | -          |
| 30  | Metrics                                     | Later | ops           | S    | -          |
| 31  | Session cookie instead of a stored token    | Later | auth          | M    | 1          |
| 32  | Swimlanes                                   | Maybe | boards        | M    | -          |
| 33  | Custom fields                               | Maybe | cards         | L    | 15         |
| 34  | Single sign-on with OpenID Connect          | Maybe | auth          | L    | -          |
| 35  | Two-factor sign-in                          | Maybe | auth          | M    | 1          |
| 36  | Large boards                                | Maybe | performance   | L    | -          |
| 37  | Time tracking                               | Maybe | cards         | M    | -          |

## Next

### 1. Signing out that actually signs out

Sign in returns a JWT that is valid for `Jwt__LifetimeHours` (a week by
default). The token carries only the user id, email and name
(`Auth/TokenService.cs`) and nothing on the server can take it back:

- "Sign out" in the web app removes the token from `localStorage`
  (`api/client.ts`). A copy of it, on another device or in someone's
  hands, keeps working for the rest of the week.
- Changing the password (`POST /auth/me/password`) sets a new hash and
  nothing else. Every existing token stays valid, which is exactly the
  case where someone changes the password because they think it leaked.
- The token holds the email and name at sign in; after `PATCH /auth/me`
  they are stale until the next sign in. Nothing reads them for decisions
  today, so that is cosmetic.

Plan:

- A `SecurityStamp` (a random string) on `User`, put into the token as a
  claim. Changing the password, "sign out everywhere" and later a password
  reset (item 6) replace it.
- Check it in `JwtBearerEvents.OnTokenValidated` in
  `Auth/AuthServiceExtensions.cs`: load the stamp and reject the token when
  it differs. That is one small query per request; cache it in memory for
  a minute per user with `IMemoryCache` to keep it cheap, and drop the
  cache entry when the stamp changes on this instance.
- `POST /auth/logout-everywhere` and a button on the settings page.
- Shorter tokens are the other half: a day instead of a week, renewed by
  the web app when it is more than half way through. That stays inside the
  current design (no refresh tokens) and limits a leaked token to a day
  even when nobody presses anything.
- Tricky: the change-password call must hand back a new token, or the
  person who just changed it is signed out too.
- Tests: in `AuthTests.cs`, an old token is refused after a password
  change and after "sign out everywhere", and the new one works.

### 2. Card edits that do not overwrite each other

`PUT /cards/{id}` replaces the title, description, due date and completion
(`UpdateCardRequest` in `Features/Cards/CardDtos.cs`). Two people with the
same card open both save, and the second one silently wins: one person's
new description is gone, and the activity log says both edited it. Also,
`Completed` defaults to `false`, so a client that sends only a title
reopens a completed card.

- Optimistic concurrency: the card DTO carries a version, and the update
  sends it back. On PostgreSQL the `xmin` system column can be the
  concurrency token (`UseXminAsConcurrencyToken` in the Npgsql provider);
  SQLite in the tests has no `xmin`, so a plain `Version` integer bumped in
  `SaveChanges` works on both and keeps the tests honest. I lean towards
  the integer.
- A stale version answers `409` with the current card. `CardModal.tsx`
  then shows "Someone changed this card while you were editing" with both
  versions of the description and lets the person pick or merge by hand.
- Make the update partial: `PATCH` with only the fields that changed, so
  two people editing different fields (one the due date, one the
  description) do not conflict at all. Keep `PUT` for a release for other
  clients.
- Checklists and comments have their own endpoints and rows and do not
  have this problem.
- Tests: `CardTests.cs` for a stale update, a partial update, and the
  completion flag; a component test for the conflict message.

### 3. Catch missing migrations in CI

The README says it plainly: the tests build their SQLite schema from the
model, so they pass without a migration, and CI does not check that one
exists. A forgotten migration shows up only when the container starts on a
real database and the api fails on the first query that needs the new
column.

- EF Core 9 has `dotnet ef migrations has-pending-model-changes`, which
  exits non-zero when the model and the last snapshot differ. Add it as a
  step in the `api` job of `.github/workflows/ci.yml`, after the build.
- The `dotnet-ef` tool is already in `api/.config/dotnet-tools.json`, so
  `dotnet tool restore` is all it needs.
- Update the paragraph in the README that says CI does not check.
- Tests: the CI step is the test. Try it once on a branch with a model
  change and no migration to see it fail.

### 4. Invitation emails over SMTP

Inviting someone creates the link and hands it to `IInvitationNotifier`.
The only implementation, `LoggingInvitationNotifier` in
`Features/Invitations/InvitationNotifier.cs`, writes it to the log. The
inviter has to copy the link from the members page and send it themselves.
The interface is already there for a real sender.

- `SmtpInvitationNotifier` using MailKit, configured with `Smtp__Host`,
  `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__From` and
  `Smtp__UseTls`. Without `Smtp__Host` the logging notifier stays, so
  nothing changes for people who do not set it up.
- A plain text email with the workspace name, the inviter's name, the role
  and the link from `App__PublicUrl`.
- A mailpit service in `docker-compose.yml` for the demo, on a free port,
  so invitations can be seen without a real mail server.
- Tricky: a slow or broken mail server should not fail the invitation.
  Create the invitation first, then send; on failure answer success with a
  flag so the members page says "The email could not be sent; copy the
  link instead".
- Tests: a recording notifier in the test factory; a unit test for the
  message text.

### 5. Boards that update by themselves

The web app fetches the board once and keeps it for 15 seconds
(`staleTime` in `queryClient.ts`), without refetching on window focus. When
Priya moves a card, Marco sees it only after a reload or after he changes
something himself. On a board that several people work on at once, people
drag cards that are no longer where they see them, and the api refuses the
move with "The card to place this one after is not in the target column".

- Server-sent events: `GET /boards/{id}/events` streams small messages
  ("card changed", "column changed", with ids) to members of the board's
  workspace. The web app invalidates the matching TanStack Query keys
  (`lib/queryKeys.ts`) and refetches.
- The api publishes after `SaveChanges`. `ActivityLog` already records every
  change in the same unit of work, so hooking there covers all endpoints
  without touching each one.
- One api instance: an in-memory broadcaster is enough. Several instances
  would need PostgreSQL `LISTEN/NOTIFY`; write the broadcaster behind an
  interface so that can come later.
- Tricky: `EventSource` cannot send an `Authorization` header. Either a
  short-lived ticket from a normal request in the query string, or the
  cookie from item 31. The ticket keeps this item independent.
- Tricky: nginx buffers responses. `web/nginx.conf` needs
  `proxy_buffering off` for the events path.
- Tricky: a drag in progress. Refetching while someone holds a card must
  not yank it away; defer invalidations until the drop.
- Undecided: refetch on window focus as a cheap first step. It fixes the
  "I came back to the tab" case alone, and I would turn it on in this item
  regardless.
- Tests: api tests that a change sends an event to a member and not to an
  outsider; a web test for the invalidation logic as a function in `lib/`.

## Later

### 6. Password reset

There is no way back from a forgotten password. An owner can invite the
person again under another address, which loses their assignments.

- `POST /auth/password-reset` sends a link with a random token (stored as
  a hash, like invitation tokens), valid for an hour and once.
- The answer is the same for known and unknown addresses, and the endpoint
  sits behind the auth rate limit.
- Setting the new password replaces the security stamp from item 1, so old
  sessions end.
- Needs the SMTP sender from item 4. Without SMTP the "forgot password"
  link is hidden.
- Tests: `AuthTests.cs` for the full flow, an expired token and a reused
  one.

### 7. Api tests on PostgreSQL

The api tests run on SQLite and production runs on PostgreSQL. The two
differ where it hurts: case sensitivity of `LIKE` and `ToLower`, date and
time types, `DateOnly` handling, transaction isolation and concurrency
tokens (item 2). A query that works in the tests can fail or behave
differently in production.

- Make the test factory (`Infrastructure/` in the test project) choose the
  provider from an environment variable, and run the suite a second time in
  CI against a PostgreSQL service container.
- Keep SQLite as the default for local runs; it is fast and needs nothing.
- Each test class gets its own database or schema so tests can still run
  in parallel.
- Tests: this item is the tests. Expect a few to fail at first; fix the
  code, not the tests.

### 8. Search that scales and finds comments

Search (`Features/Search/SearchEndpoints.cs`) runs
`c.Title.ToLower().Contains(term) || c.Description.ToLower().Contains(term)`
over every card in the workspace. That is a sequential scan with no index,
it does not look at comments or checklist items, and a typo finds nothing.

- PostgreSQL full-text search: a generated `tsvector` column over title and
  description with a GIN index, plus comments and checklist items through
  a join or a separate index.
- `pg_trgm` for typos and partial words, if the full-text ranking alone
  feels too strict.
- SQLite has neither, so this needs the PostgreSQL test run from item 7;
  keep the current `Contains` as the SQLite fallback so local tests still
  run.
- Results show where the match is (title, description, comment) with a
  short highlighted snippet.
- Tests: on PostgreSQL, ranking and comment matches; the existing
  `SearchTests.cs` on both.

### 9. Move a card to another board

`MoveAsync` in `Features/Cards/CardEndpoints.cs` refuses a target column on
another board ("The target column is not on this card's board."). Work that
belongs to another team's board has to be retyped there, losing its
comments, checklists and history.

- A "Move to board" action in the card dialog: pick a board in the same
  workspace and a column on it.
- Labels are per board. Keep labels with the same name and colour on the
  target, drop the others and say so in the activity entry.
- Activity entries stay with the card; the old board's feed gets a "moved
  to Q1 marketing" entry.
- Tricky: `Card.BoardId` is used by filters, export and permission checks.
  Moving between workspaces is a different matter (members, assignees) and
  stays out.
- Tests: `CardTests.cs` for labels kept and dropped, assignees kept,
  activity on both boards, and a board in another workspace refused.

### 10. Mentions and notifications

Comments reach nobody. Assigning a card to someone does not tell them,
except through the overdue count in the top bar. People learn about work
by opening boards.

- `@name` in comments, completed from the workspace members in
  `CommentsSection.tsx`, stored as user ids.
- A `Notification` entity (user, kind, card, read at) written for
  mentions, assignments and comments on cards you are assigned to, in the
  same `SaveChanges` as the change, like the activity log.
- A bell in the top bar with an unread count (the layout already polls
  every five minutes) and a list that marks items read.
- Optionally email for notifications, with the sender from item 4 and a
  per user setting.
- Tricky: removed members. `RemoveAsync` in `MemberEndpoints.cs` removes
  their assignments; their notifications for that workspace should go too.
- Tests: api tests for each kind and for not notifying yourself; component
  tests for the bell.

### 11. Due date reminders by email

My tasks shows what is due, but only to people who open it.

- A daily email at a chosen hour with what is overdue and due today, per
  user, only when there is something.
- A `BackgroundService` in the api that wakes up every few minutes and
  sends the reminders that are due; a `last_reminded_on` per user so a
  restart does not send twice.
- Due dates are calendar dates without a zone, and the server does not
  know the user's zone. Store a time zone per user (taken from the browser
  on sign in) to pick both the hour and "today".
- Tests: unit tests for "whose reminder is due now"; an api test with a
  fake time provider (`TimeProvider` is already injected everywhere).

### 12. Markdown in descriptions and comments

Descriptions and comments are plain text with clickable links
(`lib/linkify.ts`). Lists, headings, code and bold text show as raw
characters.

- Render Markdown on the web side with a small parser (markdown-it or
  micromark) and sanitise the output with DOMPurify. The api keeps storing
  text, so search and CSV export stay plain.
- Task list syntax (`- [ ]`) could tick boxes in place, but checklists
  already exist; leave it out at first.
- Tricky: existing text was written as plain text. Lines with leading
  `#` or `*` will suddenly render differently; acceptable.
- Tests: `lib/` tests for the rendering wrapper, including a script tag and
  a `javascript:` link that must not survive.

### 13. Attachments

Cards cannot hold a screenshot or a document, which is the first thing
people miss when they come from Trello.

- Files stored on a volume mounted into the api container, behind an
  `IFileStore` interface so an S3-compatible store (MinIO, self-hosted)
  can come later.
- Upload limits per file and per workspace, set in configuration.
- Downloads go through the api with the same membership check as the
  card, never as public URLs. Images get a thumbnail on the card tile.
- Tricky: deleting a card, board or workspace must delete its files;
  database cascades do not reach the disk. A cleanup pass for orphaned
  files on start covers crashes in between.
- Tricky: served files must not run in the app's origin. Send
  `Content-Disposition: attachment` for everything but images, and
  `X-Content-Type-Options: nosniff`.
- Tests: api tests with a temporary folder for upload, download,
  permissions and deletion.

### 14. Start dates and a calendar view

Cards have a due date only. Work that spans a week (a campaign, a
renovation step) cannot be shown as a span, and there is no way to see a
month at a glance.

- `StartDate` on `Card`, optional, not after the due date.
- A calendar view per board (month and week) that shows cards on their
  due date, or as bars from start to due. Dragging a card to another day
  changes its dates.
- Uses the same filters as the board (`lib/filters.ts`), kept in the URL.
- Tests: `lib/` tests for laying out spans in weeks; api validation tests.

### 15. Table view of a board

A board with eighty cards is hard to scan as columns. A table with one row
per card (title, column, assignees, labels, due date, checklist progress)
and sorting is the usual second view.

- A view switch on the board page, same filters and URL.
- Inline edits for due date and column only; everything else opens the
  card dialog.
- The data is already loaded by `useBoardData.ts`; no new endpoint.
- Tests: component tests for sorting and the column change.

### 16. Card templates

Teams create the same card again and again (a bug report with its
checklist, an onboarding task). "Duplicate" exists, but it copies a real
card and its history of edits.

- Mark a card as a template; templates are hidden from the board and
  appear in the "Add card" form.
- Creating from a template copies title, description, labels and
  checklists, like `POST /cards/{id}/duplicate` does.
- Tests: api tests for creating from a template; templates excluded from
  filters, export and my tasks.

### 17. Recurring cards

"Pay the invoice every month" and "weekly review" are a large share of a
small team's cards.

- A recurrence on a card (daily, weekly on a day, monthly on a day). When
  it is completed, a copy appears in the first column with the next due
  date.
- Completion is the trigger, not a clock, so no background job is needed.
- Tricky: un-completing a card should not create a second copy. Remember
  the copy's id on the original.
- Tests: unit tests for the next date (month ends, leap years); an api
  test for complete, uncomplete, complete.

### 18. Blocked by: links between cards

"This cannot start before that is done" lives in comments today.

- Links between cards in the same workspace: blocks / blocked by, and a
  plain "related".
- The card tile shows a small marker when an open card blocks it.
- Tricky: deleted and archived cards. A link to an archived card stays
  visible with the card struck through; a deleted card removes the link.
- Tests: api tests for linking, permissions across workspaces, and
  deletion.

### 19. Bulk actions on cards

Archiving a finished column or moving ten cards to next month's board is
one card at a time.

- Select cards on the board (shift-click, or checkboxes in the table view
  of item 15) and move, archive, label or assign them together.
- "Archive all cards in this column" in the column menu, which is the most
  common case on its own.
- One request per action with a list of ids, one transaction, one activity
  entry per card.
- Tests: api tests for each action and for a list that contains a card
  from another board.

### 20. Import from Trello and CSV

Teams switching from Trello have a JSON export; others have a spreadsheet.

- Trello JSON import into a new board: lists, cards, labels, checklists,
  comments (as text with the original author's name), due dates.
- CSV import with the same columns as the existing export
  (`Features/Export/`), so an export can be edited and imported again.
- A preview of what will be created before committing.
- Tests: fixture files for both formats; api tests for the import result.

### 21. Calendar feed of my tasks

Many people live in their calendar app, not in the task list.

- A personal iCal URL with a secret token (revocable) that lists the cards
  assigned to me with a due date, as all-day events.
- `GET /me/tasks.ics?token=...`, outside the bearer auth, with its own
  token check.
- Tests: an api test that parses the feed; a revoked token is refused.

### 22. Personal api tokens

The api is complete enough for scripts (create cards from a form, a weekly
report), but the only credential is the sign-in token, which expires and
carries the password's power.

- Tokens on the settings page, with a name and an expiry, shown once and
  stored as a hash. Read-only or full access.
- Sent as `Authorization: Bearer` like the JWT; the auth handler tells
  them apart by a prefix.
- Endpoints that change the account itself (password, delete) refuse api
  tokens.
- Tests: api tests for each scope and for a revoked token.

### 23. Webhooks

A board posting to a chat channel when a card is done, or a script reacting
to new cards, needs webhooks.

- Per board: URL, secret, event types (card created, moved, completed,
  commented). Signed with HMAC-SHA256.
- Built on the same "after save" hook as item 5 if that has shipped.
- Deliveries in a table with retries from a `BackgroundService`, so a slow
  receiver never slows a request down, and a delivery log in board
  settings.
- Tests: signature unit tests; delivery tests with a fake handler.

### 24. Delete my account and export my data

There is no way to leave. Several foreign keys make it harder than one
`DELETE`: `Card.CreatedById` is `Restrict`, `Comment.AuthorId` and
`Activity.ActorId` cascade, so deleting a user as configured today would
either fail or wipe other people's history.

- Delete account: refuse while you are the last owner of a workspace (the
  rule that already stops the last owner leaving). Otherwise keep the
  person's comments and activity under a "Deleted user" placeholder:
  change the deletes to `SetNull` with nullable columns, or point them at a
  placeholder row. I lean towards nullable columns.
- Export: a JSON file with the account, memberships, assigned cards and
  your comments.
- Tests: api tests for both, including the last owner rule and that other
  people's view of a board is unchanged after the deletion.

### 25. Read-only members

The roles are owner, admin and member, and members can change everything on
every board. Clients, stakeholders and bosses who only want to look need a
full member seat.

- A `Viewer` value in `WorkspaceRole`. `WorkspaceAccess` grows a "may
  write" check, and every write endpoint asks for it.
- `PermissionMatrixTests` is the place to prove it: one row per endpoint
  and role.
- The web app hides edit controls for viewers and turns off drag and drop.
- Tests: the permission matrix extended; component tests for the hidden
  controls.

### 26. Private boards

Every member of a workspace sees every board. HR or a hiring board cannot
live next to the team's boards.

- A board can be restricted to chosen members; admins and owners always
  see it.
- Every query that lists or searches cards across boards (search, my tasks,
  workload, board list) must apply the restriction. That is the risky part:
  my tasks and workload count cards across all boards today.
- Tests: the permission matrix for a restricted board; search, my tasks and
  workload tests that a hidden board's cards do not appear.

### 27. Screen reader announcements on the board

Cards can be moved with `Alt` and the arrow keys (`lib/keyboardMove.ts`),
which is good. But nothing says where the card went: there is no
`aria-live` region anywhere in the web app, so a screen reader user moves a
card and hears nothing.

- One polite live region on the board page that says "Moved 'Fix footer'
  to Review, position 2 of 5" after a keyboard or drag move, and the
  equivalent for archive and restore.
- Column headers announce their WIP limit state ("over the limit").
- Check the card dialog's focus handling: focus into the dialog on open,
  back to the card on close.
- Tests: component tests that assert the live region text after a move.

### 28. Rate limits beyond sign in

The only rate limit is the `auth` policy on sign in and sign up
(`Common/RateLimiting.cs`). A signed-in account, or a script with a stolen
token, can create invitations without limit, and each one sends an email
once item 4 has shipped.

- A per user policy for invitations (say 20 an hour) and a general per
  user limit on writes that is high enough never to bother a person.
- Partition by user id from the token, falling back to the address.
- Tests: extend `RateLimitTests.cs`.

### 29. Backups and restore

Everything is in PostgreSQL, but the README does not mention backups, and
`docker compose down -v` is one flag away from losing it all.

- A short `docs/operations.md`: `pg_dump` from the db container, restore
  into an empty database, and what an upgrade does (migrations on start,
  no way back to an older image once a migration ran).
- An optional backup service in `docker-compose.yml` behind a compose
  profile, running `pg_dump` daily into a host folder and keeping a week.
- Tests: none at the repo's levels; try a restore by hand and note it in
  the PR.

### 30. Metrics

There is a health endpoint and JSON logs, nothing to graph.

- OpenTelemetry metrics with the Prometheus exporter at `/api/metrics`:
  request rate and duration per route (ASP.NET Core provides them), plus a
  few of our own (cards created, moves, failed sign ins).
- Off by default, and nginx should not expose it publicly.
- Tests: an api test that the endpoint answers when enabled and 404s when
  not.

### 31. Session cookie instead of a stored token

The JWT lives in `localStorage`, where any script running on the page can
read it. The app escapes everything and has no third-party scripts, so
this is defence in depth, not a known hole. But one XSS (for example in
Markdown rendering, item 12) would hand out week-long tokens.

- An `HttpOnly`, `Secure`, `SameSite=Lax` cookie set by sign in, which the
  api accepts next to the bearer header. nginx already serves the app and
  the api from one origin, so no CORS changes are needed.
- Cookies bring CSRF: require a custom header on writes, which a
  cross-site form cannot send.
- Builds on item 1, so the cookie can be revoked too.
- Tests: api tests for cookie auth and the CSRF header.

## Maybe

### 32. Swimlanes

Rows across the columns, by assignee or by label, so a board shows who is
doing what at a glance. Useful on busy boards, and a lot of layout and drag
and drop work for the board components. The workload page and the
assignee filter cover part of the need already.

### 33. Custom fields

Per board fields (a number, a choice, a date, a link) shown on the card and
in the table view of item 15. Popular in bigger tools, and the point where a
simple board starts to look like the "full project management suite" the
README says this is not. I would only build it with a concrete request.

### 34. Single sign-on with OpenID Connect

Companies with Keycloak, Authentik or another identity provider want people
to sign in with it. ASP.NET Core has the OpenID Connect handler built in;
the work is account linking by email, what happens to the password, and
invitations for people who have no password at all.

### 35. Two-factor sign-in

TOTP with recovery codes. Self-contained and well understood, and only
meaningful once tokens can be revoked (item 1); without that a stolen token
skips the second factor anyway.

### 36. Large boards

`GET /boards/{id}/cards` returns every open card, and the board renders all
of them. Fine at a few hundred. At a few thousand (a support board that is
never cleaned up) both the query and the render get slow. Paging per
column and a virtualised column list would fix it, at the cost of making
drag and drop and filters aware of cards that are not loaded. I would
measure with generated data first.

### 37. Time tracking

Log time against a card, see totals per card and per person. Some small
teams bill by the hour and would use it daily; others never. It touches
cards, the workload page and the export, and needs its own reports to be
worth anything.
