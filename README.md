# DevPulse

**DevPulse** is a developer performance and reporting platform built with **.NET 10**, **Blazor WebAssembly**, and **Clean Architecture**. It integrates multiple **ClickUp** workspaces via API tokens and is designed to extend with **Cursor** analytics for unified monthly KPI dashboards.

## Solution Structure

```
DevPulse/
├── src/
│   ├── DevPulse.Domain/           # Entities, enums (core business model)
│   ├── DevPulse.Shared/           # DTOs, Result pattern, API contracts
│   ├── DevPulse.Application/      # Use cases, interfaces, application services
│   ├── DevPulse.Infrastructure/   # EF Core, ClickUp API client, repositories
│   ├── DevPulse.Server/           # ASP.NET Core Web API + Blazor host
│   └── DevPulse.Client/           # Blazor WebAssembly UI
└── DevPulse.sln
```

## Architecture & Design Patterns

| Pattern | Usage |
|---------|--------|
| **Clean Architecture** | Domain → Application → Infrastructure → Presentation |
| **Repository Pattern** | `IClickUpAccountRepository` abstracts data access |
| **Result Pattern** | Explicit success/failure without exceptions for business rules |
| **Options / DI** | Constructor injection throughout |
| **Typed HttpClient** | `ClickUpApiClient` with token-per-request for multi-account support |
| **Data Protection** | Encrypts ClickUp access tokens at rest |

## Phase 1 Features (Implemented)

- Register multiple ClickUp API tokens (one per workspace)
- Validate token against workspace on create/update
- Test connection per account
- List workspace members
- Query filtered tasks by month and assignees
- Blazor UI for account management

## Getting Started

### Prerequisites

- .NET 10 SDK
- ClickUp Personal Access Token(s)
- ClickUp Workspace ID(s)

### Run the application

```bash
cd e:\Clickup
dotnet run --project src/DevPulse.Server
```

Open the URL shown in the terminal (typically `https://localhost:7089`).

### Get your ClickUp Workspace ID

```bash
curl -H "Authorization: YOUR_TOKEN" https://api.clickup.com/api/v2/team
```

Use the `id` field from the response as the **Workspace ID** when adding an account.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/clickup/accounts` | List all configured accounts |
| POST | `/api/clickup/accounts` | Add and validate a new account |
| GET | `/api/clickup/accounts/{id}` | Get account by ID |
| PUT | `/api/clickup/accounts/{id}` | Update account |
| DELETE | `/api/clickup/accounts/{id}` | Delete account |
| GET | `/api/clickup/accounts/{id}/test` | Test token connection |
| GET | `/api/clickup/accounts/{id}/members` | List workspace members |
| GET | `/api/clickup/accounts/{id}/workspaces` | List authorized workspaces |
| POST | `/api/clickup/accounts/{id}/tasks/query` | Query filtered tasks |

### Example: Create account

```json
POST /api/clickup/accounts
{
  "name": "Client A Production",
  "workspaceId": "9012345678",
  "accessToken": "pk_your_clickup_token"
}
```

## Security Notes

- API tokens are **never** sent to the Blazor client after storage
- Tokens are encrypted using ASP.NET Core **Data Protection**
- For production, use Azure Key Vault and enable authentication (Azure AD / JWT)

## Next Phases

1. Developer registry and cross-workspace assignee mapping
2. Unified monthly task report by selected developers
3. Cursor Admin / Analytics API integration
4. KPI dashboard with filters and export (PDF/Excel)

## License

MIT
