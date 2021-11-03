using System.IO;
using System.Threading.Tasks;


namespace PoeShared.Scaffolding
{
    public static class StreamExtensions
    {
        public static async Task<byte[]> ReadToEndAsync(this Stream instance)
        {
            Guard.ArgumentNotNull(instance, nameof(instance));

            var buffer = new byte[2048];
            await using var ms = new MemoryStream();
            int bytesRead;
                
            while ((bytesRead = await instance.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, bytesRead);
            }

            return ms.ToArray();
        }

        public static byte[] ReadToEnd(this Stream instance)
        {
            Guard.ArgumentNotNull(instance, nameof(instance));

            var buffer = new byte[2048];
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