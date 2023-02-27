using System;
using System.Net;
using System.Text;

namespace UpuGui.tar_cs
{
    internal class TarHeader : ITarHeader
    {
        private readonly byte[] _buffer = new byte[512];
        private long _headerChecksum;

        public TarHeader()
        {
            // Default values
            Mode = 511; // 0777 dec
            UserId = 61; // 101 dec
            GroupId = 61; // 101 dec
        }

        private string? _fileName;
        private readonly DateTime _theEpoch = new(1970, 1, 1, 0, 0, 0);
        public EntryType EntryType { get; set; }
        private static readonly byte[] Spaces = "        "u8.ToArray();

        public virtual string? FileName
        {
            get => _fileName!.Replace("\0",string.Empty);
            set
            {
                if(value!.Length > 100)
                {
                    throw new TarException("A file name can not be more than 100 chars long");
                }
                _fileName = value;
            }
        }
        public int Mode { get; set; }

        private string ModeString => Convert.ToString(Mode, 8).PadLeft(7, '0');

        public int UserId { get; set; }
        public virtual string? UserName
        {
            get => UserId.ToString();
            set => UserId = int.Parse(value!);
        }

        private string UserIdString => Convert.ToString(UserId, 8).PadLeft(7, '0');

        public int GroupId { get; set; }
        public virtual string? GroupName
        {
            get => GroupId.ToString();
            set => GroupId = int.Parse(value!);
        }

        private string GroupIdString => Convert.ToString(GroupId, 8).PadLeft(7, '0');

        public long SizeInBytes { get; set; }

        private string SizeString => Convert.ToString(SizeInBytes, 8).PadLeft(11, '0');

        public DateTime LastModification { get; set; }

        private string LastModificationString => ((long)(LastModification - _theEpoch).TotalSeconds).ToString("D11");

        protected string HeaderChecksumString => Convert.ToString(_headerChecksum, 8).PadLeft(6, '0');


        public virtual int HeaderSize => 512;

        public byte[] GetBytes()
        {
            return _buffer;
        }

        public virtual bool UpdateHeaderFromBytes()
        {
            FileName = Encoding.ASCII.GetString(_buffer, 0, 100);
            Mode = Convert.ToInt32(Encoding.ASCII.GetString(_buffer, 100, 7).Trim(), 8);
            UserId = Convert.ToInt32(Encoding.ASCII.GetString(_buffer, 108, 7).Trim(), 8);
            GroupId = Convert.ToInt32(Encoding.ASCII.GetString(_buffer, 116, 7).Trim(), 8);

            EntryType = (EntryType)_buffer[156];

            if((_buffer[124] & 0x80) == 0x80) // if size in binary
            {
                var sizeBigEndian = BitConverter.ToInt64(_buffer,0x80);
                SizeInBytes = IPAddress.NetworkToHostOrder(sizeBigEndian);
            }
            else
            {
                SizeInBytes = Convert.ToInt64(Encoding.ASCII.GetString(_buffer, 124, 11), 8);
            }
            var unixTimeStamp = Convert.ToInt64(Encoding.ASCII.GetString(_buffer,136,11),8);
            LastModification = _theEpoch.AddSeconds(unixTimeStamp);

            var storedChecksum = Convert.ToInt32(Encoding.ASCII.GetString(_buffer,148,6));
            RecalculateChecksum(_buffer);
            if (storedChecksum == _headerChecksum)
            {
                return true;
            }

            RecalculateAltChecksum(_buffer);
            return storedChecksum == _headerChecksum;
        }

        private void RecalculateAltChecksum(byte[] buf)
        {
            Spaces.CopyTo(buf, 148);
            _headerChecksum = 0;
            foreach(var b in buf)
            {
                if((b & 0x80) == 0x80)
                {
                    _headerChecksum -= b ^ 0x80;
                }
                else
                {
                    _headerChecksum += b;
                }
            }
        }

        public virtual byte[] GetHeaderValue()
        {
            // Clean old values
            Array.Clear(_buffer,0, _buffer.Length);

            if (string.IsNullOrEmpty(FileName)) throw new TarException("FileName can not be empty.");
            if (FileName.Length >= 100) throw new TarException("FileName is too long. It must be less than 100 bytes.");

            // Fill header
            Encoding.ASCII.GetBytes(FileName.PadRight(100, '\0')).CopyTo(_buffer, 0);
            Encoding.ASCII.GetBytes(ModeString).CopyTo(_buffer, 100);
            Encoding.ASCII.GetBytes(UserIdString).CopyTo(_buffer, 108);
            Encoding.ASCII.GetBytes(GroupIdString).CopyTo(_buffer, 116);
            Encoding.ASCII.GetBytes(SizeString).CopyTo(_buffer, 124);
            Encoding.ASCII.GetBytes(LastModificationString).CopyTo(_buffer, 136);

//            buffer[156] = 20;
            _buffer[156] = ((byte) EntryType);


            RecalculateChecksum(_buffer);

            // Write checksum
            Encoding.ASCII.GetBytes(HeaderChecksumString).CopyTo(_buffer, 148);

            return _buffer;
        }

        protected virtual void RecalculateChecksum(byte[] buf)
        {
            // Set default value for checksum. That is 8 spaces.
            Spaces.CopyTo(buf, 148);

            // Calculate checksum
            _headerChecksum = 0;
            foreach (byte b in buf)
            {
                _headerChecksum += b;
            }
        }
    }
}