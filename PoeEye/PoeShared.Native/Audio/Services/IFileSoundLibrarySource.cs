using System.IO;

namespace PoeShared.Audio.Services
{
    public interface IFileSoundLibrarySource : ISoundLibrarySource
    {
        string AddFromFile(FileInfo soundFile);

        string AddFromWaveData(string notification, byte[] waveData);
    }
}