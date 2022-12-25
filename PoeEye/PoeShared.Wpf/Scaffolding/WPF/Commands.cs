using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PoeShared.Scaffolding.WPF;

public static class Commands
{
    public static ICommand OpenUri { get; } = CommandWrapper.Create<object>(OpenUriExecuted);

    private static async Task OpenUriExecuted(object arg)
    {
        var uri = arg switch
        {
            string stringArg => stringArg,
            Uri uriArg => uriArg.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(arg), arg, $"Unsupported argument type: {arg}")
        };
        await ProcessUtils.OpenUri(uri);
    }
}