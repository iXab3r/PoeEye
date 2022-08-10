using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PoeShared.Scaffolding;

public static class StringUtils
{
    public static readonly string GzipPrefix = "GZip ";

    /// <summary>
    ///     Разделитель, который используется при формировании списков
    /// </summary>
    public static string ListDelimiter = "];[";

    public static string ToSHA1(string value)
    {
        return ToHex(Encoding.Unicode.GetBytes(value ?? string.Empty));
    }

    /// <summary>
    ///     Добавляет отформатированную строку и дополняет ее переводом каретки.
    /// </summary>
    /// <param name="_sb"></param>
    /// <param name="_format"></param>
    /// <param name="_args"></param>
    public static void AppendFormattedLine(this StringBuilder _sb, string _format, params object[] _args)
    {
        if (_sb == null) throw new ArgumentNullException(nameof(_sb));
        if (_format == null) throw new ArgumentNullException(nameof(_format));
        _sb.AppendFormat(_format, _args);
        _sb.AppendLine();
    }

    /// <summary>
    ///     Считывает NullTerminated строку используя указанную кодировку
    /// </summary>
    /// <param name="buffer"> </param>
    /// <param name="_encoding"> </param>
    /// <returns> </returns>
    public static string AsciiBytesToString(byte[] buffer, Encoding _encoding = null)
    {
        return AsciiBytesToString(buffer, 0, buffer.Length, _encoding);
    }

    /// <summary>
    ///     Считывает NullTerminated строку используя указанную кодировку
    /// </summary>
    /// <param name="buffer"> </param>
    /// <param name="offset"> </param>
    /// <param name="maxLength"> </param>
    /// <param name="_encoding"> </param>
    /// <returns> </returns>
    public static string AsciiBytesToString(byte[] buffer, int offset, int maxLength, Encoding _encoding = null)
    {
        var maxIndex = offset + maxLength;
        if (_encoding == null) _encoding = Encoding.Default;

        for (var i = offset; i < maxIndex; i++)
        {
            // Skip non-nulls.
            if (buffer[i] != 0) continue;

            // First null we find, return the string.
            return _encoding.GetString(buffer, offset, i - offset);
        }

        // Terminating null not found. Convert the entire section from offset to maxLength.
        return _encoding.GetString(buffer, offset, maxLength);
    }

    /// <summary>
    ///     Форматирует указанный байтовый размер в человекочитаемый вид
    /// </summary>
    /// <param name="_bytesCount">Размер</param>
    /// <returns></returns>
    public static string FormatBytesToString(long _bytesCount)
    {
        var buffer = new StringBuilder(256);
        StrFormatByteSize(_bytesCount, buffer, buffer.MaxCapacity);
        return buffer.ToString();
    }

