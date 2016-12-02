// Decompiled with JetBrains decompiler
// Type: Mono.Options.ResponseFileSource
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System.Collections.Generic;

namespace Mono.Options
{
    public class ResponseFileSource : ArgumentSource
    {
        public override string Description
        {
            get { return "Read response file for more options."; }
        }

        public override string[] GetNames()
        {
            return new string[1]
            {
                "@file"
            };
        }

        public override bool GetArguments(string value, out IEnumerable<string> replacement)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("@"))
            {
                replacement = null;
                return false;
            }
            replacement = GetArgumentsFromFile(value.Substring(1));
            return true;
        }
    }
}