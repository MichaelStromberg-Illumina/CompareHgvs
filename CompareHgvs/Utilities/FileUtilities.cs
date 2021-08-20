using System.IO;
using System.IO.Compression;

namespace CompareHgvs.Utilities
{
    public static class FileUtilities
    {
        public static FileStream GetReadStream(string path) =>
            new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        public static FileStream GetWriteStream(string path) => new(path, FileMode.Create);

        public static StreamReader StreamReader(string path) => new(GetReadStream(path));
        public static StreamWriter StreamWriter(string path) => new(GetWriteStream(path));
        
        public static StreamReader GzipReader(string path) => new(new GZipStream(GetReadStream(path), CompressionMode.Decompress));
        
        public static StreamWriter GzipWriter(string path) => new(new GZipStream(GetWriteStream(path), CompressionLevel.Optimal));
        
        public static ExtendedBinaryReader ExtendedBinaryReader(string path) => new(GetReadStream(path));
    }
}