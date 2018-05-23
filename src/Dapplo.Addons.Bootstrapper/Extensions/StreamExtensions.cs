using System.IO;

namespace Dapplo.Addons.Bootstrapper.Extensions
{
    /// <summary>
    ///     Extensions for Stream
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        ///     Create a byte array for the stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>byte array</returns>
        public static byte[] ToByteArray(this Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
