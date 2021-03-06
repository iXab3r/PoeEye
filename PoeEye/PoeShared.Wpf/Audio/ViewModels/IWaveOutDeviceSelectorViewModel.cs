using System.Collections.ObjectModel;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Audio.ViewModels
{
    public interface IWaveOutDeviceSelectorViewModel : IDisposableReactiveObject
    {
        WaveOutDevice SelectedItem { get; set; }
        
        ReadOnlyObservableCollection<WaveOutDevice> Devices { get; }

        void SelectById(string deviceId);
    }
}