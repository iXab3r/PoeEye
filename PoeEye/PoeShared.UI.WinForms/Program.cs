using DynamicData;
using PoeShared.Blazor;
using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.WinForms.Prism;
using PoeShared.Prism;
using PoeShared.UI.WinForms;
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
        Application.Run(container.Resolve<Form1>());
    }

    private static IUnityContainer CreateContainer()
    {
        var container = new UnityContainer();
        container.AddNewExtensionIfNotExists<CommonRegistrations>();
        container.AddNewExtensionIfNotExists<PoeSharedBlazorRegistrations>();
        container.AddNewExtensionIfNotExists<BlazorWinFormsRegistrations>();
        
        var blazorContentRepository = container.Resolve<IBlazorContentRepository>();
        blazorContentRepository.AdditionalFiles.Add(new RefFileInfo(@"_content/PoeShared.Blazor.WinForms/css/bootstrap.css"));
        blazorContentRepository.AdditionalFiles.Add(new RefFileInfo(@"_content/PoeShared.Blazor.WinForms/css/bootstrap-extra.css"));
        blazorContentRepository.AdditionalFiles.Add(new RefFileInfo(@"_content/PoeShared.Blazor.WinForms/css/app.css"));
        blazorContentRepository.AdditionalFiles.Add(new RefFileInfo(@"_content/PoeShared.Blazor.WinForms/css/font-awesome6.min.css"));
        blazorContentRepository.AdditionalFiles.Add(new RefFileInfo(@"_content/PoeShared.Blazor.WinForms/css/blazor-window.css"));
        blazorContentRepository.AdditionalFiles.Add(new RefFileInfo(@"_content/PoeShared.Blazor.Controls/assets/css/main-colors.css"));
        blazorContentRepository.AdditionalFiles.Add(new RefFileInfo(@"_content/PoeShared.Blazor.Controls/assets/css/main-style.css"));
        blazorContentRepository.AdditionalFiles.Add(new RefFileInfo(@"_content/PoeShared.Blazor.Controls/assets/css/main-ant-blazor.css"));
        
        return container;
    }
}
