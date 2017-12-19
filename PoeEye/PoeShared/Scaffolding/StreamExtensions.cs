using System.IO;
using Guards;

namespace PoeShared.Scaffolding {
    public static class StreamExtensions
    {
        public static byte[] ReadToEnd(this Stream instance)
        {
            Guard.ArgumentNotNull(instance, nameof(instance));

            byte[] buffer = new byte[2048];
            using (var ms = new MemoryStream())
            {
                int bytesRead;
                while ((bytesRead = instance.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }

                return ms.ToArray();
            }
        }
    }
}