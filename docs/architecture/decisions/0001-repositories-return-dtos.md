# ADR 0001: Repositories return DTOs, never entities

- Status: Accepted
- Date: 2026-11-05

## Context

Controllers need data to answer requests, and the obvious shortcut is returning entities. That couples the API contract to the database schema (every column rename becomes a breaking change), leaks fields the client must not see, and drags EF types into the web layer.

## Decision

Repository interfaces live in `Hattori.Core` and speak only DTOs: reads project LINQ straight into DTO records (`Select(i => new TodoItemDto(…))`, `AsNoTracking()`), writes accept primitives/DTOs and return DTOs. Entities never leave `Hattori.Persistence` (the tracked-load-then-mutate pattern stays inside write methods). Because DTOs live in `.Core`, the interfaces — and the tests — need no EF reference.

## Consequences

- API contract and schema evolve independently; response shapes are explicit.
- Only queried columns are read for list endpoints.
- Cost: a projection per read and a DTO per shape — the boilerplate this buys off in a two-table system already.
