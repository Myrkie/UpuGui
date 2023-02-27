namespace UpuGui.tar_cs
{
    public interface IArchiveDataWriter
    {
        bool CanWrite { get; }

        int Write(byte[] buffer, int count);
    }
}