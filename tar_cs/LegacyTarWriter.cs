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

        /// <summary>
        /// Writes tar (see GNU tar) archive to a stream
        /// </summary>
        /// <param name="writeStream">stream to write archive to</param>
        protected LegacyTarWriter(Stream writeStream)
        {
            OutStream = writeStream;
        }

        protected virtual Stream OutStream { get; }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion


        private void WriteDirectoryEntry(string path)
        {
            if(string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (path[^1] != '/')
            {
                path += '/';
            }
            var lastWriteTime = Directory.Exists(path) ? Directory.GetLastWriteTime(path) : DateTime.Now;
            WriteHeader(path, lastWriteTime, 0, 101, 101, 0777, EntryType.Directory);
        }

        public void WriteDirectory(string directory, bool doRecursive)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));

            WriteDirectoryEntry(directory);

            string[] files = Directory.GetFiles(directory);
            foreach(var fileName in files)
            {
                Write(fileName);
            }

            string[] directories = Directory.GetDirectories(directory);
            foreach(var dirName in directories)
            {
                WriteDirectoryEntry(dirName);
                if(doRecursive)
                {
                    WriteDirectory(dirName,true);
                }
            }
        }


        private void Write(string fileName)
        {
            if(string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            using FileStream file = File.OpenRead(fileName);
            Write(file, file.Length, fileName, 61, 61, 511, File.GetLastWriteTime(file.Name));
        }

        public void Write(FileStream file)
        {
            string path = Path.GetFullPath(file.Name).Replace(Path.GetPathRoot(file.Name)!,string.Empty);
            path = path.Replace(Path.DirectorySeparatorChar, '/');
            Write(file, file.Length, path, 61, 61, 511, File.GetLastWriteTime(file.Name));
        }

        public void Write(Stream data, long dataSizeInBytes, string name)
        {
            Write(data, dataSizeInBytes, name, 61, 61, 511, DateTime.Now);
        }

        public virtual void Write(string name, long dataSizeInBytes, int userId, int groupId, int mode, DateTime lastModificationTime, WriteDataDelegate writeDelegate)
        {
            IArchiveDataWriter writer = new DataWriter(OutStream, dataSizeInBytes);
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            while(writer.CanWrite)
            {
                writeDelegate(writer);
            }
            AlignTo512(dataSizeInBytes, false);
        }

        protected virtual void Write(Stream data, long dataSizeInBytes, string name, int userId, int groupId, int mode,
                                  DateTime lastModificationTime)
        {
            if(_isClosed)
                throw new TarException("Can not write to the closed writer");
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            WriteContent(dataSizeInBytes, data);
            AlignTo512(dataSizeInBytes,false);
        }

        protected void WriteContent(long count, Stream data)
        {
            while (count > 0 && count > _buffer.Length)
            {
                var bytesRead = data.Read(_buffer, 0, _buffer.Length);
                switch (bytesRead)
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

                OutStream.Write(_buffer, 0, bytesRead);
                count -= bytesRead;
            }
            if (count > 0)
            {
                int bytesRead = data.Read(_buffer, 0, (int) count);
                if (bytesRead < 0)
                    throw new IOException("LegacyTarWriter unable to read from provided stream");
                if (bytesRead == 0)
                {
                    while (count > 0)
                    {
                        OutStream.WriteByte(0);
                        --count;
                    }
                }
                else
                    OutStream.Write(_buffer, 0, bytesRead);
            }
        }

        protected virtual void WriteHeader(string name, DateTime lastModificationTime, long count, int userId, int groupId, int mode, EntryType entryType)
        {
            var header = new TarHeader
                         {
                             FileName = name,
                             LastModification = lastModificationTime,
                             SizeInBytes = count,
                             UserId = userId,
                             GroupId = groupId,
                             Mode = mode,
                             EntryType = entryType
                         };
            OutStream.Write(header.GetHeaderValue(), 0, header.HeaderSize);
        }


        protected void AlignTo512(long size,bool acceptZero)
        {
            size %= 512;
            if (size == 0 && !acceptZero) return;
            while (size < 512)
            {
                OutStream.WriteByte(0);
                size++;
            }
        }

        protected virtual void Close()
        {
            if (_isClosed) return;
            AlignTo512(0,true);
            AlignTo512(0,true);
            _isClosed = true;
        }
    }
}