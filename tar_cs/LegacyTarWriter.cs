using System;
using System.IO;
using System.Threading;

namespace UpuGui.tar_cs
{
    public class LegacyTarWriter : IDisposable
    {
        private readonly byte[] _buffer = new byte[1024];
        private bool _isClosed;
        private const bool ReadOnZero = true;

        protected LegacyTarWriter(Stream writeStream)
        {
            OutStream = writeStream;
        }

        protected virtual Stream OutStream { get; }

        public void Dispose()
        {
            Close();
        }

        private void WriteDirectoryEntry(string? path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (path[^1] != 47)
                path += (string) (object) '/';
            var lastModificationTime = !Directory.Exists(path) ? DateTime.Now : Directory.GetLastWriteTime(path);
            WriteHeader(path, lastModificationTime, 0L, 101, 101, 777, EntryType.Directory);
        }

        public void WriteDirectory(string? directory, bool doRecursive)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));
            WriteDirectoryEntry(directory);
            foreach (var fileName in Directory.GetFiles(directory))
                Write(fileName);
            foreach (var str in Directory.GetDirectories(directory))
            {
                WriteDirectoryEntry(str);
                if (doRecursive)
                    WriteDirectory(str, true);
            }
        }

        private void Write(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            using var fileStream = File.OpenRead(fileName);
            Write(fileStream, fileStream.Length, fileName, 61, 61, 511, File.GetLastWriteTime(fileStream.Name));
        }

        public void Write(FileStream file)
        {
            var name =
                Path.GetFullPath(file.Name)
                    .Replace(Path.GetPathRoot(file.Name)!, string.Empty)
                    .Replace(Path.DirectorySeparatorChar, '/');
            Write(file, file.Length, name, 61, 61, 511, File.GetLastWriteTime(file.Name));
        }

        public void Write(Stream data, long dataSizeInBytes, string? name)
        {
            Write(data, dataSizeInBytes, name, 61, 61, 511, DateTime.Now);
        }

        public virtual void Write(string? name, long dataSizeInBytes, int userId, int groupId, int mode,
            DateTime lastModificationTime, WriteDataDelegate writeDelegate)
        {
            IArchiveDataWriter writer = new DataWriter(OutStream, dataSizeInBytes);
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            while (writer.CanWrite)
                writeDelegate(writer);
            AlignTo512(dataSizeInBytes, false);
        }

        protected virtual void Write(Stream data, long dataSizeInBytes, string? name, int userId, int groupId, int mode, DateTime lastModificationTime)
        {
            if (_isClosed)
                throw new TarException("Can not write to the closed writer");
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            WriteContent(dataSizeInBytes, data);
            AlignTo512(dataSizeInBytes, false);
        }

        protected void WriteContent(long count, Stream data)
        {
            while ((count > 0L) && (count > _buffer.Length))
            {
                var count1 = data.Read(_buffer, 0, _buffer.Length);
                switch (count1)
                {
                    case < 0:
                        throw new IOException("LegacyTarWriter unable to read from provided stream");
                    case 0:
                    {
                        if (ReadOnZero)
                            Thread.Sleep(100);
                        break;
                    }
                }

                OutStream.Write(_buffer, 0, count1);
                count -= count1;
            }
            if (count <= 0L)
                return;
            var count2 = data.Read(_buffer, 0, (int) count);
            switch (count2)
            {
                case < 0:
                    throw new IOException("LegacyTarWriter unable to read from provided stream");
                case 0:
                {
                    for (; count > 0L; --count)
                        OutStream.WriteByte(0);
                    break;
                }
                default:
                    OutStream.Write(_buffer, 0, count2);
                    break;
            }
        }

        protected virtual void WriteHeader(string? name, DateTime lastModificationTime, long count, int userId,
            int groupId, int mode, EntryType entryType)
        {
            var tarHeader = new TarHeader
            {
                FileName = name,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userId,
                GroupId = groupId,
                Mode = mode,
                EntryType = entryType
            };
            OutStream.Write(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }

        protected void AlignTo512(long size, bool acceptZero)
        {
            size %= 512L;
            if ((size == 0L) && !acceptZero)
                return;
            for (; size < 512L; ++size)
                OutStream.WriteByte(0);
        }

        protected virtual void Close()
        {
            if (_isClosed)
                return;
            AlignTo512(0L, true);
            AlignTo512(0L, true);
            _isClosed = true;
        }
    }
}