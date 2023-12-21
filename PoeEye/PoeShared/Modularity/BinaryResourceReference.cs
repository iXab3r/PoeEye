using System.Net.Mime;
using Newtonsoft.Json;

namespace PoeShared.Modularity;

public sealed record BinaryResourceReference 
{ 
    public string Uri { get; set; }
    
    public string SHA1 { get; set; } 
    
    public byte[] Data { get; set; }
    
    public DateTimeOffset? LastModified { get; set; }
    
    public ContentType ContentType { get; set; }
    
    public string FileName { get; set; }
    
    public int? ContentLength { get; set; }

    [JsonIgnore]
    public bool HasMetadata => !string.IsNullOrEmpty(Uri) || 
                               !string.IsNullOrEmpty(SHA1) || 
                               !string.IsNullOrEmpty(FileName) || 
                               ContentType != null || 
                               ContentLength != null || 
                               LastModified != null;
}