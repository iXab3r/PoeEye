using System.Net.Mime;
using Newtonsoft.Json;

namespace PoeShared.Modularity;

/// <summary>
/// Represents a reference to a binary resource, which can be either stored as binary data or referenced via a URI.
/// <remarks>
/// Important! Do not change to "record" as it will change comparison semantics(e.g. Data will be compared by value) 
/// </remarks>
/// </summary>
public sealed record BinaryResourceRef : IHasValidation
{
    public static readonly BinaryResourceRef Empty = new(); 
    
    /// <summary>
    /// Gets the URI of the binary resource if it is stored externally.
    /// </summary>
    public string Uri { get; init; }
    
    /// <summary>
    /// Gets the xxHash hash of the binary data for integrity verification.
    /// </summary>
    public string Hash { get; init; } 
    
    /// <summary>
    /// Gets the binary data directly, may be null if data is stored externally and is not resolved yet
    /// </summary>
    public byte[] Data { get; init; }
    
    /// <summary>
    /// Gets the last modification timestamp of the resource, if available.
    /// </summary>
    public DateTimeOffset? LastModified { get; init; }
    
    /// <summary>
    /// Gets the MIME type of the binary resource.
    /// </summary>
    public ContentType ContentType { get; init; }
    
    /// <summary>
    /// Gets the file name of the binary resource, if applicable.
    /// </summary>
    public string FileName { get; init; }
    
    /// <summary>
    /// Gets the length of the content in bytes, if known.
    /// </summary>
    public int? ContentLength { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the binary data stored in this blob could be materialized(downloaded)/de-materialized(converted to URI).
    /// </summary>
    public bool SupportsMaterialization { get; init; }

    /// <summary>
    /// Gets a value indicating whether the binary data is stored directly in this object.
    /// </summary>
    [JsonIgnore]
    public bool IsMaterialized => Data != null;

    /// <summary>
    /// Gets a value indicating whether any metadata (like Uri, SHA1, FileName, etc.) is associated with the binary resource.
    /// </summary>
    [JsonIgnore]
    public bool HasMetadata => !string.IsNullOrEmpty(Uri) || 
                               !string.IsNullOrEmpty(Hash) || 
                               !string.IsNullOrEmpty(FileName) || 
                               ContentType != null || 
                               ContentLength != null || 
                               LastModified != null;

    /// <summary>
    /// Gets a value indicating whether the resource reference is valid (i.e., it either has a URI or binary data).
    /// </summary>
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrEmpty(Uri) || Data != null;

    public bool Equals(BinaryResourceRef other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Uri == other.Uri && Hash == other.Hash && 
               ReferenceEquals(Data, other.Data) && 
               Nullable.Equals(LastModified, other.LastModified) && 
               Equals(ContentType, other.ContentType) && 
               FileName == other.FileName && 
               ContentLength == other.ContentLength;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Uri, Hash, Data, LastModified, ContentType, FileName, ContentLength);
    }

    public override string ToString()
    {
        var builder = new ToStringBuilder(this);
        builder.AppendParameterIfNotDefault(nameof(Uri), Uri);
        if (Data != null)
        {
            builder.AppendParameter(nameof(Data), ByteSizeLib.ByteSize.FromBytes(Data.LongLength));
        }
        builder.AppendParameterIfNotDefault(nameof(Hash), Hash);
        builder.AppendParameterIfNotDefault(nameof(FileName), FileName);
        builder.AppendParameterIfNotDefault(nameof(ContentType), ContentType);
        builder.AppendParameterIfNotDefault(nameof(ContentLength), ContentLength);
        return base.ToString();
    }
}