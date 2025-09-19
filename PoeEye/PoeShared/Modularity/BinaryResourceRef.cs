using System.Net.Mime;
using Newtonsoft.Json;
// ReSharper disable SealedMemberInSealedClass

namespace PoeShared.Modularity;

/// <summary>
/// Represents a reference to a binary resource, which can be either stored as binary data or referenced via a URI.
/// </summary>
[Newtonsoft.Json.JsonConverter(typeof(BinaryResourceRefConverter))]
public sealed record BinaryResourceRef : IHasValidation
{
    public static readonly BinaryResourceRef Empty = new();

    public BinaryResourceRef()
    {
    }

    public BinaryResourceRef(byte[] data)
    {
        Data = data;
    }

    public BinaryResourceRef(string uri)
    {
        Uri = uri;
    }

    /// <summary>
    /// Gets the URI of the binary resource if it is stored externally.
    /// </summary>
    public string Uri { get; init; }
    
    /// <summary>
    /// Gets the cipher suite - a combination of encryption, key exchange, and HMAC algorithms, null if not applicable (e.g. data is not encrypted)
    /// </summary>
    public string CipherSuite { get; init; } 
    
    /// <summary>
    /// Gets the salt(optional), used in combination with password for decryption purposes
    /// </summary>
    public string CipherKeySalt { get; init; } 
    
    /// <summary>
    /// Gets the hash of the binary data for integrity verification. If CipherSuite is not set, you can assume it is xxHash.
    /// Hash is calculated on raw(decrypted) data and can be used for validation purposes.
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
    public MimeContentType? ContentType { get; init; }
    
    /// <summary>
    /// Gets the file name of the binary resource, if applicable.
    /// </summary>
    public string FileName { get; init; }
    
    /// <summary>
    /// Gets the length of the content in bytes, if known.
    /// </summary>
    public int? ContentLength { get; init; }

    /// <summary>
    /// Gets a value indicating whether the binary data is stored directly in this object.
    /// </summary>
    [JsonIgnore]
    public bool IsMaterialized => Data != null;

    /// <summary>
    /// Gets a value indicating whether any metadata (like Uri, Hash, FileName, etc.) is associated with the binary resource.
    /// </summary>
    [JsonIgnore]
    public bool HasMetadata => !string.IsNullOrEmpty(Uri) || 
                               !string.IsNullOrEmpty(CipherSuite) || 
                               !string.IsNullOrEmpty(CipherKeySalt) || 
                               !string.IsNullOrEmpty(Hash) || 
                               !string.IsNullOrEmpty(FileName) || 
                               ContentType != null || 
                               LastModified != null;

    /// <summary>
    /// Gets a value indicating whether the resource reference is valid (i.e., it either has a URI or binary data).
    /// </summary>
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrEmpty(Uri) || Data != null;
    
    /// <summary>
    /// Gets a value indicating whether the resource is encrypted
    /// </summary>
    [JsonIgnore]
    public bool IsEncrypted => !string.IsNullOrEmpty(CipherKeySalt) || !string.IsNullOrEmpty(CipherSuite);

    public sealed override string ToString()
    {
        var builder = new ToStringBuilder(this);
        builder.AppendParameterIfNotDefault(nameof(Uri), Uri);
        if (Data != null)
        {
            builder.AppendParameter(nameof(Data), ByteSizeLib.ByteSize.FromBytes(Data.LongLength));
        }
        builder.AppendParameterIfNotDefault(nameof(CipherSuite), CipherSuite);
        builder.AppendParameterIfNotDefault(nameof(CipherKeySalt), CipherKeySalt);
        builder.AppendParameterIfNotDefault(nameof(Hash), Hash);
        builder.AppendParameterIfNotDefault(nameof(FileName), FileName);
        builder.AppendParameterIfNotDefault(nameof(ContentType), ContentType);
        builder.AppendParameterIfNotDefault(nameof(ContentLength), ContentLength);
        return builder.ToString();
    }
}