# ADR 0005: Due dates are `DateOnly` and never converted

- Status: Accepted
- Date: 2026-11-05

## Context

A todo's due date could be modeled three ways: a UTC moment, a wall-clock date-time with a zone, or a bare date. The skill's time rules split these strictly, and a due date is "a date with no time": "tomorrow" means tomorrow, not tomorrow-midnight-in-some-zone — and shifting it across zones for storage is how due dates silently move a day.

## Decision

`DueDate` is `DateOnly?` → PostgreSQL `date` → JSON `"2026-11-06"`. No time, no offset, no zone, no conversion anywhere in the stack. Completion, by contrast, is a *moment* and stays `DateTime?` UTC in `timestamp with time zone`.

## Consequences

- Reminders/sorting by due date are trivially correct; a date survives any timezone change untouched.
- No `DateTime.Kind` traps (the classic Npgsql "Unspecified kind" write failure can't occur for this column).
- Cost: "due at 9am" precision is not representable; if ever needed, that is a new wall-clock-rule field with an IANA zone ID, per the same rules — not a widening of this one.
