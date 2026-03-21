# PoeShared.Blazor.UnscopedCss

Selective build-time post-processing for Blazor scoped CSS.

## What it does

Blazor rewrites scoped component CSS into generated outputs and then bundles them into
`{PackageId}.styles.css`. This package adds build steps that can rewrite either the generated
component CSS or the final bundle outputs.

If a CSS file is marked with:

```xml
<None Update="EventViewer/AuraEventList.razor.css"
      UnscopeCss="true" />
```

the package rewrites the matching generated CSS output and removes the Blazor scope attribute
selectors.

This works for scoped source files like `*.razor.css` and generated bundle files like
`{PackageId}.styles.css` or `{PackageId}.bundle.scp.css`.

## Consumer usage

1. Add the package:

```xml
<PackageReference Include="PoeShared.Blazor.UnscopedCss" Version="1.0.2" PrivateAssets="all" />
```

2. Opt in file-by-file:

```xml
<ItemGroup>
  <None Update="EventViewer/AuraEventList.razor.css"
        UnscopeCss="true" />
</ItemGroup>
```

Optional:

```xml
<None Update="EventViewer/AuraEventList.razor.css"
      CssScope="ea-event-log"
      UnscopeCss="true" />
```

Bundle-level usage:

```xml
<None Update="MyApp.styles.css"
      CssScope="ea-event-log"
      UnscopeCss="true" />
```

## Notes

- The package rewrites generated `obj/.../scopedcss/.../*.rz.scp.css` files before bundling and can
  also rewrite bundled outputs like `*.styles.css` after bundling.
- The unscoping pass is intentionally selective: only files marked with `UnscopeCss` are touched.
- The package is delivered through `buildTransitive`, so adding the package reference is enough for
  consuming projects.
- The task assembly is multi-targeted for both desktop MSBuild (`net472`) and `dotnet build`
  (`net8.0`).
- Logging is enabled by default and includes task duration in milliseconds.
- `PoeUnscopeAfterRewrite` is still accepted as a compatibility alias, but `UnscopeCss` is the
  preferred metadata name.
- If you toggle `UnscopeCss` on an existing file, do a clean rebuild once so the
  generated scoped CSS is regenerated from source before the post-processing step runs again.
