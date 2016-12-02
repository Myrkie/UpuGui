// Decompiled with JetBrains decompiler
// Type: tar_cs.ITarHeader
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;

namespace tar_cs
{
    public interface ITarHeader
    {
        string FileName { get; set; }

        int Mode { get; set; }

        int UserId { get; set; }

        string UserName { get; set; }

        int GroupId { get; set; }

        string GroupName { get; set; }

        long SizeInBytes { get; set; }

        DateTime LastModification { get; set; }

        int HeaderSize { get; }

        EntryType EntryType { get; set; }
    }
}