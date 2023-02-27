using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace UpuGui.tar_cs
{
    /// <summary>
    /// Extract contents of a tar file represented by a stream for the TarReader constructor
    /// </summary>
    public class TarReader
    {
        private readonly byte[]? _dataBuffer = new byte[512];
        private readonly UsTarHeader _header;
        private readonly Stream _inStream;
        private long _remainingBytesInFile;

        /// <summary>
        /// Constructs TarReader object to read data from `tarredData` stream
        /// </summary>
        /// <param name="tarredData">A stream to read tar archive from</param>
        public TarReader(Stream tarredData)
        {
            _inStream = tarredData;
            _header = new UsTarHeader();
        }

        private ITarHeader FileInfo => _header;

        /// <summary>
        /// Read all files from an archive to a directory. It creates some child directories to
        /// reproduce a file structure from the archive.
        /// </summary>
        /// <param name="destDirectory">The out directory.</param>
        /// 
        /// CAUTION! This method is not safe. It's not tar-bomb proof. 
        /// {see http://en.wikipedia.org/wiki/Tar_(file_format) }
        /// If you are not sure about the source of an archive you extracting,
        /// then use MoveNext and Read and handle paths like ".." and "../.." according
        /// to your business logic.
        public void ReadToEnd(string destDirectory)
        {
            while (MoveNext(false))
            {
                var fileNameFromArchive = FileInfo.FileName;
                var totalPath = destDirectory + Path.DirectorySeparatorChar + fileNameFromArchive;
                if(UsTarHeader.IsPathSeparator(fileNameFromArchive![fileNameFromArchive.Length -1]) || FileInfo.EntryType == EntryType.Directory)
                {
                    // Record is a directory
                    Directory.CreateDirectory(totalPath);
                    continue;
                }
                // If record is a file
                var fileName = Path.GetFileName(totalPath);
                var directory = totalPath.Remove(totalPath.Length - fileName.Length);
                Directory.CreateDirectory(directory);
                using var file = File.Create(totalPath);
                Read(file);
            }
        }
        
        /// <summary>
        /// Read data from a current file to a Stream.
        /// </summary>
        /// <param name="dataDestanation">A stream to read data to</param>
        /// 
        /// <seealso cref="MoveNext"/>
        // ReSharper disable once IdentifierTypo
        private void Read(Stream dataDestanation)
        {
            Debug.WriteLine("tar stream position Read in: " + _inStream.Position);
            int readBytes;
            while ((readBytes = Read(out var read)) != -1)
            {
                Debug.WriteLine("tar stream position Read while(...) : " + _inStream.Position);
                dataDestanation.Write(read!, 0, readBytes);
            }
            Debug.WriteLine("tar stream position Read out: " + _inStream.Position);
        }

        private int Read(out byte[]? buffer)
        {
            if(_remainingBytesInFile == 0)
            {
                buffer = null;
                return -1;
            }
            var align512 = -1;
            var toRead = _remainingBytesInFile - 512;

            if (toRead > 0) 
                toRead = 512;
            else
            {
                align512 = 512 - (int)_remainingBytesInFile;
                toRead = _remainingBytesInFile;
            }
                

            var bytesRead = _inStream.Read(_dataBuffer!, 0, (int)toRead);
            _remainingBytesInFile -= bytesRead;
            
            if(_inStream.CanSeek && align512 > 0)
            {
                _inStream.Seek(align512, SeekOrigin.Current);
            }
            else
                while(align512 > 0)
                {
                    _inStream.ReadByte();
                    --align512;
                }
                
            buffer = _dataBuffer;
            return bytesRead;
        }

        /// <summary>
        /// Check if all bytes in buffer are zeroes
        /// </summary>
        /// <param name="buffer">buffer to check</param>
        /// <returns>true if all bytes are zeroes, otherwise false</returns>
        private static bool IsEmpty(IEnumerable<byte> buffer)
        {
            foreach(var b in buffer)
            {
                if (b != 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Move internal pointer to a next file in archive.
        /// </summary>
        /// <param name="skipData">Should be true if you want to read a header only, otherwise false</param>
        /// <returns>false on End Of File otherwise true</returns>
        /// 
        /// Example:
        /// while(MoveNext())
        /// { 
        ///     Read(dataDestStream); 
        /// }
        /// <seealso cref="Read(Stream)"/>
        private bool MoveNext(bool skipData)
        {
            Debug.WriteLine("tar stream position MoveNext in: " + _inStream.Position);
            if (_remainingBytesInFile > 0)
            {
                if (!skipData)
                {
                    throw new TarException(
                        "You are trying to change file while not all the data from the previous one was read. If you do want to skip files use skipData parameter set to true.");
                }
                // Skip to the end of file.
                if (_inStream.CanSeek)
                {
                    var remainer = (_remainingBytesInFile%512);
                    _inStream.Seek(_remainingBytesInFile + (512 - (remainer == 0 ? 512 : remainer) ), SeekOrigin.Current);
                }
                else
                {
                    while (Read(out _) != -1)
                    {
                    }
                }
            }

            var bytes = _header.GetBytes();

            var headerRead = _inStream.Read(bytes, 0, _header.HeaderSize);
            if (headerRead < 512)
            {
                throw new TarException("Can not read header");
            }

            if(IsEmpty(bytes))
            {
                headerRead = _inStream.Read(bytes, 0, _header.HeaderSize);
                if(headerRead == 512 && IsEmpty(bytes))
                {
                    Debug.WriteLine("tar stream position MoveNext  out(false): " + _inStream.Position);
                    return false;
                }
                throw new TarException("Broken archive");
            }

            if (_header.UpdateHeaderFromBytes())
            {
                throw new TarException("Checksum check failed");
            }

            _remainingBytesInFile = _header.SizeInBytes;

            Debug.WriteLine("tar stream position MoveNext  out(true): " + _inStream.Position);
            return true;
        }
    }
}