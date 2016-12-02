// Decompiled with JetBrains decompiler
// Type: tar_cs.EntryType
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

namespace tar_cs
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