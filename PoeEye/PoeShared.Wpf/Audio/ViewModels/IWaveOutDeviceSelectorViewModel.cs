using System.Collections.ObjectModel;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.ViewModels;

public interface IWaveOutDeviceSelectorViewModel : IDisposableReactiveObject
{
    WaveOutDevice SelectedItem { get; set; }
        
    IReadOnlyObservableCollection<WaveOutDevice> Devices { get; }

    void SelectById(string deviceId);
}