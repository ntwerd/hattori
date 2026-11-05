# ADR 0002: `OwnerId` is a plain column with no foreign key

- Status: Accepted
- Date: 2026-11-05

## Context

`TodoList.OwnerId` refers to a user. A foreign key to the identity table would be the textbook choice, but it drags identity into every repository test: fixtures would have to create real user rows just to satisfy referential integrity, and seeding/cleanup would cascade through identity tables.

## Decision

`OwnerId` is a `uuid` column with an index, not a foreign key. Ownership is enforced in every repository query (`Where(l => l.OwnerId == ownerId)` and, for items, `i.TodoList!.OwnerId == ownerId`), which is the check that actually matters for isolation.

## Consequences

- Repository tests create lists with arbitrary `Guid`s — no identity rows needed, simpler fixtures.
- The database would not reject an orphan owner ID written by a bug; nothing in the codebase writes `OwnerId` except `CreateAsync(ownerId, …)` with the ID from the authenticated principal.
- Deleting a user leaves their rows (the API has no user-deletion flow); noted if one is ever added.
