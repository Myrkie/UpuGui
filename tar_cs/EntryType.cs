namespace UpuGui.tar_cs
{
    public enum EntryType : byte
    {
        File = 0,
        FileObsolete = 48,
        HardLink = 49,
        SymLink = 50,
        CharDevice = 51,
        BlockDevice = 52,
        Directory = 53,
        Fifo = 54
    }
}