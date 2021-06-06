using JetBrains.Annotations;

namespace PoeShared.UI
{
    public struct ExceptionDialogConfig
    {
        public string Title { [CanBeNull] get; [CanBeNull] set; }
        
        public string AppName { [CanBeNull] get; [CanBeNull] set; }
        
        public string[] FilesToAttach { get; set; }
    }
}