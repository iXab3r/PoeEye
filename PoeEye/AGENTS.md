# PoeEye Instructions

These instructions apply to the whole `Submodules/PoeEye/PoeEye` tree unless a deeper `AGENTS.md` overrides them.

## PropertyChanged.Fody

All C# projects in this solution are expected to include `PropertyChanged.Fody` directly.

- Add `<PackageReference Include="PropertyChanged.Fody" Version="3.4.0" PrivateAssets="All"><ExcludeAssets>runtime</ExcludeAssets><IncludeAssets>All</IncludeAssets></PackageReference>` to every `.csproj`.
- Add a matching `FodyWeavers.xml` with `<PropertyChanged />` in every project directory.
- Prefer normal field assignment or auto-properties and let Fody raise property changed notifications.
- Do not introduce new `RaiseAndSetIfChanged` usage just to implement routine property notification.
- Keep manual setters only when they are needed for side effects, validation, equality guards, or extra control flow beyond notification itself.
- This rule is enforced by the `PoeShared.Tests` meta tests, so drift should be treated as a build/test failure.
