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
- **WIP limits**: optional per column; the column turns red when it's over
- **Copy a board**: reuse a board's columns and labels (and optionally its
  cards) for the next sprint or month
- **Activity**: every board and every card keeps a log of who did what
- **Filters**: narrow a board down by assignee, label, due date or text; the
  filters live in the URL so you can share a filtered view
- **My tasks**: cards assigned to you across all boards, grouped into overdue,
  today, the next few days, later and no due date; the top bar shows how many
  are overdue
- **Small touches**: dark theme that follows your system, `/` to search a
  board, and a layout that works on a phone

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

## API overview

Everything lives under `/api` and speaks JSON. Errors come back as
[problem details](https://www.rfc-editor.org/rfc/rfc7807) with a `detail`
message, and validation errors list each field under `errors`. All routes
except sign up, sign in, health and invitation previews need an
`Authorization: Bearer <token>` header.

| Area        | Routes                                                                                   |
| ----------- | ---------------------------------------------------------------------------------------- |
| Auth        | `POST /auth/signup`, `POST /auth/login`, `GET/PATCH /auth/me`, `POST /auth/me/password`   |
| Workspaces  | `GET/POST /workspaces`, `GET/PATCH/DELETE /workspaces/{id}`                              |
| Members     | `GET /workspaces/{id}/members`, `PATCH/DELETE /workspaces/{id}/members/{userId}`          |
| Invitations | `GET/POST /workspaces/{id}/invitations`, `DELETE .../{invitationId}`, `GET /invitations/{token}`, `POST /invitations/{token}/accept` |
| Boards      | `GET/POST /workspaces/{id}/boards`, `GET/PATCH/DELETE /boards/{id}`                      |
| Columns     | `POST /boards/{id}/columns`, `PATCH/DELETE /columns/{id}`, `POST /columns/{id}/move`     |
| Cards       | `GET /boards/{id}/cards`, `POST /columns/{id}/cards`, `GET/PUT/DELETE /cards/{id}`, `POST /cards/{id}/move`, `PUT /cards/{id}/labels`, `PUT /cards/{id}/assignees` |
| Labels      | `GET/POST /boards/{id}/labels`, `PATCH/DELETE /labels/{id}`                              |
| Checklists  | `POST /cards/{id}/checklists`, `PATCH/DELETE /checklists/{id}`, `POST /checklists/{id}/items`, `PATCH/DELETE /checklist-items/{id}` |
| Comments    | `GET/POST /cards/{id}/comments`, `PATCH/DELETE /comments/{id}`                           |
| Activity    | `GET /boards/{id}/activity?before=&limit=`, `GET /cards/{id}/activity`                   |
| My tasks    | `GET /me/tasks?due=&includeCompleted=`                                                   |

`GET /boards/{id}/cards` and `GET /me/tasks` take the same filters:
`assignee` (a user id, `me` or `none`), `label` (a label id), `due`
(`overdue`, `today`, `week` or `none`) and `q` (text in the title).

Roles: members can do everything with cards, columns, labels and comments;
admins can also invite people, change roles, and archive or delete boards;
owners can additionally delete the workspace and promote other owners.

## Tests

```bash
cd api && dotnet test
cd web && npm test
```
