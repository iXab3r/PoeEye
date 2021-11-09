using PoeShared.Logging;

namespace PoeShared.Squirrel.Updater
{
    public sealed record UpdaterWindowArgs
    {
        public string Message { get; set; }
        
        public FluentLogLevel MessageLevel { get; set; }
        
        public bool AllowTermination { get; set; }
    }
}