using System.IO;

namespace PoeShared.Audio.Services
{
    internal interface IFileSoundLibrarySource : ISoundLibrarySource
    {
        void AddFromFile(FileInfo soundFile);
    }
}