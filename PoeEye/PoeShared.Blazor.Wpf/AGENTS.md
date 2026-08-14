# PoeShared.Blazor.Wpf Instructions

These instructions apply to the `PoeShared.Blazor.Wpf` project.

## NativeWindow tracked-property origins

`TrackedPropertyUpdateSource` describes command-routing and notification semantics. It does not
describe whether a human or the system conceptually caused a change.

- `External` is a scalar public-property request whose authoritative notification belongs to the
  Fody-woven setter. `PropertyValueHolder` does not raise `PropertyChanged` for this origin. For
  native-backed properties with an automatic command subscription, the holder stream also routes
  this origin to that command; not every `External` property has such a subscription.
- `Internal` must not enqueue the property's automatic scalar command. Use it for native readback,
  for a derived property owned by another external request, or inside a composite operation that
  explicitly enqueues exactly one native command. The holder raises `PropertyChanged` immediately.
- Origin is part of the holder state. Assigning the same value with a different origin intentionally
  emits a new state.
- Holder notifications and reactive subscriptions can run synchronously. Assignment order is part
  of the behavior, not a cosmetic detail.

For aspect-constrained scalar resize:

- Keep the setter-requested axis `External`.
- Set the derived axis `Internal` first, then set the requested axis `External`.
- Let the existing external subscription enqueue the one size command after both dimensions contain
  the final pair.
- Do not replace this with two `Internal` assignments plus a manual command. That shortcut changes
  notification timing and can feed a derived dimension back through TwoWay bindings as a new request.
- `TargetAspectRatio` itself is an `External` scalar property. A complete size derived after changing
  the ratio may be applied as an `Internal` composite operation which owns one explicit size command.

Composite APIs such as `SetWindowSize`, `SetWindowRect`, and `SetWindowPos` update their participating
holders as `Internal` and explicitly enqueue one matching command. Native geometry readback is also
`Internal`.

## Preserve operation intent

- A size-only operation must use the size command and preserve position.
- A position-only operation must use the position command and preserve size.
- Use a full-rectangle command only when the caller actually intends to change both.
- Do not merge size and position updates merely to obtain one convenient bounds value.

Do not change Win32 message selection, hook dispatch, or native readback mechanics to fix an
origin-routing bug without separate evidence that those mechanisms are at fault.
