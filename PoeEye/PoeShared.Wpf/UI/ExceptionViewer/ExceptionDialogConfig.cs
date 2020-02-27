using JetBrains.Annotations;

namespace PoeShared.Wpf.UI.ExceptionViewer
{
    public struct ExceptionDialogConfig
    {
        public string Title { [CanBeNull] get; [CanBeNull] set; }
        
        public string AppName { [CanBeNull] get; [CanBeNull] set; }
        
        public string[] FilesToAttach { get; set; }
    }
}