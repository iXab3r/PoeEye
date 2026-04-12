# PoeShared.Tests Instructions

These instructions apply to `Sources/PoeShared.Tests` unless a deeper `AGENTS.md` overrides them.

## C# Test Structure

For every compiled C# test method in this project:

- name the test method in `CamelCase`
- add an XML doc comment that includes `WHAT:` and `HOW:`
- use explicit `// Given`, `// When`, and `// Then` sections in the method body
- when a scenario intentionally contains multiple phases, add additional `// When` and `// Then` pairs for each phase instead of collapsing them into one block

Keep the structure consistent so test intent is readable in both source and test runner output.
