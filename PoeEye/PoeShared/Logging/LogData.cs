using System.Text;

namespace PoeShared.Logging;

/// <summary>
///     A class holding log data before being written.
/// </summary>
internal record struct LogData
{
    private const int DefaultMaxLineLength = 4096;
    
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

    public int? MaxLineLength { get; set; }

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

        var messageContent = Message.TakeMidChars(MaxLineLength ?? DefaultMaxLineLength);
        message.Append(messageContent);

        if (SuffixProvider != null)
        {
            message.Append(SuffixProvider());
        }

        return message.ToString();
    }
}