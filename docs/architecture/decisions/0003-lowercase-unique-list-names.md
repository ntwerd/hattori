# ADR 0003: List names stored lowercase, unique among live lists

- Status: Accepted
- Date: 2026-11-05

## Context

List names must be unique per user, case-insensitively. Enforcing that in PostgreSQL needs either a functional index (`LOWER(Name)`) or a normalized column, and the check in code must agree with the index or a rename race slips duplicates past one of them.

## Decision

Follow the skill's tag pattern: the controller normalizes names with `Trim().ToLowerInvariant()` on create and update; the database enforces `UNIQUE (OwnerId, Name) WHERE "IsDeleted" = false`. Both layers therefore compare identical strings.

## Consequences

- Code check (`ExistsAsync`) and database constraint can never disagree about case.
- The partial index means a soft-deleted list frees its name for reuse.
- Cost: the stored/displayed name is lowercase — "Chores" comes back as "chores". A display-casing layer, if ever wanted, belongs in the frontend, not here.
