// Decompiled with JetBrains decompiler
// Type: tar_cs.TarWriter
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;
using System.IO;

namespace tar_cs
{
    public class TarWriter : LegacyTarWriter
    {
        public TarWriter(Stream writeStream)
            : base(writeStream)
        {
        }

        protected override void WriteHeader(string name, DateTime lastModificationTime, long count, int userId,
            int groupId, int mode, EntryType entryType)
        {
            var usTarHeader1 = new UsTarHeader();
            usTarHeader1.FileName = name;
            usTarHeader1.LastModification = lastModificationTime;
            usTarHeader1.SizeInBytes = count;
            usTarHeader1.UserId = userId;
            usTarHeader1.UserName = Convert.ToString(userId, 8);
            usTarHeader1.GroupId = groupId;
            usTarHeader1.GroupName = Convert.ToString(groupId, 8);
            usTarHeader1.Mode = mode;
            usTarHeader1.EntryType = entryType;
            var usTarHeader2 = usTarHeader1;
            OutStream.Write(usTarHeader2.GetHeaderValue(), 0, usTarHeader2.HeaderSize);
        }

        protected virtual void WriteHeader(string name, DateTime lastModificationTime, long count, string userName,
            string groupName, int mode)
        {
            var usTarHeader1 = new UsTarHeader();
            usTarHeader1.FileName = name;
            usTarHeader1.LastModification = lastModificationTime;
            usTarHeader1.SizeInBytes = count;
            usTarHeader1.UserId = userName.GetHashCode();
            usTarHeader1.UserName = userName;
            usTarHeader1.GroupId = groupName.GetHashCode();
            usTarHeader1.GroupName = groupName;
            usTarHeader1.Mode = mode;
            var usTarHeader2 = usTarHeader1;
            OutStream.Write(usTarHeader2.GetHeaderValue(), 0, usTarHeader2.HeaderSize);
        }

        public virtual void Write(string name, long dataSizeInBytes, string userName, string groupName, int mode,
            DateTime lastModificationTime, WriteDataDelegate writeDelegate)
        {
            var dataWriter = new DataWriter(OutStream, dataSizeInBytes);
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userName, groupName, mode);
            while (dataWriter.CanWrite)
                writeDelegate(dataWriter);
            AlignTo512(dataSizeInBytes, false);
        }

        public void Write(Stream data, long dataSizeInBytes, string fileName, string userId, string groupId, int mode,
            DateTime lastModificationTime)
        {
            WriteHeader(fileName, lastModificationTime, dataSizeInBytes, userId, groupId, mode);
            WriteContent(dataSizeInBytes, data);
            AlignTo512(dataSizeInBytes, false);
        }
    }
}