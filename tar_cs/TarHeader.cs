// Decompiled with JetBrains decompiler
// Type: tar_cs.TarHeader
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;
using System.Net;
using System.Text;

namespace tar_cs
{
    internal class TarHeader : ITarHeader
    {
        private static readonly byte[] spaces = Encoding.ASCII.GetBytes("        ");
        private readonly byte[] buffer = new byte[512];
        protected readonly DateTime TheEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private string fileName;
        private long headerChecksum;

        public TarHeader()
        {
            Mode = 511;
            UserId = 61;
            GroupId = 61;
        }

        public string ModeString
        {
            get { return Convert.ToString(Mode, 8).PadLeft(7, '0'); }
        }

        public string UserIdString
        {
            get { return Convert.ToString(UserId, 8).PadLeft(7, '0'); }
        }

        public string GroupIdString
        {
            get { return Convert.ToString(GroupId, 8).PadLeft(7, '0'); }
        }

        public string SizeString
        {
            get { return Convert.ToString(SizeInBytes, 8).PadLeft(11, '0'); }
        }

        public string LastModificationString
        {
            get { return Convert.ToString((long) (LastModification - TheEpoch).TotalSeconds, 8).PadLeft(11, '0'); }
        }

        public string HeaderChecksumString
        {
            get { return Convert.ToString(headerChecksum, 8).PadLeft(6, '0'); }
        }

        public EntryType EntryType { get; set; }

        public virtual string FileName
        {
            get { return fileName.Replace("\0", string.Empty); }
            set
            {
                if (value.Length > 100)
                    throw new TarException("A file name can not be more than 100 chars long");
                fileName = value;
            }
        }

        public int Mode { get; set; }

        public int UserId { get; set; }

        public virtual string UserName
        {
            get { return UserId.ToString(); }
            set { UserId = int.Parse(value); }
        }

        public int GroupId { get; set; }

        public virtual string GroupName
        {
            get { return GroupId.ToString(); }
            set { GroupId = int.Parse(value); }
        }

        public long SizeInBytes { get; set; }

        public DateTime LastModification { get; set; }

        public virtual int HeaderSize
        {
            get { return 512; }
        }

        public byte[] GetBytes()
        {
            return buffer;
        }

        private string TrimNulls(string input)
        {
            return input.Trim().Replace("\0", "");
        }

        public virtual bool UpdateHeaderFromBytes()
        {
            FileName = Encoding.ASCII.GetString(buffer, 0, 100);
            Mode = Convert.ToInt32(TrimNulls(Encoding.ASCII.GetString(buffer, 100, 7)), 8);
            UserId = Convert.ToInt32(TrimNulls(Encoding.ASCII.GetString(buffer, 108, 7)), 8);
            GroupId = Convert.ToInt32(TrimNulls(Encoding.ASCII.GetString(buffer, 116, 7)), 8);
            EntryType = (EntryType) buffer[156];
            SizeInBytes = ((int) buffer[124] & 128) != 128
                ? Convert.ToInt64(TrimNulls(Encoding.ASCII.GetString(buffer, 124, 11)), 8)
                : IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 128));
            LastModification =
                TheEpoch.AddSeconds(Convert.ToInt64(TrimNulls(Encoding.ASCII.GetString(buffer, 136, 11)), 8));
            var num = Convert.ToInt32(TrimNulls(Encoding.ASCII.GetString(buffer, 148, 6)));
            RecalculateChecksum(buffer);
            if (num == headerChecksum)
                return true;
            RecalculateAltChecksum(buffer);
            return num == headerChecksum;
        }

        private void RecalculateAltChecksum(byte[] buf)
        {
            spaces.CopyTo(buf, 148);
            headerChecksum = 0L;
            foreach (var num in buf)
                if ((num & 128) == 128)
                    headerChecksum -= num ^ 128;
                else
                    headerChecksum += num;
        }

        public virtual byte[] GetHeaderValue()
        {
            Array.Clear(buffer, 0, buffer.Length);
            if (string.IsNullOrEmpty(FileName))
                throw new TarException("FileName can not be empty.");
            if (FileName.Length >= 100)
                throw new TarException("FileName is too long. It must be less than 100 bytes.");
            Encoding.ASCII.GetBytes(FileName.PadRight(100, char.MinValue)).CopyTo(buffer, 0);
            Encoding.ASCII.GetBytes(ModeString).CopyTo(buffer, 100);
            Encoding.ASCII.GetBytes(UserIdString).CopyTo(buffer, 108);
            Encoding.ASCII.GetBytes(GroupIdString).CopyTo(buffer, 116);
            Encoding.ASCII.GetBytes(SizeString).CopyTo(buffer, 124);
            Encoding.ASCII.GetBytes(LastModificationString).CopyTo(buffer, 136);
            buffer[156] = (byte) EntryType;
            RecalculateChecksum(buffer);
            Encoding.ASCII.GetBytes(HeaderChecksumString).CopyTo(buffer, 148);
            return buffer;
        }

        protected virtual void RecalculateChecksum(byte[] buf)
        {
            spaces.CopyTo(buf, 148);
            headerChecksum = 0L;
            foreach (long num in buf)
                headerChecksum += num;
        }
    }
}