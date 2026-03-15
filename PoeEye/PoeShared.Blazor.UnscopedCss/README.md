# PoeShared.Blazor.UnscopedCss

Selective build-time post-processing for Blazor scoped CSS.

## What it does

Blazor rewrites `.razor.css` files into generated scoped CSS and then bundles them into
`{PackageId}.styles.css`. This package adds a build step between those two phases.

If a `.razor.css` file is marked with:

```xml
<None Update="EventViewer/AuraEventList.razor.css"
      PoeUnscopeAfterRewrite="true" />
```

the package rewrites the generated `*.rz.scp.css` file and removes the Blazor scope attribute
selectors before the final bundle is produced.

Everything else remains normal Blazor scoped CSS.

## Consumer usage

1. Add the package:

```xml
<PackageReference Include="PoeShared.Blazor.UnscopedCss" Version="1.0.2" PrivateAssets="all" />
```

2. Opt in file-by-file:

```xml
<ItemGroup>
  <None Update="EventViewer/AuraEventList.razor.css"
        PoeUnscopeAfterRewrite="true" />
</ItemGroup>
```

Optional:

```xml
<None Update="EventViewer/AuraEventList.razor.css"
      CssScope="ea-event-log"
      PoeUnscopeAfterRewrite="true" />
```

## Notes

- The package runs after Blazor generates `obj/.../scopedcss/.../*.rz.scp.css`.
- The final `*.styles.css` bundle is still produced and linked by the normal Blazor pipeline.
- The unscoping pass is intentionally selective: only files marked with `PoeUnscopeAfterRewrite`
  are touched.
- The package is delivered through `buildTransitive`, so adding the package reference is enough for
  consuming projects.
- The task assembly is multi-targeted for both desktop MSBuild (`net472`) and `dotnet build`
  (`net8.0`).
- If you toggle `PoeUnscopeAfterRewrite` on an existing file, do a clean rebuild once so the
  generated scoped CSS is regenerated from source before the post-processing step runs again.
