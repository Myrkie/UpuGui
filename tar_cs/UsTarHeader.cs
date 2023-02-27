using System;
using System.Net;
using System.Text;

namespace UpuGui.tar_cs
{
    /// <summary>
    /// UsTar header implementation.
    /// </summary>
    internal class UsTarHeader : TarHeader
    {
        private const string Magic = "ustar";
        private const string Version = "  ";
        private string? _groupName;
        private string _namePrefix = string.Empty;
        private string? _userName;

        public override string? UserName
        {
            get => _userName!.Replace("\0",string.Empty);
            set
            {
                if (value!.Length > 32)
                    throw new TarException("user name can not be longer than 32 chars");
                _userName = value;
            }
        }

        public override string? GroupName
        {
            get => _groupName!.Replace("\0",string.Empty);
            set
            {
                if (value!.Length > 32)
                    throw new TarException("group name can not be longer than 32 chars");
                _groupName = value;
            }
        }

        public override string? FileName
        {
            get => _namePrefix.Replace("\0", string.Empty) + base.FileName.Replace("\0", string.Empty);
            set
            {
                if (value.Length > 100)
                {
                    if (value.Length > 255)
                    {
                        throw new TarException("UsTar fileName can not be longer thatn 255 chars");
                    }
                    int position = value.Length - 100;

                    // Find first path separator in the remaining 100 chars of the file name
                    while (!IsPathSeparator(value[position]))
                    {
                        ++position;
                        if (position == value.Length)
                            break;
                    }
                    if (position == value.Length)
                        position = value.Length - 100;
                    _namePrefix = value.Substring(0, position);
                    base.FileName = value.Substring(position, value.Length - position);
                }
                else base.FileName = value;
            }
        }

        public override bool UpdateHeaderFromBytes()
        {
            var bytes = GetBytes();
            UserName = Encoding.ASCII.GetString(bytes, 0x109, 32);
            GroupName = Encoding.ASCII.GetString(bytes, 0x129, 32);
            _namePrefix = Encoding.ASCII.GetString(bytes, 347, 157);
            return base.UpdateHeaderFromBytes();
        }

        internal static bool IsPathSeparator(char ch)
        {
            return (ch == '\\' || ch == '/' || ch == '|'); // All the path separators I ever met.
        }

        public override byte[] GetHeaderValue()
        {
            var header = base.GetHeaderValue();

            Encoding.ASCII.GetBytes(Magic).CopyTo(header, 0x101); // Mark header as ustar
            Encoding.ASCII.GetBytes(Version).CopyTo(header, 0x106);
            Encoding.ASCII.GetBytes(UserName!).CopyTo(header, 0x109);
            Encoding.ASCII.GetBytes(GroupName!).CopyTo(header, 0x129);
            Encoding.ASCII.GetBytes(_namePrefix).CopyTo(header, 347);
            if (SizeInBytes >= 0x1FFFFFFFF)
            {
                var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(SizeInBytes));
                SetMarker(AlignTo12(bytes)).CopyTo(header, 124);
            }
            RecalculateChecksum(header);
            Encoding.ASCII.GetBytes(HeaderChecksumString).CopyTo(header, 148);
            return header;
        }

        private static byte[] SetMarker(byte[] bytes)
        {
            bytes[0] |= 0x80;
            return bytes;
        }

        private static byte[] AlignTo12(byte[] bytes)
        {
            var retVal = new byte[12];
            bytes.CopyTo(retVal, 12 - bytes.Length);
            return retVal;
        }
    }
}