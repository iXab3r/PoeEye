using System;

namespace PoeShared.Squirrel.Scaffolding
{
    public class ChecksumFailedException : Exception
    {
        public string Filename { get; set; }
    }
}