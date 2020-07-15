using System.IO;

namespace PoeShared.Audio.Services
{
    internal interface IFileSoundLibrarySource : ISoundLibrarySource
    {
        string AddFromFile(FileInfo soundFile);
    }
}