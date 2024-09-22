namespace PoeShared.Reporting;

public interface IExceptionInterceptor
{
    void Handle(UnobservedTaskExceptionEventArgs eventArgs);
}