// Decompiled with JetBrains decompiler
// Type: tar_cs.LegacyTarWriter
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;
using System.IO;
using System.Threading;

namespace tar_cs
{
    public class LegacyTarWriter : IDisposable
    {
        protected byte[] buffer = new byte[1024];
        private bool isClosed;
        public bool ReadOnZero = true;

        public LegacyTarWriter(Stream writeStream)
        {
            OutStream = writeStream;
        }

        protected virtual Stream OutStream { get; }

        public void Dispose()
        {
            Close();
        }

        public void WriteDirectoryEntry(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (path[path.Length - 1] != 47)
                path += (string) (object) '/';
            var lastModificationTime = !Directory.Exists(path) ? DateTime.Now : Directory.GetLastWriteTime(path);
            WriteHeader(path, lastModificationTime, 0L, 101, 101, 777, EntryType.Directory);
        }

        public void WriteDirectory(string directory, bool doRecursive)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException("directory");
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

        public void Write(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");
            using (var fileStream = File.OpenRead(fileName))
            {
                Write(fileStream, fileStream.Length, fileName, 61, 61, 511, File.GetLastWriteTime(fileStream.Name));
            }
        }

        public void Write(FileStream file)
        {
            var name =
                Path.GetFullPath(file.Name)
                    .Replace(Path.GetPathRoot(file.Name), string.Empty)
                    .Replace(Path.DirectorySeparatorChar, '/');
            Write(file, file.Length, name, 61, 61, 511, File.GetLastWriteTime(file.Name));
        }

        public void Write(Stream data, long dataSizeInBytes, string name)
        {
            Write(data, dataSizeInBytes, name, 61, 61, 511, DateTime.Now);
        }

        public virtual void Write(string name, long dataSizeInBytes, int userId, int groupId, int mode,
            DateTime lastModificationTime, WriteDataDelegate writeDelegate)
        {
            IArchiveDataWriter writer = new DataWriter(OutStream, dataSizeInBytes);
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            while (writer.CanWrite)
                writeDelegate(writer);
            AlignTo512(dataSizeInBytes, false);
        }

        public virtual void Write(Stream data, long dataSizeInBytes, string name, int userId, int groupId, int mode,
            DateTime lastModificationTime)
        {
            if (isClosed)
                throw new TarException("Can not write to the closed writer");
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            WriteContent(dataSizeInBytes, data);
            AlignTo512(dataSizeInBytes, false);
        }

        protected void WriteContent(long count, Stream data)
        {
            while ((count > 0L) && (count > buffer.Length))
            {
                var count1 = data.Read(buffer, 0, buffer.Length);
                if (count1 < 0)
                    throw new IOException("LegacyTarWriter unable to read from provided stream");
                if (count1 == 0)
                    if (ReadOnZero)
                        Thread.Sleep(100);
                    else
                        break;
                OutStream.Write(buffer, 0, count1);
                count -= count1;
            }
            if (count <= 0L)
                return;
            var count2 = data.Read(buffer, 0, (int) count);
            if (count2 < 0)
                throw new IOException("LegacyTarWriter unable to read from provided stream");
            if (count2 == 0)
                for (; count > 0L; --count)
                    OutStream.WriteByte(0);
            else
                OutStream.Write(buffer, 0, count2);
        }

        protected virtual void WriteHeader(string name, DateTime lastModificationTime, long count, int userId,
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

        public void AlignTo512(long size, bool acceptZero)
        {
            size %= 512L;
            if ((size == 0L) && !acceptZero)
                return;
            for (; size < 512L; ++size)
                OutStream.WriteByte(0);
        }

        public virtual void Close()
        {
            if (isClosed)
                return;
            AlignTo512(0L, true);
            AlignTo512(0L, true);
            isClosed = true;
        }
    }
}