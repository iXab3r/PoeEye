# Basis - WPF window
- has its own dispatcher
- thread-safe properties
- properties update must be instantly visible to getter
- do not use locking!
- separate properties for Left/Top/Width/Height
- anything (own Window events / user driver events) come through the same Channel

# Class structure
- `NativeWindow` (`INativeWindow`) - Blazor-agnostic core: dispatcher, command channel (Queue), timestamped
  property mirror (PropertyHolder), lifecycle, title bar chrome/transparency logic (WindowView).
  Hosts arbitrary WPF content supplied via `ContentFactory` (invoked on the window's own UI thread).
- `BlazorWindow` (`IBlazorWindow : INativeWindow`) - specialization which hosts Blazor/WebView2 content
  (body + title bar `BlazorContentControl`s), child Unity container, file providers, dev tools and
  automation registrar wiring. `ContentFactory` is not supported there - it throws, use `ViewType`.

# Problems
- race condition when window notifies about its own properties update (e.g. dragging/resizing) + user sets value.
Solved by timestamping and IDing all events. Last always wins. 

# How it works
- inner WPF window is not even created initially
- all properties which it has could be changed before window is shown
- window could be shown/hidden at any moment and how many times needed
- window could be closed only once
- we have a single Channel through which all changes are happening - Show/Hide requests, property updates by user,
event updates by window itself
- each property value is stored inside its own reactive container
tracks its last update timestamp, revision and update source (system or user)
- each property is being subscribed to after window is first shown. If property changes
then a special command is put into Channel
- Channel analyses update/command notifications on a separate dispatcher
- each "tick" all changes are processed and either propagated TO window, or FROM window to properties holder
- if there is some command, like Show/Hide command - those are parsed as well
