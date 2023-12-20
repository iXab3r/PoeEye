namespace PoeShared.Modularity;

public sealed record BinaryResourceReference 
{ 
    public string Uri { get; set; }
    
    public string SHA1 { get; set; } 
    
    public byte[] Data { get; set; }
}