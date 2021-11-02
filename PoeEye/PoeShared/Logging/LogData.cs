using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PoeShared.Logging
{
    /// <summary>
    ///     A class holding log data before being written.
    /// </summary>
    internal struct LogData
    {
        /// <summary>
        ///     Gets or sets the trace level.
        /// </summary>
        /// <value>
        ///     The trace level.
        /// </value>
        public FluentLogLevel LogLevel { get; set; }

        /// <summary>
        ///     Gets or sets the message.
        /// </summary>
        /// <value>
        ///     The message.
        /// </value>
        public string Message { get; set; }

        public Func<string> PrefixProvider { get; set; }
        
        public Func<string> SuffixProvider { get; set; }

        /// <summary>
        ///     Gets or sets the exception.
        /// </summary>
        /// <value>
        ///     The exception.
        /// </value>
        public Exception Exception { get; set; }

        /// <summary>
        ///     Gets or sets the name of the member.
        /// </summary>
        /// <value>
        ///     The name of the member.
        /// </value>
        public string MemberName { get; set; }

        /// <summary>
        ///     Gets or sets the file path.
        /// </summary>
        /// <value>
        ///     The file path.
        /// </value>
        public string FilePath { get; set; }

        /// <summary>
        ///     Gets or sets the line number.
        /// </summary>
        /// <value>
        ///     The line number.
        /// </value>
        public int LineNumber { get; set; }

        public readonly LogData WithSuffix(Func<string> provider)
        {
            var result = this;
            var initialSuffixProvider = this.SuffixProvider;
            result.SuffixProvider = () => $"{provider()}{initialSuffixProvider?.Invoke()}";
            return result;
        }
        
        public readonly LogData WithPrefix(Func<string> provider)
        {
            var result = this;
            var initial = this.PrefixProvider;
            result.PrefixProvider = () => $"{initial?.Invoke()}{provider()}";
            return result;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var message = new StringBuilder();
            if (!string.IsNullOrEmpty(FilePath) && !string.IsNullOrEmpty(MemberName))
            {
                message
                    .Append("[")
                    .Append(FilePath)
                    .Append(" ")
                    .Append(MemberName)
                    .Append("()")
                    .Append(" Ln: ")
                    .Append(LineNumber)
                    .Append("] ");
            }

            if (PrefixProvider != null)
            {
                message.Append(PrefixProvider());
            }
                
            message.Append(Message);

            if (SuffixProvider != null)
            {
                message.Append(SuffixProvider());
            }

            return message.ToString();
        }


        /// <summary>
        ///     Reset all properties back to default.
        /// </summary>
        internal void Reset()
        {
            LogLevel = FluentLogLevel.Trace;
            Message = null;
            PrefixProvider = null;
            Exception = null;
            MemberName = null;
            FilePath = null;
            LineNumber = 0;
        }
    }
}