using log4net;
using Prism.Logging;
using Prism.Unity;

namespace PoeShared.Wpf.Services
{
    public sealed class PrismLogger : ILoggerFacade
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PrismApplication));

        public void Log(string message, Category category, Priority priority)
        {
            switch (category)
            {
                case Category.Debug when Logger.IsDebugEnabled:
                    Logger.Debug($"[{priority}] {message}");
                    break;
                case Category.Exception when Logger.IsErrorEnabled:
                    Logger.Error($"[{priority}] {message}");
                    break;
                case Category.Info when Logger.IsInfoEnabled:
                    Logger.Info($"[{priority}] {message}");
                    break;
                case Category.Warn when Logger.IsWarnEnabled:
                    Logger.Warn($"[{priority}] {message}");
                    break;
                default:
                    Logger.Warn($"[Unknown category! {category}] [{priority}] {message}");
                    break;
            }
        }
    }
}