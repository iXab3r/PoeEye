using System;
using JetBrains.Annotations;

namespace PoeShared.Wpf.UI.ExceptionViewer
{
    public interface IExceptionDialogDisplayer
    {
        void ShowDialog(ExceptionDialogConfig config, [NotNull] Exception exception);

        void ShowDialogAndTerminate([NotNull] Exception exception);
    }
}