using System.Net.Mime;
using PoeShared.Modularity;

namespace PoeShared.Scaffolding;

/// <summary>
/// Provides a collection of well-known ContentType instances.
/// </summary>
public static class WellKnownContentTypes
{
    /// <summary>
    /// Represents the content type for JPEG images.
    /// </summary>
    public static readonly MimeContentType Jpeg = new(MediaTypeNames.Image.Jpeg);
    
    public static readonly MimeContentType Png = new("image/png");
    public static readonly MimeContentType Gif = new("image/gif");
    public static readonly MimeContentType Bmp = new("image/bmp");
    public static readonly MimeContentType Svg = new("image/svg+xml");
    public static readonly MimeContentType Tiff = new("image/tiff");
    public static readonly MimeContentType Webp = new("image/webp");
    public static readonly MimeContentType BinaryBgra32 = new("image/bgra-32");
    public static readonly MimeContentType BinaryBgr24 = new("image/bgr-24");
    public static readonly MimeContentType BinaryArgb32 = new("image/argb-32");
    public static readonly MimeContentType BinaryRgba32 = new("image/rgba-32");
    public static readonly MimeContentType BinaryRgb24 = new("image/rgba-24");

    public static readonly MimeContentType Mp3 = new("audio/mpeg");
    public static readonly MimeContentType Wav = new("audio/wav");
    public static readonly MimeContentType OggAudio = new("audio/ogg");
    public static readonly MimeContentType Midi = new("audio/midi");
    public static readonly MimeContentType WebmAudio = new("audio/webm");

    public static readonly MimeContentType Mp4Video = new("video/mp4");
    public static readonly MimeContentType Avi = new("video/x-msvideo");
    public static readonly MimeContentType WebmVideo = new("video/webm");
    public static readonly MimeContentType OggVideo = new("video/ogg");
    public static readonly MimeContentType Mov = new("video/quicktime");
    public static readonly MimeContentType Flv = new("video/x-flv");

    public static readonly MimeContentType Pdf = new("application/pdf");
    public static readonly MimeContentType MsWord = new("application/msword");
    public static readonly MimeContentType MsExcel = new("application/vnd.ms-excel");
    public static readonly MimeContentType MsPowerpoint = new("application/vnd.ms-powerpoint");
    public static readonly MimeContentType OpenDocumentText = new("application/vnd.oasis.opendocument.text");
    public static readonly MimeContentType OpenDocumentSpreadsheet = new("application/vnd.oasis.opendocument.spreadsheet");

    public static readonly MimeContentType PlainText = new(MediaTypeNames.Text.Plain);
    public static readonly MimeContentType Html = new(MediaTypeNames.Text.Html);
    public static readonly MimeContentType Css = new("text/css");
    public static readonly MimeContentType Csv = new("text/csv");
    public static readonly MimeContentType Json = new("application/json");
    public static readonly MimeContentType Xml = new("application/xml");

    public static readonly MimeContentType Zip = new("application/zip");
    public static readonly MimeContentType Rar = new("application/x-rar-compressed");
    public static readonly MimeContentType SevenZip = new("application/x-7z-compressed");
    public static readonly MimeContentType Tar = new("application/x-tar");
    public static readonly MimeContentType Gzip = new("application/gzip");

    public static readonly MimeContentType Javascript = new("application/javascript");
    public static readonly MimeContentType Php = new("application/x-httpd-php");
    public static readonly MimeContentType Python = new("text/x-python");
    public static readonly MimeContentType Java = new("text/x-java-source");

    public static readonly MimeContentType Rtf = new("application/rtf");
    public static readonly MimeContentType MultipartFormData = new("multipart/form-data");
    public static readonly MimeContentType UrlEncoded = new("application/x-www-form-urlencoded");
    public static readonly MimeContentType AtomXml = new("application/atom+xml");
    public static readonly MimeContentType RssXml = new("application/rss+xml");
}