    /// <summary>
    ///     Форматирует указанный XML в человекочитаемый вид
    /// </summary>
    /// <param name="_unformattedXml"></param>
    /// <returns></returns>
    public static string FormatXmlString(string _unformattedXml)
    {
        if (_unformattedXml == null) throw new ArgumentNullException("_unformattedXml");
        var stringBuilder = new StringBuilder();

        var element = XElement.Parse(_unformattedXml);

        var settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.Indent = true;
        settings.NewLineOnAttributes = false;

        using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
        {
            element.Save(xmlWriter);
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Дампит содержимое массива в HEX виде
    ///     Реализация взята отсюда http://www.codeproject.com/Articles/36747/Quick-and-Dirty-HexDump-of-a-Byte-Array
    /// </summary>
    /// <param name="_bytes">Байтовый массив</param>
    /// <param name="_bytesPerLine">Количество байт на одну строку</param>
    /// <returns>Строки вида 00000010   00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ················ </returns>
    public static string HexDump(byte[] _bytes, int _bytesPerLine = 16)
    {
        if (_bytes == null) return "<null>";
        var bytesLength = _bytes.Length;

        var HexChars = "0123456789ABCDEF".ToCharArray();

        var firstHexColumn = 8 // 8 characters for the address
                             + 3; // 3 spaces

        var firstCharColumn = firstHexColumn + _bytesPerLine * 3 // - 2 digit for the hexadecimal value and 1 space
                                             + (_bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                                             + 2; // 2 spaces 

        var lineLength = firstCharColumn + _bytesPerLine // - characters to show the ascii value
                                         + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

        var line = (new string(' ', lineLength - 2) + Environment.NewLine).ToCharArray();
        var expectedLines = (bytesLength + _bytesPerLine - 1) / _bytesPerLine;
        var result = new StringBuilder(expectedLines * lineLength);

        for (var i = 0; i < bytesLength; i += _bytesPerLine)
        {
            line[0] = HexChars[(i >> 28) & 0xF];
            line[1] = HexChars[(i >> 24) & 0xF];
            line[2] = HexChars[(i >> 20) & 0xF];
            line[3] = HexChars[(i >> 16) & 0xF];
            line[4] = HexChars[(i >> 12) & 0xF];
            line[5] = HexChars[(i >> 8) & 0xF];
            line[6] = HexChars[(i >> 4) & 0xF];
            line[7] = HexChars[(i >> 0) & 0xF];

            var hexColumn = firstHexColumn;
            var charColumn = firstCharColumn;

            for (var j = 0; j < _bytesPerLine; j++)
            {
                if (j > 0 && (j & 7) == 0) hexColumn++;
                if (i + j >= bytesLength)
                {
                    line[hexColumn] = ' ';
                    line[hexColumn + 1] = ' ';
                    line[charColumn] = ' ';
                }
                else
                {
                    var b = _bytes[i + j];
                    line[hexColumn] = HexChars[(b >> 4) & 0xF];
                    line[hexColumn + 1] = HexChars[b & 0xF];
                    line[charColumn] = b < 32 ? '·' : (char) b;
                }

                hexColumn += 3;
                charColumn++;
            }

            result.Append(line);
        }

        return result.ToString();
    }

    /// <summary>
    ///     Конвертирует массив в набор HEX данных вида 0x00
    /// </summary>
    /// <param name="_bytes"></param>
    /// <param name="_separator"></param>
    /// <returns></returns>
    public static string ToHex(byte[] _bytes, string _separator = " ")
    {
        if (_bytes == null) throw new ArgumentNullException(nameof(_bytes));
        return string.Join(_separator, _bytes.Select(x => x.ToString("X2")));
    }

    /// <summary>
    ///     Укорачивает имя файла до 8 символов
    /// </summary>
    /// <param name="_fileName"></param>
    /// <returns></returns>
    public static string ShortenFilename(string _fileName)
    {
        return ShortenFilename(_fileName, 8);
    }

    /// <summary>
    ///     Преобразовывает из HEX строки в байтовый массив
    /// </summary>
    /// <param name="_hex"></param>
    /// <returns></returns>
    public static byte[] FromHex(string _hex)
    {
        if (_hex == null) throw new ArgumentNullException(nameof(_hex));
        _hex = _hex.Replace(" ", string.Empty);
        return Enumerable.Range(0, _hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(_hex.Substring(x, 2), 16))
            .ToArray();
    }

    /// <summary>
    ///     Укорачивает имя файла до указанного числа символов, к примеру "C:\Dir1\Dir2\Dir3\LongFileName1.ext"  =>
    ///     "C:\Dir...Name1.ext"
    /// </summary>
    /// <param name="_fileName"></param>
    /// <param name="_desiredLength"></param>
    /// <returns></returns>
    public static string ShortenFilename(string _fileName, uint _desiredLength)
    {
        if (string.IsNullOrWhiteSpace(_fileName)) throw new ArgumentNullException("_fileName");
        if (_desiredLength == 0) return string.Empty;
        var fileExtension = Path.GetExtension(_fileName);
        var fileName = Path.GetFileNameWithoutExtension(_fileName);
        if (fileName.Length <= _desiredLength) return _fileName;
        var filler = "...";
        var symbolsCount = (int) (_desiredLength / 2);
        return string.Format(
            "{0}{1}{2}{3}",
            fileName.Substring(0, symbolsCount),
            filler,
            fileName.Substring(fileName.Length - symbolsCount, symbolsCount),
            fileExtension);
    }

    /// <summary>
    ///     Склеивает указанные строки с использованием ListDelimiter'а в качестве разделителя элементов
    /// </summary>
    /// <param name="_list"></param>
    /// <returns></returns>
    public static string CompressList(IEnumerable<string> _list)
    {
        if (_list == null) throw new ArgumentNullException("_list");
        return string.Join(ListDelimiter, _list);
    }

    /// <summary>
    ///     Распаковывает сжатый список с использованием ListDelimiter'а в качестве разделителя элементов
    /// </summary>
    /// <param name="_compressedList"></param>
    /// <returns></returns>
    public static IEnumerable<string> DecompressList(string _compressedList)
    {
        var result = new List<string>();
        if (!string.IsNullOrWhiteSpace(_compressedList))
        {
            var splittedString = _compressedList.Split(new[] {ListDelimiter}, StringSplitOptions.RemoveEmptyEntries);
            result.AddRange(splittedString);
        }

        return result;
    }

    /// <summary>
    ///     Пропарсивает значение _value. Если это string, тогда проверяет на True/False и 1/0, если int - только на 1/0
    /// </summary>
    /// <param name="_value">String/Int значение, которое описывает булевую сущность</param>
    /// <param name="_booleanValue"></param>
    /// <returns></returns>
    public static bool TryParseMultiBooleanValue(object _value, out bool _booleanValue)
    {
        _booleanValue = false;
        if (_value == null) return false;
        // преобразуем в строку, чтобы не заморачиваться с внутренними типами
        var stringValue = _value.ToString();
        // проверяем, не Boolean ли это
        if (bool.TryParse(stringValue, out _booleanValue)) return true;

        int intValue;
        // возможно, тогда это int ?
        if (int.TryParse(stringValue, out intValue))
        {
            _booleanValue = intValue != 0;
            return true;
        }

        return false;
    }

    public static bool IsGzip(string text)
    {
        return text.StartsWith(GzipPrefix);
    }

    /// <summary>
    ///     Compresses the string.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="includePrefix">includes 'GZip' prefix to indicate that it's compressed base64 string</param>
    /// <returns></returns>
    public static string CompressStringToGZip(string text, bool includePrefix = false)
    {
        Guard.ArgumentNotNull(() => text);

        var buffer = Encoding.UTF8.GetBytes(text);
        using var memoryStream = new MemoryStream();
        using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
        {
            gZipStream.Write(buffer, 0, buffer.Length);
        }
        var compressedBuffer = memoryStream.ToArray();
        if (includePrefix)
        {
            return $"{GzipPrefix}{Convert.ToBase64String(compressedBuffer)}";
        }
        else
        {
            return Convert.ToBase64String(compressedBuffer);
        }
    }

    /// <summary>
    ///     Decompresses the string.
    /// </summary>
    /// <param name="compressedText">The compressed text.</param>
    /// <param name="requirePrefix">Indicates whether compressed text must contain GZip prefix</param>
    /// <returns></returns>
    public static string DecompressStringFromGZip(string compressedText, bool requirePrefix = false)
    {
        Guard.ArgumentNotNull(() => compressedText);

        try
        {

            var hasPrefix = compressedText.StartsWith(GzipPrefix);
            if (requirePrefix && !hasPrefix)
            {
                throw new ArgumentException($"Provided message must start with '{GzipPrefix}', got: {compressedText}");
            }
            if (hasPrefix)
            {
                return DecompressStringFromGZip(compressedText[GzipPrefix.Length..]);
            }

            var compressedBuffer = Convert.FromBase64String(compressedText);
            using (var decompressed = new MemoryStream())
            using (var compressed = new MemoryStream(compressedBuffer))
            {
                compressed.Position = 0;
                using (var gZipStream = new GZipStream(compressed, CompressionMode.Decompress))
                {
                    gZipStream.CopyTo(decompressed);
                }

                return Encoding.UTF8.GetString(decompressed.ToArray());
            }
        }
        catch (InvalidDataException)
        {
            // try the old method as it seems that data either is malformed or compressed via legacy method
            try
            {
                var legacyResult = DecompressStringFromGZipLegacy(compressedText, requirePrefix);
                return legacyResult;
            }
            catch (Exception)
            {
                // we do not care about legacy exception
            }
            throw;
        }
    }

    /// <summary>
    ///     Парсит Enum из строки/числа, регистронезависимо
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_value"></param>
    /// <param name="_enumValue"></param>
    /// <returns></returns>
    public static bool TryParseEnum<T>(object _value, out T _enumValue) where T : struct
    {
        if (!typeof(T).IsEnum) throw new ArgumentException(string.Format("{0} is not Enum", typeof(T)));
        _enumValue = default;
        if (_value == null) return false;

        var stringValue = _value.ToString();
        return Enum.TryParse(stringValue, true, out _enumValue);
    }
        
    [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
    private static extern long StrFormatByteSize(long _fileSize, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder _buffer, int _bufferSize);
    
    
    [Obsolete("Contains an error, kept here for compatibility reasons")]
    private static string DecompressStringFromGZipLegacy(string compressedText, bool requirePrefix = false)
    {
        Guard.ArgumentNotNull(() => compressedText);

        if (requirePrefix)
        {
            if (compressedText.StartsWith(GzipPrefix))
            {
                return DecompressStringFromGZip(compressedText[GzipPrefix.Length..]);
            }
            throw new ArgumentException($"Provided message must start with '{GzipPrefix}', got: {compressedText}");
        }

        var gZipBuffer = Convert.FromBase64String(compressedText);
        using (var memoryStream = new MemoryStream())
        {
            var dataLength = BitConverter.ToInt32(gZipBuffer, 0);
            memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

            var buffer = new byte[dataLength];

            memoryStream.Position = 0;
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                gZipStream.Read(buffer, 0, buffer.Length);
            }

            return Encoding.UTF8.GetString(buffer);
        }
    }
}