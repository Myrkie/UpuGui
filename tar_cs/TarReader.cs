using System.Collections.Generic;
using System.IO;

namespace UpuGui.tar_cs
{
    public class TarReader
    {
        private readonly byte[]? _dataBuffer = new byte[512];
        private readonly UsTarHeader _header;
        private readonly Stream _inStream;
        private long _remainingBytesInFile;

        public TarReader(Stream tarredData)
        {
            _inStream = tarredData;
            _header = new UsTarHeader();
        }

        private ITarHeader FileInfo => _header;

        public void ReadToEnd(string destDirectory)
        {
            while (MoveNext(false))
            {
                var fileName1 = FileInfo.FileName;
                var path = destDirectory + Path.DirectorySeparatorChar + fileName1;
                if (fileName1 != null && (UsTarHeader.IsPathSeparator(fileName1[^1]) ||
                                          (FileInfo.EntryType == EntryType.Directory)))
                {
                    Directory.CreateDirectory(path);
                }
                else
                {
                    var fileName2 = Path.GetFileName(path);
                    Directory.CreateDirectory(path.Remove(path.Length - fileName2.Length));
                    using var fileStream = File.Create(path);
                    Read(fileStream);
                }
            }
        }

        // ReSharper disable once IdentifierTypo
        private void Read(Stream dataDestanation)
        {
            int count;
            while ((count = Read(out var buffer)) != -1)
                dataDestanation.Write(buffer!, 0, count);
        }

        private int Read(out byte[]? buffer)
        {
            if (_remainingBytesInFile == 0L)
            {
                buffer = null;
                return -1;
            }
            var num1 = -1;
            long num2;
            if (_remainingBytesInFile - 512L > 0L)
            {
                num2 = 512L;
            }
            else
            {
                num1 = 512 - (int) _remainingBytesInFile;
                num2 = _remainingBytesInFile;
            }
            var num3 = _inStream.Read(_dataBuffer!, 0, (int) num2);
            _remainingBytesInFile -= num3;
            if (_inStream.CanSeek && (num1 > 0))
                _inStream.Seek(num1, SeekOrigin.Current);
            else
                for (; num1 > 0; --num1)
                    _inStream.ReadByte();
            buffer = _dataBuffer;
            return num3;
        }

        private static bool IsEmpty(IEnumerable<byte> buffer)
        {
            foreach (int num in buffer)
                if (num != 0)
                    return false;
            return true;
        }

        private bool MoveNext(bool skipData)
        {
            if (_remainingBytesInFile > 0L)
            {
                if (!skipData)
                    throw new TarException(
                        "You are trying to change file while not all the data from the previous one was read. If you do want to skip files use skipData parameter set to true.");
                if (_inStream.CanSeek)
                {
                    var num = _remainingBytesInFile%512L;
                    _inStream.Seek(_remainingBytesInFile + (512L - (num == 0L ? 512L : num)), SeekOrigin.Current);
                }
            }
            var bytes = _header.GetBytes();
            if (_inStream.Read(bytes, 0, _header.HeaderSize) < 512)
                throw new TarException("Can not read header");
            if (IsEmpty(bytes))
            {
                if ((_inStream.Read(bytes, 0, _header.HeaderSize) == 512) && IsEmpty(bytes))
                    return false;
                throw new TarException("Broken archive");
            }
            if (_header.UpdateHeaderFromBytes())
                throw new TarException("Checksum check failed");
            _remainingBytesInFile = _header.SizeInBytes;
            return true;
        }
    }
}