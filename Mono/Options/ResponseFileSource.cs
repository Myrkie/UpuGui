﻿using System.Collections.Generic;

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