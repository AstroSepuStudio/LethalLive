using System.IO;
using System.IO.Compression;

namespace SimpleVoiceChat {

    /// <summary>
    /// Used to compress and decompress a byte arrays.
    /// </summary>
    public static class AudioCompressor {

        public static byte[] Compress(byte[] data) {
            MemoryStream memStream = new MemoryStream();
            using (GZipStream gZip = new GZipStream(memStream, CompressionLevel.Optimal)) {
                gZip.Write(data, 0, data.Length);
            }
            return memStream.ToArray();
        }
        
        public static byte[] Decompress(byte[] data) {
            MemoryStream inputStream = new MemoryStream(data);
            MemoryStream resultStream = new MemoryStream();
            using (GZipStream gZip = new GZipStream(inputStream, CompressionMode.Decompress)) {
                gZip.CopyTo(resultStream);
            }
            return resultStream.ToArray();
        }
    }
}