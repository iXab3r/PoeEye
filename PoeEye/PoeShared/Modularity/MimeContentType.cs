using System.Net.Mime;

namespace PoeShared.Modularity;

/// <summary>
/// Readonly MIME content-type. The main difference between this and ContentType is immutability and better serialization support
/// Also, ContentType uses few weird constructs like TrackingStringDictionary which is not supported by CompareService 
/// </summary>
public readonly struct MimeContentType
{
    public MimeContentType(string value)
    {
        MediaType = value;
    }

    public MimeContentType(ContentType contentType)
    {
        MediaType = contentType.MediaType;
    }

    /// <summary>Gets or sets the media type value included in the Content-Type header represented by this instance.</summary>
    /// <exception cref="T:System.ArgumentNullException">The value specified for a set operation is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException">The value specified for a set operation is <see cref="F:System.String.Empty" /> ("").</exception>
    /// <exception cref="T:System.FormatException">The value specified for a set operation is in a form that cannot be parsed.</exception>
    /// <returns>A <see cref="T:System.String" /> that contains the media type and subtype value. This value does not include the semicolon (;) separator that follows the subtype.</returns>
    public string MediaType { get; }

    public ContentType ToContentType()
    {
        return new ContentType(MediaType);
    }

    public override string ToString()
    {
        return MediaType;
    }
}