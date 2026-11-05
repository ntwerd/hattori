# hattori architecture

Layered documentation for the hattori todo API: start high, drill down. Each file answers one reader's question; diagrams are Mermaid and render on GitHub.

## Contents

| File | Level | Answers |
| --- | --- | --- |
| [01-system-overview.md](01-system-overview.md) | High | What is this system, who talks to it, what is it built of? |
| [02-containers.md](02-containers.md) | Mid | What are the projects and databases, and which way do dependencies point? |
| [03-components.md](03-components.md) | Mid | What lives inside each project, type by type? |
| [04-data-model.md](04-data-model.md) | Low | How is the data shaped, and how do soft delete and audit work? |
| [05-security.md](05-security.md) | Low | How do sign-in, permissions and cookies work? |
| [06-request-lifecycle.md](06-request-lifecycle.md) | Low | What exactly happens to one request? |
| [07-cross-cutting.md](07-cross-cutting.md) | Low | Configuration, logging, errors, health, time, API docs |
| [08-maintainers-guide.md](08-maintainers-guide.md) | Recipes | How do I add an endpoint, migration, setting, permission, test? |
| [decisions/](decisions/README.md) | Why | The decision records behind the design |

## Reading paths

- **Five minutes:** 01 → 02.
- **New developer:** 01 → 02 → 04 → 08, then the [README](../../README.md) to run it.
- **Adding a feature:** 08 first, then the deep dive it points at.
- **Debugging auth or a 401/403:** 05 → 06.
- **"Why is it like this?":** [decisions/](decisions/README.md).

House rules that keep this set maintainable:

- Code is the source of truth; when code and docs disagree, fix the doc in the same change.
- No aspirational content — everything here describes what exists.
- Detail lives here; [README](../../README.md) stays "how to run", [AGENTS.md](../../AGENTS.md) stays the short agent rulebook, and neither duplicates these pages.
