using System;
using System.Net;
using System.Text;

namespace UpuGui.tar_cs
{
    internal class TarHeader : ITarHeader
    {
        private static readonly byte[] Spaces = Encoding.ASCII.GetBytes("        ");
        private readonly byte[] _buffer = new byte[512];
        private readonly DateTime _theEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private string? _fileName;
        private long _headerChecksum;

        public TarHeader()
        {
            Mode = 511;
            UserId = 61;
            GroupId = 61;
        }

        private string ModeString => Convert.ToString(Mode, 8).PadLeft(7, '0');

        private string UserIdString => Convert.ToString(UserId, 8).PadLeft(7, '0');

        private string GroupIdString => Convert.ToString(GroupId, 8).PadLeft(7, '0');

        private string SizeString => Convert.ToString(SizeInBytes, 8).PadLeft(11, '0');

        private string LastModificationString => Convert.ToString((long) (LastModification - _theEpoch).TotalSeconds, 8).PadLeft(11, '0');

        protected string HeaderChecksumString => Convert.ToString(_headerChecksum, 8).PadLeft(6, '0');

        public EntryType EntryType { get; set; }

        public virtual string? FileName
        {
            get => _fileName?.Replace("\0", string.Empty);
            set
            {
                if (value is { Length: > 100 })
                    throw new TarException("A file name can not be more than 100 chars long");
                _fileName = value;
            }
        }

        public int Mode { get; set; }

        public int UserId { get; set; }

        public virtual string? UserName
        {
            get => UserId.ToString();
            set => UserId = int.Parse(value!);
        }

        public int GroupId { get; set; }

        public virtual string? GroupName
        {
            get => GroupId.ToString();
            set => GroupId = int.Parse(value!);
        }

        public long SizeInBytes { get; set; }

        public DateTime LastModification { get; set; }

        public virtual int HeaderSize => 512;

        public byte[] GetBytes()
        {
            return _buffer;
        }

        private string TrimNulls(string input)
        {
            return input.Trim().Replace("\0", "");
        }

        public virtual bool UpdateHeaderFromBytes()
        {
            FileName = Encoding.ASCII.GetString(_buffer, 0, 100);
            Mode = Convert.ToInt32(TrimNulls(Encoding.ASCII.GetString(_buffer, 100, 7)), 8);
            UserId = Convert.ToInt32(TrimNulls(Encoding.ASCII.GetString(_buffer, 108, 7)), 8);
            GroupId = Convert.ToInt32(TrimNulls(Encoding.ASCII.GetString(_buffer, 116, 7)), 8);
            EntryType = (EntryType) _buffer[156];
            SizeInBytes = (_buffer[124] & 128) != 128
                ? Convert.ToInt64(TrimNulls(Encoding.ASCII.GetString(_buffer, 124, 11)), 8)
                : IPAddress.NetworkToHostOrder(BitConverter.ToInt64(_buffer, 128));
            LastModification =
                _theEpoch.AddSeconds(Convert.ToInt64(TrimNulls(Encoding.ASCII.GetString(_buffer, 136, 11)), 8));
            var num = Convert.ToInt32(TrimNulls(Encoding.ASCII.GetString(_buffer, 148, 6)));
            RecalculateChecksum(_buffer);
            if (num == _headerChecksum)
                return true;
            RecalculateAltChecksum(_buffer);
            return num == _headerChecksum;
        }

        private void RecalculateAltChecksum(byte[] buf)
        {
            Spaces.CopyTo(buf, 148);
            _headerChecksum = 0L;
            foreach (var num in buf)
                if ((num & 128) == 128)
                    _headerChecksum -= num ^ 128;
                else
                    _headerChecksum += num;
        }

        public virtual byte[] GetHeaderValue()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            if (string.IsNullOrEmpty(FileName))
                throw new TarException("FileName can not be empty.");
            if (FileName.Length >= 100)
                throw new TarException("FileName is too long. It must be less than 100 bytes.");
            Encoding.ASCII.GetBytes(FileName.PadRight(100, char.MinValue)).CopyTo(_buffer, 0);
            Encoding.ASCII.GetBytes(ModeString).CopyTo(_buffer, 100);
            Encoding.ASCII.GetBytes(UserIdString).CopyTo(_buffer, 108);
            Encoding.ASCII.GetBytes(GroupIdString).CopyTo(_buffer, 116);
            Encoding.ASCII.GetBytes(SizeString).CopyTo(_buffer, 124);
            Encoding.ASCII.GetBytes(LastModificationString).CopyTo(_buffer, 136);
            _buffer[156] = (byte) EntryType;
            RecalculateChecksum(_buffer);
            Encoding.ASCII.GetBytes(HeaderChecksumString).CopyTo(_buffer, 148);
            return _buffer;
        }

        protected virtual void RecalculateChecksum(byte[] buf)
        {
            Spaces.CopyTo(buf, 148);
            _headerChecksum = 0L;
            foreach (long num in buf)
                _headerChecksum += num;
        }
    }
}