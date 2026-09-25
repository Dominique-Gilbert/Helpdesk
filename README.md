# IT Helpdesk & Assets

Training Project 5 of 9 - solo build. Local-only, no cloud.

Independently deployable
domains, gRPC between them, one REST gateway in front, EF Core per service, an async
event pipeline for the one thing that crosses a service boundary, Docker for the lot.

## Shape

```
Blazor Client --HTTPS--> REST Gateway --gRPC--> Ticket.API      --> Ticket DB
                                      --gRPC--> Asset.API       --> Asset DB
                                      --gRPC--> Assignment.API  --> Assignment DB
                                      --gRPC--> SLA.API         --> SLA DB
                                      --gRPC--> User.API        --> User DB
                                      --gRPC--> Client.API      --> Client DB

Ticket.API --publishes--> Ticket.Created --> Pulsar --> Assignment.API  (independent)
                                                     --> SLA.API         (independent)
SLA.API    --publishes--> SLA.Breached   --> Pulsar --> Ticket.API      (stretch, wired)
```

User.API and Client.API were added after the original four-service spec: User.API owns
login/accounts (the gateway's JWT issuer), Client.API owns the company/customer directory
that BaseRole manages and every other service's records are scoped to. Neither publishes
or consumes an event - no Pulsar wiring for either, unlike the original four.

`Ticket.Created` is a **fan-out, not a chain**. Assignment.API and SLA.API each bind their
own queue to the same exchange, process independently, and neither knows the other exists.
Stop one container and the other still works - that is the property under test.

| Folder | What lives there |
|---|---|
| `Backends/<Domain>.{Domain,Infrastructure,API}` | One triad per domain. Domain = POCOs, Infrastructure = DbContext + migrations, API = gRPC host. |
| `Gateway/Helpdesk.Gateway.{API,Interfacing,DTO}` | The only REST surface. Controllers call interfacing, interfacing calls gRPC and maps to hand-written DTOs. |
| `Frontends/Helpdesk.Blazor` | HTTP/JSON only. Never references a `.proto` or a `Grpc.*` package. |
| `Shared/Helpdesk.Contracts` | Event contracts (`TicketCreated`, `SlaBreached`) and `IAuditItem`. The only thing all services share. |
| `Shared/Helpdesk.Mapping` | Shared AutoMapper primitive converters (`DateTime <-> Timestamp`, `Guid <-> string`). |
| `Shared/Helpdesk.ServiceDefaults` | `AddServiceDefaults()` / `MapDefaultEndpoints()` - health checks, the Supercard `ServiceDefaults` equivalent. |
| `Testing/` | xUnit against the risky business logic (routing-rule matching, SLA clock maths) plus shared mock factories. |

## Run it

```bash
docker compose up --build
```

If this fails with `all predefined address pools have been fully subnetted`, Docker has run
out of local bridge subnets - usually from switching between this and `dotnet run` on the
AppHost a lot, which leaves an unused network behind on every restart. Fix:
`docker network prune -f`, then run `docker compose up --build` again.

Then:

| Surface | URL |
|---|---|
| Blazor client | http://localhost:5000 |
| REST gateway (Swagger) | http://localhost:8080/swagger |
| Pulsar admin API | http://localhost:8080/admin/v2/clusters |
| Ticket.API readiness | http://localhost:8101/health/readiness |
| Asset.API readiness | http://localhost:8102/health/readiness |
| Assignment.API readiness | http://localhost:8103/health/readiness |
| SLA.API readiness | http://localhost:8104/health/readiness |
| User.API readiness | http://localhost:8105/health/readiness |
| Client.API readiness | http://localhost:8106/health/readiness |

Everything behind the gateway except `POST /user/login` requires a Bearer token - log in
first (e.g. via Swagger's Authorize button, or `POST http://localhost:8080/user/login` with
`{"username":"admin","password":"Passw0rd!"}`) and pass the returned token as
`Authorization: Bearer <token>` on every other call. Trying an endpoint cold, without
logging in first, is a 401, not a bug.

Prove the fan-out in one command:

```powershell
pwsh ./scripts/verify-fanout.ps1
```

It creates a ticket through the gateway, then polls until both an `Assignment` row and an
`SLAClock` row exist for it - one event, two consumers, neither aware of the other.

## Two DTO layers, on purpose

```
EF entity -> AutoMapper (in <Domain>.API) -> proto message (gRPC wire)
          -> AutoMapper (in Gateway.Interfacing) -> DtoTicket -> REST/JSON -> Blazor
```

Proto messages are generated and never hand-written. `Helpdesk.Gateway.DTO` types are
hand-written, camelCase over the wire, and deliberately *not* 1:1 with the proto message -
they are shaped for the screen, not for the wire (`DtoTicketOverview` stitches together
three services in one object).

## Ports

Inside the compose network every service listens on `8080` (HTTP/1.1: health, REST) and
`8081` (HTTP/2 prior-knowledge: gRPC). Two endpoints, because plaintext h2c and HTTP/1.1
cannot share a port reliably. Host mappings are in `docker-compose.yml`; running outside
Docker uses the 51xx range from each `appsettings.json` so all six can run at once.

## Known gaps (be honest about these in the PR)

- `Ticket.API` publishes after `SaveChangesAsync`, with no transactional outbox. A crash
  between the two loses the event. MassTransit's EF Core outbox is the fix; out of scope
  for the deadline, listed here so it is a known risk rather than an unknown one.
- The SLA breach monitor is a polling `BackgroundService` on a 15s tick, not a scheduler.
  Fine locally, not a production design.
- Seed data for technicians and routing rules is inserted at startup, guarded by an
  existence check. Real systems get this from an admin surface.
