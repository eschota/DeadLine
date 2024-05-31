using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

 
    public static class Converter
    {
    public static string ConvertToCompressedBase64(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
    public static string DecompressFromBase64(string compressedBase64)
    {
        byte[] compressedBytes = Convert.FromBase64String(compressedBase64);
        using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
        {
            using (MemoryStream resultStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(resultStream);
                }
                return Encoding.UTF8.GetString(resultStream.ToArray());
            }
        }
    }
} 