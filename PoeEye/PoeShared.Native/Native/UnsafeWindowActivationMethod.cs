namespace PoeShared.Native;

/// <summary>
/// Describes window activation technique.
/// There are a lot of nuances in window activation and there are multiple methods, none of which are ideal.
/// https://devblogs.microsoft.com/oldnewthing/?p=94745
/// https://github.com/microsoft/PowerToys/pull/1282
/// </summary>
public enum UnsafeWindowActivationMethod
{
    /// <summary>
    /// Select best activation method automatically
    /// </summary>
    Auto,
    /// <summary>
    /// Uses AttachThreadInput to attach to input queue of target window
    /// (by Redmond Chen) A bad solution would be to use the AttachThreadInput function to connect the test automation tool’s input queue to the input queue of the target window.
    /// This is a bad solution because it means that if the target window has stopped responding, then the test automation will also stop responding.
    /// And it’s bad for a test to stop responding.
    /// The purpose of the test is to monitor the main application reliably, not to get into the same jail.  
    /// </summary>
    AttachThreadInput,
    /// <summary>
    /// Sends empty input message to target window to make sure that it is fully ready for activation
    /// </summary>
    SendInput
}