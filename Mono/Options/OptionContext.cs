// Decompiled with JetBrains decompiler
// Type: Mono.Options.OptionContext
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

namespace Mono.Options
{
    public class OptionContext
    {
        public OptionContext(OptionSet set)
        {
            OptionSet = set;
            OptionValues = new OptionValueCollection(this);
        }

        public Option Option { get; set; }

        public string OptionName { get; set; }

        public int OptionIndex { get; set; }

        public OptionSet OptionSet { get; }

        public OptionValueCollection OptionValues { get; }
    }
}