// Decompiled with JetBrains decompiler
// Type: UpuGui.Properties.Resources
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace UpuGui.Properties
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [DebuggerNonUserCode]
    [CompilerGenerated]
    internal class Resources
    {
        private static ResourceManager resourceMan;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (ReferenceEquals(resourceMan, null))
                    resourceMan = new ResourceManager("UpuGui.Properties.Resources", typeof(Resources).Assembly);
                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture { get; set; }

        internal static Bitmap _1405899564_archive_basic_green
        {
            get { return (Bitmap) ResourceManager.GetObject("1405899564_archive_basic_green", Culture); }
        }
    }
}