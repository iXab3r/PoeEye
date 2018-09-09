using System;

namespace PoeShared.Exceptions
{
    public sealed class CaptchaException : ApplicationException
    {
        public CaptchaException(string message) : base(message)
        {
        }

        public CaptchaException(string message, string resolutionUri) : this(message)
        {
            ResolutionUri = resolutionUri;
        }

        public string ResolutionUri { get; }
    }
}