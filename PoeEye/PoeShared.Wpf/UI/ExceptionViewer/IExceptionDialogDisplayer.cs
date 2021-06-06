using System;
using JetBrains.Annotations;

namespace PoeShared.UI
{
    public interface IExceptionDialogDisplayer
    {
        void ShowDialog(ExceptionDialogConfig config, [NotNull] Exception exception);

        void ShowDialogAndTerminate([NotNull] Exception exception);
    }
}