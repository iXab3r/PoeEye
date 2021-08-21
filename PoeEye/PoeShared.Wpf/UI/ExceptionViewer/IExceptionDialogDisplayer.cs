using System;
using JetBrains.Annotations;

namespace PoeShared.UI
{
    public interface IExceptionDialogDisplayer
    {
        void ShowDialog(ExceptionDialogConfig config);
    }
}