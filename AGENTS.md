# Repository guidance

This checkout contains the PoeShared libraries under `PoeEye/` alongside historical PoeEye material. Start in the owning project and read its local instructions. In particular:

- [PoeShared.Blazor.Wpf/AGENTS.md](PoeEye/PoeShared.Blazor.Wpf/AGENTS.md) defines native window origin-routing, notification order, and operation intent.
- [PoeShared.Tests/AGENTS.md](PoeEye/PoeShared.Tests/AGENTS.md) defines test documentation and structure.

Preserve those contracts when changing shared behavior; do not treat old project names, target frameworks, or historical notes as authorization to delete runtime compatibility code.

## Validation

Use the affected project's tests and the actual consuming workspace's build configuration. Some project references expect sibling `PropertyBinder` and `maui` sources absent from this standalone checkout; identify the consumer layout before attempting a build or inventing replacement projects. Preserve configured SDK, framework, weaving, and package settings unless the task requires changing them. Report unavailable checks explicitly. Documentation-only changes need link, factual consistency, and diff checks; those checks do not establish runtime correctness.

## Independent architecture and review

Start every delegated architect, advisor, and reviewer with empty conversation context (`fork_turns="none"` with `spawn_agent`, or the host's equivalent). Provide only the concrete question, repository path and relevant revisions, scope, constraints, and evidence locations. Keep the assignment focused on its actual topic; do not carry the author's reasoning, preferred verdict, or prior agents' conclusions into it.

Each agent must inspect the relevant implementation and contracts independently and state its own evidence, conclusions, and uncertainty. The final reviewer must be separate from the implementer and design advisor, inspect the final diff at the named revision, and report actionable findings or a qualified pass. Disclose unavailable independent review instead of calling self-review independent.
