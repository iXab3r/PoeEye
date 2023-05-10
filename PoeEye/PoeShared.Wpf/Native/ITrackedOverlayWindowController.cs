namespace PoeShared.Native;

public interface ITrackedOverlayWindowController : IOverlayWindowController
{
    IWindowTracker WindowTracker { get; }
    
    void ActivateLastActiveWindow();
}