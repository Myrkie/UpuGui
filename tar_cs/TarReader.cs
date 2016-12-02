// Decompiled with JetBrains decompiler
// Type: tar_cs.TarReader
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System.Collections.Generic;
using System.IO;

namespace tar_cs
{
    public class TarReader
    {
        private readonly byte[] dataBuffer = new byte[512];
        private readonly UsTarHeader header;
        private readonly Stream inStream;
        private long remainingBytesInFile;

        public TarReader(Stream tarredData)
        {
            inStream = tarredData;
            header = new UsTarHeader();
        }

        public ITarHeader FileInfo
        {
            get { return header; }
        }

        public void ReadToEnd(string destDirectory)
        {
            while (MoveNext(false))
            {
                var fileName1 = FileInfo.FileName;
                var path = destDirectory + Path.DirectorySeparatorChar + fileName1;
                if (UsTarHeader.IsPathSeparator(fileName1[fileName1.Length - 1]) ||
                    (FileInfo.EntryType == EntryType.Directory))
                {
                    Directory.CreateDirectory(path);
                }
                else
                {
                    var fileName2 = Path.GetFileName(path);
                    Directory.CreateDirectory(path.Remove(path.Length - fileName2.Length));
                    using (var fileStream = File.Create(path))
                    {
                        Read(fileStream);
                    }
                }
            }
        }

        public void Read(Stream dataDestanation)
        {
            byte[] buffer;
            int count;
            while ((count = Read(out buffer)) != -1)
                dataDestanation.Write(buffer, 0, count);
        }

        protected int Read(out byte[] buffer)
        {
            if (remainingBytesInFile == 0L)
            {
                buffer = null;
                return -1;
            }
            var num1 = -1;
            long num2;
            if (remainingBytesInFile - 512L > 0L)
            {
                num2 = 512L;
            }
            else
            {
                num1 = 512 - (int) remainingBytesInFile;
                num2 = remainingBytesInFile;
            }
            var num3 = inStream.Read(dataBuffer, 0, (int) num2);
            remainingBytesInFile -= num3;
            if (inStream.CanSeek && (num1 > 0))
                inStream.Seek(num1, SeekOrigin.Current);
            else
                for (; num1 > 0; --num1)
                    inStream.ReadByte();
            buffer = dataBuffer;
            return num3;
        }

        private static bool IsEmpty(IEnumerable<byte> buffer)
        {
            foreach (int num in buffer)
                if (num != 0)
                    return false;
            return true;
        }

        public bool MoveNext(bool skipData)
        {
            if (remainingBytesInFile > 0L)
            {
                if (!skipData)
                    throw new TarException(
                        "You are trying to change file while not all the data from the previous one was read. If you do want to skip files use skipData parameter set to true.");
                if (inStream.CanSeek)
                {
                    var num = remainingBytesInFile%512L;
                    inStream.Seek(remainingBytesInFile + (512L - (num == 0L ? 512L : num)), SeekOrigin.Current);
                }
                else
                {
                    byte[] buffer;
                    while (Read(out buffer) != -1)
                    ;
                }
            }
            var bytes = header.GetBytes();
            if (inStream.Read(bytes, 0, header.HeaderSize) < 512)
                throw new TarException("Can not read header");
            if (IsEmpty(bytes))
            {
                if ((inStream.Read(bytes, 0, header.HeaderSize) == 512) && IsEmpty(bytes))
                    return false;
                throw new TarException("Broken archive");
            }
            if (header.UpdateHeaderFromBytes())
                throw new TarException("Checksum check failed");
            remainingBytesInFile = header.SizeInBytes;
            return true;
        }
    }
}