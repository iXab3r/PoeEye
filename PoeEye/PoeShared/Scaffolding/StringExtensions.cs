using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Guards;

namespace PoeShared.Scaffolding
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Split string into substrings using separator string
        ///     Empty items are removed and existing are trimmed
        /// </summary>
        public static string[] SplitTrim(this string str, string separator)
        {
            return str
                .Split(new[] {separator}, StringSplitOptions.RemoveEmptyEntries)
                .Select(sub => sub.Trim('\n', '\r', ' '))
                .ToArray();
        }

        public static int? ToIntOrDefault(this string str)
        {
            int result;

            if (int.TryParse(str, out result))
            {
                return result;
            }

            return null;
        }
        
        /// <summary>
        /// Perform a string split that also trims whitespace from each result and removes duplicats
        /// </summary>
        /// <param name="text"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static IEnumerable<string> SplitTrim(this string text, char separator)
        {
            var separators = new[] { separator };
            return text.SplitTrim(separators);
        }

        /// <summary>
        ///     Perform a string split that also trims whitespace from each result and removes duplicats
        /// </summary>
        /// <param name="text"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static IEnumerable<string> SplitTrim(this string text, char[] separator)
        {
            var list = (text ?? string.Empty).Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (list.Length <= 0)
            {
                yield break;
            }

            var uniqueList = new HashSet<string>();
            foreach (var item in list)
            {
                if (uniqueList.Add(item))
                {
                    yield return item.Trim();
                }
            }
        }

        public static decimal? ToDecimalOrDefault(this string str)
        {
            decimal result;

            if (decimal.TryParse(str, out result))
            {
                return result;
            }

            return null;
        }
        
        /// <summary>
        /// By default, pascalize converts strings to UpperCamelCase also removing underscores
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Pascalize(this string input)
        {
            return input.Length > 0 ? input.Substring(0, 1).ToUpper() + input.Substring(1) : input;
        }

        public static Uri ToUriOrDefault(this string str)
        {
            Uri result;
            if (!Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out result))
            {
                return default(Uri);
            }

            return result;
        }

        /// <summary>
        ///     Compresses the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static string CompressStringToGZip(this string text)
        {
            Guard.ArgumentNotNull(() => text);

            var buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
        }

        /// <summary>
        ///     Decompresses the string.
        /// </summary>
        /// <param name="compressedText">The compressed text.</param>
        /// <returns></returns>
        public static string DecompressStringFromGZip(this string compressedText)
        {
            Guard.ArgumentNotNull(() => compressedText);

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
}