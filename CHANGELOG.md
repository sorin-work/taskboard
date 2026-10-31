# Changelog

## 1.0.0 (2026-10-31)

First release that I'd hand to another team.

### Workspaces and people

- Workspaces with owner, admin and member roles; the last owner can't leave
- Invitations by email link (seven days, one use, replaced by re-inviting)
- Members page with roles, pending invitations and each person's workload

### Boards

- Boards with your own columns, optional WIP limits and coloured labels
- Drag and drop cards with a stable order; Alt + arrow keys for keyboards
- Filters for assignee, label, due date and text, kept in the url, plus
  quick filters for my cards, due this week and overdue
- Star boards, archive boards, copy a board with or without its cards
- Collapse columns; export a board as CSV
- Board activity feed

### Cards

- Description with clickable links, assignees, labels, due date, completion
- Checklists you can reorder and rename, comments, per-card activity
- Archive and restore, duplicate

### Across boards

- My tasks grouped by due date, with an overdue count in the top bar
- Search titles and descriptions across a workspace

### Running it

- Docker compose for Postgres, the api and the web app; migrations and the
  demo seed run on start
- Rate limited sign in and sign up, request ids, JSON logs in production,
  OpenAPI document in development
