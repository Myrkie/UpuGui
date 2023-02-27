using System.IO;

namespace UpuGui.tar_cs
{
    internal class DataWriter : IArchiveDataWriter
    {
        private long _remainingBytes;
        private bool _canWrite = true;
        private readonly Stream _stream;

        public DataWriter(Stream data, long dataSizeInBytes)
        {
            _remainingBytes = dataSizeInBytes;
            _stream = data;
        }

        public int Write(byte[] buffer, int count)
        {
            if(_remainingBytes == 0)
            {
                _canWrite = false;
                return -1;
            }
            int bytesToWrite;
            if(_remainingBytes - count < 0)
            {
                bytesToWrite = (int)_remainingBytes;
            }
            else
            {
                bytesToWrite = count;
            }
            _stream.Write(buffer,0,bytesToWrite);
            _remainingBytes -= bytesToWrite;
            return bytesToWrite;
        }

        public bool CanWrite => _canWrite;
    }
}