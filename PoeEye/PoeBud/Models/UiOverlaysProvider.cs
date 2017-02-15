using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeBud.Models
{
    public class UiOverlaysProvider : IUiOverlaysProvider
    {
        public UiOverlaysProvider()
        {
            var assemblyLocation = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            var overlaysDirectory = Path.Combine(assemblyDirectory, "Resources", "Overlays");

            var overlaysList = Directory.Exists(overlaysDirectory) 
                ? Directory.GetFiles(overlaysDirectory, "*.png")
                : new string[0];

            OverlaysList.Add(UiOverlayInfo.Empty);

            overlaysList
                .Select(
                x => new UiOverlayInfo
                {
                    Name = Path.GetFileNameWithoutExtension(x),
                    AbsolutePath = x,
                }).ForEach(OverlaysList.Add);
        }

        public IReactiveList<UiOverlayInfo> OverlaysList { get; } = new ReactiveList<UiOverlayInfo>();
    }

    public struct UiOverlayInfo
    {
        public static readonly UiOverlayInfo Empty = new UiOverlayInfo() { Name = "None"};

        public string Name { get; set; } 

        public string AbsolutePath { get; set; }
    }
}