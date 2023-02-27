using System;
using System.IO;

namespace UpuGui.tar_cs
{
    public class TarWriter : LegacyTarWriter
    {
        public TarWriter(Stream writeStream)
            : base(writeStream)
        {
        }

        protected override void WriteHeader(string? name, DateTime lastModificationTime, long count, int userId,
            int groupId, int mode, EntryType entryType)
        {
            var usTarHeader1 = new UsTarHeader
            {
                FileName = name,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userId,
                UserName = Convert.ToString(userId, 8),
                GroupId = groupId,
                GroupName = Convert.ToString(groupId, 8),
                Mode = mode,
                EntryType = entryType
            };
            var usTarHeader2 = usTarHeader1;
            OutStream.Write(usTarHeader2.GetHeaderValue(), 0, usTarHeader2.HeaderSize);
        }

        protected virtual void WriteHeader(string? name, DateTime lastModificationTime, long count, string userName,
            string groupName, int mode)
        {
            var usTarHeader1 = new UsTarHeader
            {
                FileName = name,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userName.GetHashCode(),
                UserName = userName,
                GroupId = groupName.GetHashCode(),
                GroupName = groupName,
                Mode = mode
            };
            var usTarHeader2 = usTarHeader1;
            OutStream.Write(usTarHeader2.GetHeaderValue(), 0, usTarHeader2.HeaderSize);
        }

        public virtual void Write(string? name, long dataSizeInBytes, string userName, string groupName, int mode,
            DateTime lastModificationTime, WriteDataDelegate writeDelegate)
        {
            var dataWriter = new DataWriter(OutStream, dataSizeInBytes);
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userName, groupName, mode);
            while (dataWriter.CanWrite)
                writeDelegate(dataWriter);
            AlignTo512(dataSizeInBytes, false);
        }

        public void Write(Stream data, long dataSizeInBytes, string? fileName, string userId, string groupId, int mode,
            DateTime lastModificationTime)
        {
            WriteHeader(fileName, lastModificationTime, dataSizeInBytes, userId, groupId, mode);
            WriteContent(dataSizeInBytes, data);
            AlignTo512(dataSizeInBytes, false);
        }
    }
}