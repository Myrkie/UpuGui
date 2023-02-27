using System;
using System.Net;
using System.Text;

namespace UpuGui.tar_cs
{
    internal class UsTarHeader : TarHeader
    {
        private string? _groupName;
        private string _namePrefix = string.Empty;
        private string? _userName;

        public override string? UserName
        {
            get => _userName!.Replace("\0", string.Empty);
            set
            {
                if (value!.Length > 32)
                    throw new TarException("user name can not be longer than 32 chars");
                _userName = value;
            }
        }

        public override string? GroupName
        {
            get => _groupName!.Replace("\0", string.Empty);
            set
            {
                if (value!.Length > 32)
                    throw new TarException("group name can not be longer than 32 chars");
                _groupName = value;
            }
        }

        public override string? FileName
        {
            get => _namePrefix.Replace("\0", string.Empty) + base.FileName!.Replace("\0", string.Empty);
            set
            {
                if (value!.Length > 100)
                {
                    if (value.Length > byte.MaxValue)
                        throw new TarException("UsTar fileName can not be longer than 255 chars");
                    var index = value.Length - 100;
                    while (!IsPathSeparator(value[index]))
                    {
                        ++index;
                        if (index == value.Length)
                            break;
                    }
                    if (index == value.Length)
                        index = value.Length - 100;
                    _namePrefix = value.Substring(0, index);
                    base.FileName = value.Substring(index, value.Length - index);
                }
                else
                    base.FileName = value;
            }
        }

        public override bool UpdateHeaderFromBytes()
        {
            var bytes = GetBytes();
            UserName = Encoding.ASCII.GetString(bytes, 265, 32);
            GroupName = Encoding.ASCII.GetString(bytes, 297, 32);
            _namePrefix = Encoding.ASCII.GetString(bytes, 347, 157);
            return base.UpdateHeaderFromBytes();
        }

        internal static bool IsPathSeparator(char ch)
        {
            if ((ch != 92) && (ch != 47))
                return ch == 124;
            return true;
        }

        public override byte[] GetHeaderValue()
        {
            var headerValue = base.GetHeaderValue();
            // ReSharper disable once StringLiteralTypo
            Encoding.ASCII.GetBytes("ustar").CopyTo(headerValue, 257);
            Encoding.ASCII.GetBytes("  ").CopyTo(headerValue, 262);
            Encoding.ASCII.GetBytes(UserName!).CopyTo(headerValue, 265);
            Encoding.ASCII.GetBytes(GroupName!).CopyTo(headerValue, 297);
            Encoding.ASCII.GetBytes(_namePrefix).CopyTo(headerValue, 347);
            if (SizeInBytes >= 8589934591L)
                SetMarker(AlignTo12(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(SizeInBytes))))
                    .CopyTo(headerValue, 124);
            RecalculateChecksum(headerValue);
            Encoding.ASCII.GetBytes(HeaderChecksumString).CopyTo(headerValue, 148);
            return headerValue;
        }

        private static byte[] SetMarker(byte[] bytes)
        {
            bytes[0] |= 128;
            return bytes;
        }

        private static byte[] AlignTo12(byte[] bytes)
        {
            var numArray = new byte[12];
            bytes.CopyTo(numArray, 12 - bytes.Length);
            return numArray;
        }
    }
}