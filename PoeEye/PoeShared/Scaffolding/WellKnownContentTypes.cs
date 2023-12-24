using System.Net.Mime;

namespace PoeShared.Scaffolding;

/// <summary>
/// Provides a collection of well-known ContentType instances.
/// </summary>
public static class WellKnownContentTypes
{
    /// <summary>
    /// Represents the content type for JPEG images.
    /// </summary>
    public static readonly ContentType Jpeg = new(MediaTypeNames.Image.Jpeg);
    
    public static readonly ContentType Png = new("image/png");
    public static readonly ContentType Gif = new("image/gif");
    public static readonly ContentType Bmp = new("image/bmp");
    public static readonly ContentType Svg = new("image/svg+xml");
    public static readonly ContentType Tiff = new("image/tiff");
    public static readonly ContentType Webp = new("image/webp");
    public static readonly ContentType BinaryBgra32 = new("image/bgra-32");
    public static readonly ContentType BinaryBgr24 = new("image/bgr-24");
    public static readonly ContentType BinaryArgb32 = new("image/argb-32");
    public static readonly ContentType BinaryRgba32 = new("image/rgba-32");
    public static readonly ContentType BinaryRgb24 = new("image/rgba-24");

    public static readonly ContentType Mp3 = new("audio/mpeg");
    public static readonly ContentType Wav = new("audio/wav");
    public static readonly ContentType OggAudio = new("audio/ogg");
    public static readonly ContentType Midi = new("audio/midi");
    public static readonly ContentType WebmAudio = new("audio/webm");

    public static readonly ContentType Mp4Video = new("video/mp4");
    public static readonly ContentType Avi = new("video/x-msvideo");
    public static readonly ContentType WebmVideo = new("video/webm");
    public static readonly ContentType OggVideo = new("video/ogg");
    public static readonly ContentType Mov = new("video/quicktime");
    public static readonly ContentType Flv = new("video/x-flv");

    public static readonly ContentType Pdf = new("application/pdf");
    public static readonly ContentType MsWord = new("application/msword");
    public static readonly ContentType MsExcel = new("application/vnd.ms-excel");
    public static readonly ContentType MsPowerpoint = new("application/vnd.ms-powerpoint");
    public static readonly ContentType OpenDocumentText = new("application/vnd.oasis.opendocument.text");
    public static readonly ContentType OpenDocumentSpreadsheet = new("application/vnd.oasis.opendocument.spreadsheet");

    public static readonly ContentType PlainText = new(MediaTypeNames.Text.Plain);
    public static readonly ContentType Html = new(MediaTypeNames.Text.Html);
    public static readonly ContentType Css = new("text/css");
    public static readonly ContentType Csv = new("text/csv");
    public static readonly ContentType Json = new("application/json");
    public static readonly ContentType Xml = new("application/xml");

    public static readonly ContentType Zip = new("application/zip");
    public static readonly ContentType Rar = new("application/x-rar-compressed");
    public static readonly ContentType SevenZip = new("application/x-7z-compressed");
    public static readonly ContentType Tar = new("application/x-tar");
    public static readonly ContentType Gzip = new("application/gzip");

    public static readonly ContentType Javascript = new("application/javascript");
    public static readonly ContentType Php = new("application/x-httpd-php");
    public static readonly ContentType Python = new("text/x-python");
    public static readonly ContentType Java = new("text/x-java-source");

    public static readonly ContentType Rtf = new("application/rtf");
    public static readonly ContentType MultipartFormData = new("multipart/form-data");
    public static readonly ContentType UrlEncoded = new("application/x-www-form-urlencoded");
    public static readonly ContentType AtomXml = new("application/atom+xml");
    public static readonly ContentType RssXml = new("application/rss+xml");
}