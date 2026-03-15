# Reactive Collection Presenters

This folder contains the incremental collection rendering pipeline used by `ReactiveListPresenter`,
`ReactiveCachePresenter`, and the advanced `ReactiveCollectionPresenter`.

## Why this exists

Normal Razor `foreach` rendering makes the parent component own the diff for every visible item.
That becomes a bottleneck for hot or large keyed collections where a single add, remove, or move
should not force Blazor to revisit hundreds of unrelated siblings.

These presenters keep the parent render tree small and push collection topology changes through a
dedicated JS session:

- .NET stays authoritative for the collection state.
- JS owns the container DOM and host elements.
- Each visible item is an independent Blazor island mounted through `Blazor.rootComponents.add`.

## Presenter layers

- `ReactiveListPresenter<TItem, TKey>` is the ordered entry point and preserves `Move` without recreating item roots.
- `ReactiveCachePresenter<TItem, TKey>` is the unordered keyed entry point and intentionally ignores ordering semantics.
- `ReactiveCollectionPresenter<TItem, TKey>` is the advanced entry point that accepts already-normalized frames.

## Important invariants

- No compaction in v1. Operations are preserved in the order they were received.
- Batching is optional. A non-zero `BatchDelay` only groups operations into fewer JS interop calls.
- `Move` must only reorder the existing host element. It must not recreate the mounted Blazor island.
- `Update` reuses the existing root component and only swaps the stored render payload.
- Parent rerenders must not rebuild the logical collection binding. The normalized change stream is latched
  to the current source identity so incidental Razor rerenders do not replay the initial `Reset`.
- `EmptyTemplate` belongs inside the presenter because "collection is empty" is presenter-owned state.
  Toggling that shell content externally would require an extra reactive wrapper that defeats the purpose.

## Why there is a registry

`RenderFragment` and arbitrary template captures cannot cross JS interop. The presenter therefore
stores per-item render payloads in `IReactiveCollectionItemRegistry` and sends only an opaque
`registrationId` plus `version` through JS.

The mounted `ReactiveCollectionItemHost` resolves the payload locally on the .NET side:

- `Add` => create registration, mount host with `registrationId`
- `Update` => update existing registration, call `setParameters({ registrationId, version })`
- `Remove` => dispose host, unregister registration

## CSS isolation nuance

The item host elements are created from JS, so they do not receive the parent's CSS isolation scope
attribute automatically. When a consumer wants to style those hosts from isolated CSS, use
`::deep` selectors or global styles.

This is especially relevant for:

- row striping based on `nth-child(...)`
- host-level layout like grid or absolute positioning
- styling the JS-created outer shell instead of the inner item fragment

## Template guidance

`ItemTemplate` should primarily depend on the item itself. It can technically capture parent state
or delegates because the render fragment remains on the .NET side, but doing so heavily couples
independent item islands back to the parent component and should be used sparingly.

For the same reason, `KeySelector` and `Changes` should be treated as stable bindings. If a caller
truly wants to replace the logical source or key semantics at runtime, recreate the presenter
instead of relying on incidental parent rerenders.

`EmptyTemplate` can contain dynamic content, but because the presenter shell itself is render-once
after initialization, dynamic empty-state markup should usually be wrapped in a reactive child
component such as `ReactiveSection`.
