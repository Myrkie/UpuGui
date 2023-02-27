using System.IO;

namespace UpuGui.tar_cs
{
    internal class DataWriter : IArchiveDataWriter
    {
        private readonly Stream _stream;
        private long _remainingBytes;

        public DataWriter(Stream data, long dataSizeInBytes)
        {
            _remainingBytes = dataSizeInBytes;
            _stream = data;
        }

        public bool CanWrite { get; private set; } = true;

        public int Write(byte[] buffer, int count)
        {
            if (_remainingBytes == 0L)
            {
                CanWrite = false;
                return -1;
            }
            var count1 = _remainingBytes - count >= 0L ? count : (int) _remainingBytes;
            _stream.Write(buffer, 0, count1);
            _remainingBytes -= count1;
            return count1;
        }
    }
}