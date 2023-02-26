using System.IO;

namespace tar_cs
{
    internal class DataWriter : IArchiveDataWriter
    {
        private readonly long size;
        private readonly Stream stream;
        private long remainingBytes;

        public DataWriter(Stream data, long dataSizeInBytes)
        {
            size = dataSizeInBytes;
            remainingBytes = size;
            stream = data;
        }

        public bool CanWrite { get; private set; } = true;

        public int Write(byte[] buffer, int count)
        {
            if (remainingBytes == 0L)
            {
                CanWrite = false;
                return -1;
            }
            var count1 = remainingBytes - (long) count >= 0L ? count : (int) remainingBytes;
            stream.Write(buffer, 0, count1);
            remainingBytes -= count1;
            return count1;
        }
    }
}