# Architecture decision records

Short, dated records of *why* hattori is shaped the way it is. Each one: the context that forced a choice, the decision, and what it costs. Change a decision by writing a new ADR that supersedes the old one — never by editing history.

| ADR | Decision | Status |
| --- | --- | --- |
| [0001](0001-repositories-return-dtos.md) | Repositories return DTOs, never entities | Accepted |
| [0002](0002-owner-id-without-foreign-key.md) | `OwnerId` is a plain column with no foreign key | Accepted |
| [0003](0003-lowercase-unique-list-names.md) | List names stored lowercase, unique among live lists | Accepted |
| [0004](0004-own-register-endpoint.md) | Own `api/account/register`; Identity's is answered 404 | Accepted |
| [0005](0005-dateonly-due-dates.md) | Due dates are `DateOnly` and never converted | Accepted |
| [0006](0006-permissions-in-cookie.md) | Permissions baked into the cookie as role claims | Accepted |
| [0007](0007-env-var-configuration.md) | Settings are environment variables, not `appsettings.json` | Accepted |
