using System.Threading.Tasks;

namespace PoeShared.UI;

public interface IExceptionInterceptor
{
    void Handle(UnobservedTaskExceptionEventArgs eventArgs);
}