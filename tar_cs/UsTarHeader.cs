// Decompiled with JetBrains decompiler
// Type: tar_cs.UsTarHeader
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;
using System.Net;
using System.Text;

namespace tar_cs
{
    internal class UsTarHeader : TarHeader
    {
        private const string magic = "ustar";
        private const string version = "  ";
        private string groupName;
        private string namePrefix = string.Empty;
        private string userName;

        public override string UserName
        {
            get { return userName.Replace("\0", string.Empty); }
            set
            {
                if (value.Length > 32)
                    throw new TarException("user name can not be longer than 32 chars");
                userName = value;
            }
        }

        public override string GroupName
        {
            get { return groupName.Replace("\0", string.Empty); }
            set
            {
                if (value.Length > 32)
                    throw new TarException("group name can not be longer than 32 chars");
                groupName = value;
            }
        }

        public override string FileName
        {
            get { return namePrefix.Replace("\0", string.Empty) + base.FileName.Replace("\0", string.Empty); }
            set
            {
                if (value.Length > 100)
                {
                    if (value.Length > byte.MaxValue)
                        throw new TarException("UsTar fileName can not be longer thatn 255 chars");
                    var index = value.Length - 100;
                    while (!IsPathSeparator(value[index]))
                    {
                        ++index;
                        if (index == value.Length)
                            break;
                    }
                    if (index == value.Length)
                        index = value.Length - 100;
                    namePrefix = value.Substring(0, index);
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
            namePrefix = Encoding.ASCII.GetString(bytes, 347, 157);
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
            Encoding.ASCII.GetBytes("ustar").CopyTo(headerValue, 257);
            Encoding.ASCII.GetBytes("  ").CopyTo(headerValue, 262);
            Encoding.ASCII.GetBytes(UserName).CopyTo(headerValue, 265);
            Encoding.ASCII.GetBytes(GroupName).CopyTo(headerValue, 297);
            Encoding.ASCII.GetBytes(namePrefix).CopyTo(headerValue, 347);
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