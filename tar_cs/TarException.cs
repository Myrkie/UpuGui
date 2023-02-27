using System;

namespace UpuGui.tar_cs
{
    public class TarException : Exception
    {
        public TarException(string message)
            : base(message)
        {
        }
    }
}