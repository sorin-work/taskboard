# taskboard

Kanban boards for small teams. Put your team in a workspace, give everyone a
role, and keep work moving across boards with cards, checklists, comments and
due dates. "My tasks" collects everything assigned to you in one list.

## Features

- **Workspaces and roles**: owners, admins and members; invite people by email
  with a link that expires after seven days
- **Boards**: as many as you like per workspace, with your own columns;
  archive the ones you're done with
- **Cards**: drag and drop within and between columns, with a stable order
  that survives reloads; description, assignees, labels, due date, checklists
  and comments
- **Activity**: every board and every card keeps a log of who did what
- **Filters**: narrow a board down by assignee, label, due date or text; the
  filters live in the URL so you can share a filtered view
- **My tasks**: cards assigned to you across all boards, grouped into overdue,
  today, the next few days, later and no due date

## Running with Docker

```bash
cp .env.example .env   # optional, only needed to change the defaults
docker compose up --build
```

Open http://localhost:5184 and sign in with the demo account:

- **Email:** `demo@example.com`
- **Password:** `demo-password`

| Service | URL / port                  | Notes                                  |
| ------- | --------------------------- | -------------------------------------- |
| web     | http://localhost:5184       | nginx; proxies `/api` to the api       |
| api     | http://localhost:4404/api   | ASP.NET Core; health at `/api/health`  |
| db      | `localhost:5444`            | Postgres 17, user/password/db `taskboard` |

The api applies migrations on start. Set `SEED_DEMO=false` to start with an
empty database. `docker compose down -v` removes the data volume.

## Running locally without Docker

You need the .NET 9 SDK, Node 22 and Postgres (`docker compose up -d db` is
the easiest way to get one on port 5444).

```bash
cd api
dotnet tool restore
dotnet ef database update -p src/Taskboard.Api
SEED_DEMO=true dotnet run --project src/Taskboard.Api   # http://localhost:4404
```

```bash
cd web
npm install
npm run dev                                            # http://localhost:5184
```

## Tests

```bash
cd api && dotnet test
cd web && npm test
```
