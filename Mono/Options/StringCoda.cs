// Decompiled with JetBrains decompiler
// Type: Mono.Options.StringCoda
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;
using System.Collections.Generic;

namespace Mono.Options
{
    internal static class StringCoda
    {
        public static IEnumerable<string> WrappedLines(string self, params int[] widths)
        {
            IEnumerable<int> widths1 = widths;
            return WrappedLines(self, widths1);
        }

        public static IEnumerable<string> WrappedLines(string self, IEnumerable<int> widths)
        {
            if (widths == null)
                throw new ArgumentNullException("widths");
            return CreateWrappedLinesIterator(self, widths);
        }

        private static IEnumerable<string> CreateWrappedLinesIterator(string self, IEnumerable<int> widths)
        {
            if (string.IsNullOrEmpty(self))
                yield return string.Empty;
            else
                using (var enumerator = widths.GetEnumerator())
                {
                    var hw = new bool?();
                    var width = GetNextWidth(enumerator, int.MaxValue, ref hw);
                    var start = 0;
                    do
                    {
                        var end = GetLineEnd(start, width, self);
                        var c = self[end - 1];
                        if (char.IsWhiteSpace(c))
                            --end;
                        var needContinuation = (end != self.Length) && !IsEolChar(c);
                        var continuation = "";
                        if (needContinuation)
                        {
                            --end;
                            continuation = "-";
                        }
                        var line = self.Substring(start, end - start) + continuation;
                        yield return line;
                        start = end;
                        if (char.IsWhiteSpace(c))
                            ++start;
                        width = GetNextWidth(enumerator, width, ref hw);
                    } while (start < self.Length);
                }
        }

        private static int GetNextWidth(IEnumerator<int> ewidths, int curWidth, ref bool? eValid)
        {
            if (eValid.HasValue && (!eValid.HasValue || !eValid.Value))
                return curWidth;
            curWidth = (eValid = ewidths.MoveNext()).Value ? ewidths.Current : curWidth;
            if (curWidth < ".-".Length)
                throw new ArgumentOutOfRangeException("widths",
                    string.Format("Element must be >= {0}, was {1}.", ".-".Length, curWidth));
            return curWidth;
        }

        private static bool IsEolChar(char c)
        {
            return !char.IsLetterOrDigit(c);
        }

        private static int GetLineEnd(int start, int length, string description)
        {
            var num1 = Math.Min(start + length, description.Length);
            var num2 = -1;
            for (var index = start; index < num1; ++index)
            {
                if (description[index] == 10)
                    return index + 1;
                if (IsEolChar(description[index]))
                    num2 = index + 1;
            }
            if ((num2 == -1) || (num1 == description.Length))
                return num1;
            return num2;
        }
    }
}