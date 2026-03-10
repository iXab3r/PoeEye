using PoeShared.Blazor.Prism;
using PoeShared.Blazor.WinForms.Prism;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.UI.WinForms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        using var container = CreateContainer();
        Application.Run(new Form1(container));
    }

    private static IUnityContainer CreateContainer()
    {
        var container = new UnityContainer();
        container.AddNewExtensionIfNotExists<CommonRegistrations>();
        container.AddNewExtensionIfNotExists<PoeSharedBlazorRegistrations>();
        container.AddNewExtensionIfNotExists<BlazorWinFormsRegistrations>();
        return container;
    }
}
