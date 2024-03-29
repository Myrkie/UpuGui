﻿/*
 Options.cs

 Authors:
  Jonathan Pryor <jpryor@novell.com>, <Jonathan.Pryor@microsoft.com>
  Federico Di Gregorio <fog@initd.org>
  Rolf Bjarne Kvinge <rolf@xamarin.com>

 Copyright (C) 2008 Novell (http://www.novell.com)
 Copyright (C) 2009 Federico Di Gregorio.
 Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)
 Copyright (C) 2017 Microsoft Corporation (http://www.microsoft.com)

 Permission is hereby granted, free of charge, to any person obtaining
 a copy of this software and associated documentation files (the
 "Software"), to deal in the Software without restriction, including
 without limitation the rights to use, copy, modify, merge, publish,
 distribute, sublicense, and/or sell copies of the Software, and to
 permit persons to whom the Software is furnished to do so, subject to
 the following conditions:
 
 The above copyright notice and this permission notice shall be
 included in all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;

namespace UpuGui.Mono.Options
{
    internal static class StringCoda
    {
        public static IEnumerable<string> WrappedLines(string self, params int[] widths)
        {
            IEnumerable<int> w = widths;
            return WrappedLines(self, w);
        }

        private static IEnumerable<string> WrappedLines(string self, IEnumerable<int> widths)
        {
            if (widths == null) throw new ArgumentNullException("widths");
            return CreateWrappedLinesIterator(self, widths);
        }

        private static IEnumerable<string> CreateWrappedLinesIterator(string self, IEnumerable<int> widths)
        {
            if (string.IsNullOrEmpty(self))
            {
                yield return string.Empty;
                yield break;
            }

            using var ewidths = widths.GetEnumerator();
            bool? hw = null;
            var width = GetNextWidth(ewidths, int.MaxValue, ref hw);
            int start = 0, end;
            do
            {
                end = GetLineEnd(start, width, self);
                var c = self[end - 1];
                if (char.IsWhiteSpace(c)) --end;
                var needContinuation = end != self.Length && !IsEolChar(c);
                var continuation = "";
                if (needContinuation)
                {
                    --end;
                    continuation = "-";
                }

                var line = self.Substring(start, end - start) + continuation;
                yield return line;
                start = end;
                if (char.IsWhiteSpace(c)) ++start;
                width = GetNextWidth(ewidths, width, ref hw);
            } while (end < self.Length);
        }

        private static int GetNextWidth(IEnumerator<int> ewidths, int curWidth, ref bool? eValid)
        {
            if (eValid.HasValue && (!eValid.HasValue || !eValid.Value)) return curWidth;
            curWidth = (eValid = ewidths.MoveNext()).Value ? ewidths.Current : curWidth;
            // '.' is any character, - is for a continuation
            const string minWidth = ".-";
            if (curWidth < minWidth.Length)
                throw new ArgumentOutOfRangeException("widths",
                    $@"Element must be >= {minWidth.Length}, was {curWidth}.");
            return curWidth;

            // no more elements, use the last element.
        }

        private static bool IsEolChar(char c)
        {
            return !char.IsLetterOrDigit(c);
        }

        private static int GetLineEnd(int start, int length, string description)
        {
            var end = System.Math.Min(start + length, description.Length);
            var sep = -1;
            for (var i = start; i < end; ++i)
            {
                if (description[i] == '\n') return i + 1;
                if (IsEolChar(description[i])) sep = i + 1;
            }

            if (sep == -1 || end == description.Length) return end;
            return sep;
        }
    }

    public class OptionValueCollection : IList, IList<string>
    {
        private readonly List<string> _values = new();
        private readonly OptionContext _c;

        internal OptionValueCollection(OptionContext c)
        {
            this._c = c;
        }

        #region ICollection

        void ICollection.CopyTo(Array array, int index)
        {
            (_values as ICollection).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized => (_values as ICollection).IsSynchronized;

        object ICollection.SyncRoot => (_values as ICollection).SyncRoot;

        #endregion

        #region ICollection<T>

        public void Add(string item)
        {
            _values.Add(item);
        }

        public void Clear()
        {
            _values.Clear();
        }

        public bool Contains(string item)
        {
            return _values.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _values.CopyTo(array, arrayIndex);
        }

        public bool Remove(string item)
        {
            return _values.Remove(item);
        }

        public int Count => _values.Count;

        public bool IsReadOnly => false;

        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        #endregion

        #region IEnumerable<T>

        public IEnumerator<string> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        #endregion

        #region IList

        int IList.Add(object? value)
        {
            return (_values as IList).Add(value);
        }

        bool IList.Contains(object? value)
        {
            return (_values as IList).Contains(value);
        }

        int IList.IndexOf(object? value)
        {
            return (_values as IList).IndexOf(value);
        }

        void IList.Insert(int index, object? value)
        {
            (_values as IList).Insert(index, value);
        }

        void IList.Remove(object? value)
        {
            (_values as IList).Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            (_values as IList).RemoveAt(index);
        }

        bool IList.IsFixedSize => false;

        object IList.this[int index]
        {
            get => this[index];
            set => (_values as IList)[index] = value;
        }

        #endregion

        #region IList<T>

        public int IndexOf(string item)
        {
            return _values.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _values.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _values.RemoveAt(index);
        }

        private void AssertValid(int index)
        {
            if (_c.Option == null) throw new InvalidOperationException("OptionContext.Option is null.");
            if (index >= _c.Option.MaxValueCount) throw new ArgumentOutOfRangeException(nameof(index));
            if (_c.Option.OptionValueType == OptionValueType.Required && index >= _values.Count)
                throw new OptionException(
                    string.Format(_c.OptionSet.MessageLocalizer("Missing required value for option '{0}'."),
                        _c.OptionName), _c.OptionName);
        }

        public string this[int index]
        {
            get
            {
                AssertValid(index);
                return index >= _values.Count ? null : _values[index];
            }
            set => _values[index] = value;
        }

        #endregion

        public List<string> ToList()
        {
            return new List<string>(_values);
        }

        public string[] ToArray()
        {
            return _values.ToArray();
        }

        public override string ToString()
        {
            return string.Join(", ", _values.ToArray());
        }
    }

    public class OptionContext
    {
        public OptionContext(OptionSet set)
        {
            this.OptionSet = set;
            this.OptionValues = new OptionValueCollection(this);
        }

        public Option Option { get; set; }

        public string OptionName { get; set; }

        public int OptionIndex { get; set; }

        public OptionSet OptionSet { get; }

        public OptionValueCollection OptionValues { get; }
    }

    public enum OptionValueType
    {
        None,
        Optional,
        Required,
    }

    public abstract class Option
    {
        protected Option(string prototype, string description) : this(prototype, description, 1)
        {
        }

        protected Option(string prototype, string description, int maxValueCount)
        {
            if (prototype == null) throw new ArgumentNullException(nameof(prototype));
            if (prototype.Length == 0) throw new ArgumentException(@"Cannot be the empty string.", nameof(prototype));
            if (maxValueCount < 0) throw new ArgumentOutOfRangeException(nameof(maxValueCount));
            this.Prototype = prototype;
            this.Names = prototype.Split('|');
            this.Description = description;
            this.MaxValueCount = maxValueCount;
            this.OptionValueType = ParsePrototype();
            if (this.MaxValueCount == 0 && OptionValueType != OptionValueType.None)
                throw new ArgumentException(
                    "Cannot provide maxValueCount of 0 for OptionValueType.Required or " + "OptionValueType.Optional.",
                    nameof(maxValueCount));
            if (this.OptionValueType == OptionValueType.None && maxValueCount > 1)
                throw new ArgumentException(
                    string.Format("Cannot provide maxValueCount of {0} for OptionValueType.None.", maxValueCount),
                    nameof(maxValueCount));
            if (Array.IndexOf(Names, "<>") >= 0 && ((Names.Length == 1 && this.OptionValueType != OptionValueType.None) ||
                                                    (Names.Length > 1 && this.MaxValueCount > 1)))
                throw new ArgumentException("The default option handler '<>' cannot require values.", nameof(prototype));
        }

        private string Prototype { get; }

        public string Description { get; }

        public OptionValueType OptionValueType { get; }

        public int MaxValueCount { get; }

        public string[] GetNames()
        {
            return (string[])Names.Clone();
        }

        public string[] GetValueSeparators()
        {
            if (ValueSeparators == null) return new string [0];
            return (string[])ValueSeparators.Clone();
        }

        protected static T Parse<T>(string value, OptionContext c)
        {
            var tt = typeof(T);
            var nullable = tt.IsValueType && tt.IsGenericType && !tt.IsGenericTypeDefinition &&
                           tt.GetGenericTypeDefinition() == typeof(Nullable<>);
            var targetType = nullable ? tt.GetGenericArguments()[0] : typeof(T);
            var conv = TypeDescriptor.GetConverter(targetType);
            var t = default(T);
            try
            {
                if (value != null) t = (T)conv.ConvertFromString(value);
            }
            catch (Exception e)
            {
                throw new OptionException(
                    string.Format(
                        c.OptionSet.MessageLocalizer("Could not convert string `{0}' to type {1} for option `{2}'."),
                        value, targetType.Name, c.OptionName), c.OptionName, e);
            }

            return t;
        }

        internal string[] Names { get; }

        internal string[] ValueSeparators { get; private set; }

        private static readonly char[] NameTerminator = new char[] { '=', ':' };

        private OptionValueType ParsePrototype()
        {
            var type = '\0';
            List<string> seps = new List<string>();
            for (var i = 0; i < Names.Length; ++i)
            {
                var name = Names[i];
                if (name.Length == 0) throw new ArgumentException("Empty option names are not supported.", "prototype");
                var end = name.IndexOfAny(NameTerminator);
                if (end == -1) continue;
                Names[i] = name.Substring(0, end);
                if (type == '\0' || type == name[end]) type = name[end];
                else
                    throw new ArgumentException(
                        string.Format("Conflicting option types: '{0}' vs. '{1}'.", type, name[end]), "prototype");
                AddSeparators(name, end, seps);
            }

            if (type == '\0') return OptionValueType.None;
            if (MaxValueCount <= 1 && seps.Count != 0)
                throw new ArgumentException(
                    string.Format("Cannot provide key/value separators for Options taking {0} value(s).", MaxValueCount),
                    "prototype");
            if (MaxValueCount > 1)
            {
                if (seps.Count == 0) this.ValueSeparators = new string[] { ":", "=" };
                else if (seps.Count == 1 && seps[0].Length == 0) this.ValueSeparators = null;
                else this.ValueSeparators = seps.ToArray();
            }

            return type == '=' ? OptionValueType.Required : OptionValueType.Optional;
        }

        private static void AddSeparators(string name, int end, ICollection<string> seps)
        {
            var start = -1;
            for (var i = end + 1; i < name.Length; ++i)
            {
                switch (name[i])
                {
                    case '{':
                        if (start != -1)
                            throw new ArgumentException(
                                string.Format("Ill-formed name/value separator found in \"{0}\".", name), "prototype");
                        start = i + 1;
                        break;
                    case '}':
                        if (start == -1)
                            throw new ArgumentException(
                                string.Format("Ill-formed name/value separator found in \"{0}\".", name), "prototype");
                        seps.Add(name.Substring(start, i - start));
                        start = -1;
                        break;
                    default:
                        if (start == -1) seps.Add(name[i].ToString());
                        break;
                }
            }

            if (start != -1)
                throw new ArgumentException(string.Format("Ill-formed name/value separator found in \"{0}\".", name),
                    "prototype");
        }

        public void Invoke(OptionContext c)
        {
            OnParseComplete(c);
            c.OptionName = null;
            c.Option = null;
            c.OptionValues.Clear();
        }

        protected abstract void OnParseComplete(OptionContext c);

        public override string ToString()
        {
            return Prototype;
        }
    }

    [Serializable]
    public class OptionException : Exception
    {
        public OptionException()
        {
        }

        public OptionException(string message, string optionName) : base(message)
        {
            this.OptionName = optionName;
        }

        public OptionException(string message, string optionName, Exception innerException) : base(message,
            innerException)
        {
            this.OptionName = optionName;
        }

        protected OptionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.OptionName = info.GetString("OptionName");
        }

        public string OptionName { get; }

        [SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("OptionName", OptionName);
        }
    }

    public delegate void OptionAction<TKey, TValue>(TKey key, TValue value);

    public class OptionSet : KeyedCollection<string, Option>
    {
        public OptionSet() : this(delegate(string f) { return f; })
        {
        }

        public OptionSet(Converter<string, string> localizer)
        {
            this.MessageLocalizer = localizer;
        }

        public Converter<string, string> MessageLocalizer { get; }

        protected override string GetKeyForItem(Option item)
        {
            if (item == null) throw new ArgumentNullException("option");
            if (item.Names != null && item.Names.Length > 0) return item.Names[0];
            // This should never happen, as it's invalid for Option to be
            // constructed w/o any names.
            throw new InvalidOperationException("Option has no names!");
        }

        [Obsolete("Use KeyedCollection.this[string]")]
        protected Option GetOptionForName(string option)
        {
            if (option == null) throw new ArgumentNullException("option");
            try
            {
                return base[option];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        protected override void InsertItem(int index, Option item)
        {
            base.InsertItem(index, item);
            AddImpl(item);
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            var p = Items[index];
            // KeyedCollection.RemoveItem() handles the 0th item
            for (var i = 1; i < p.Names.Length; ++i)
            {
                Dictionary.Remove(p.Names[i]);
            }
        }

        protected override void SetItem(int index, Option item)
        {
            base.SetItem(index, item);
            RemoveItem(index);
            AddImpl(item);
        }

        private void AddImpl(Option option)
        {
            if (option == null) throw new ArgumentNullException("option");
            var added = new List<string>(option.Names.Length);
            try
            {
                // KeyedCollection.InsertItem/SetItem handle the 0th name.
                for (var i = 1; i < option.Names.Length; ++i)
                {
                    Dictionary.Add(option.Names[i], option);
                    added.Add(option.Names[i]);
                }
            }
            catch (Exception)
            {
                foreach (var name in added) Dictionary.Remove(name);
                throw;
            }
        }

        public new OptionSet Add(Option option)
        {
            base.Add(option);
            return this;
        }

        private sealed class ActionOption : Option
        {
            private readonly Action<OptionValueCollection> _action;

            public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action) :
                base(prototype, description, count)
            {
                if (action == null) throw new ArgumentNullException("action");
                this._action = action;
            }

            protected override void OnParseComplete(OptionContext c)
            {
                _action(c.OptionValues);
            }
        }

        public OptionSet Add(string prototype, Action<string?> action)
        {
            return Add(prototype, null, action);
        }

        public OptionSet Add(string prototype, string description, Action<string?> action)
        {
            if (action == null) throw new ArgumentNullException("action");
            Option p = new ActionOption(prototype, description, 1, delegate(OptionValueCollection v) { action(v[0]); });
            base.Add(p);
            return this;
        }

        public OptionSet Add(string prototype, OptionAction<string, string> action)
        {
            return Add(prototype, null, action);
        }

        public OptionSet Add(string prototype, string description, OptionAction<string, string> action)
        {
            if (action == null) throw new ArgumentNullException("action");
            Option p = new ActionOption(prototype, description, 2,
                delegate(OptionValueCollection v) { action(v[0], v[1]); });
            base.Add(p);
            return this;
        }

        private sealed class ActionOption<T> : Option
        {
            private readonly Action<T> _action;

            public ActionOption(string prototype, string description, Action<T> action) : base(prototype, description,
                1)
            {
                if (action == null) throw new ArgumentNullException("action");
                this._action = action;
            }

            protected override void OnParseComplete(OptionContext c)
            {
                _action(Parse<T>(c.OptionValues[0], c));
            }
        }

        private sealed class ActionOption<TKey, TValue> : Option
        {
            private readonly OptionAction<TKey, TValue> _action;

            public ActionOption(string prototype, string description, OptionAction<TKey, TValue> action) : base(
                prototype, description, 2)
            {
                if (action == null) throw new ArgumentNullException("action");
                this._action = action;
            }

            protected override void OnParseComplete(OptionContext c)
            {
                _action(Parse<TKey>(c.OptionValues[0], c), Parse<TValue>(c.OptionValues[1], c));
            }
        }

        public OptionSet Add<T>(string prototype, Action<T> action)
        {
            return Add(prototype, null, action);
        }

        public OptionSet Add<T>(string prototype, string description, Action<T> action)
        {
            return Add(new ActionOption<T>(prototype, description, action));
        }

        public OptionSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action)
        {
            return Add(prototype, null, action);
        }

        public OptionSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action)
        {
            return Add(new ActionOption<TKey, TValue>(prototype, description, action));
        }

        protected virtual OptionContext CreateOptionContext()
        {
            return new OptionContext(this);
        }

#if LINQ
		public List<string> Parse (IEnumerable<string> arguments)
		{
			bool process = true;
			OptionContext c = CreateOptionContext ();
			c.OptionIndex = -1;
			var def = GetOptionForName ("<>");
			var unprocessed =
				from argument in arguments
				where ++c.OptionIndex >= 0 && (process || def != null)
					? process
						? argument == "--" 
							? (process = false)
							: !Parse (argument, c)
								? def != null 
									? Unprocessed (null, def, c, argument) 
									: true
								: false
						: def != null 
							? Unprocessed (null, def, c, argument)
							: true
					: true
				select argument;
			List<string> r = unprocessed.ToList ();
			if (c.Option != null)
				c.Option.Invoke (c);
			return r;
		}
#else
        public List<string> Parse(IEnumerable<string> arguments)
        {
            var c = CreateOptionContext();
            c.OptionIndex = -1;
            var process = true;
            List<string> unprocessed = new List<string>();
            var def = Contains("<>") ? this["<>"] : null;
            foreach (var argument in arguments)
            {
                ++c.OptionIndex;
                if (argument == "--")
                {
                    process = false;
                    continue;
                }

                if (!process)
                {
                    Unprocessed(unprocessed, def, c, argument);
                    continue;
                }

                if (!Parse(argument, c)) Unprocessed(unprocessed, def, c, argument);
            }

            if (c.Option != null) c.Option.Invoke(c);
            return unprocessed;
        }
#endif

        private static bool Unprocessed(ICollection<string> extra, Option def, OptionContext c, string argument)
        {
            if (def == null)
            {
                extra.Add(argument);
                return false;
            }

            c.OptionValues.Add(argument);
            c.Option = def;
            c.Option.Invoke(c);
            return false;
        }

        private readonly Regex _valueOption = new Regex(@"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$");

        protected bool GetOptionParts(string argument, out string flag, out string name, out string sep,
            out string value)
        {
            if (argument == null) throw new ArgumentNullException("argument");
            flag = name = sep = value = null;
            var m = _valueOption.Match(argument);
            if (!m.Success)
            {
                return false;
            }

            flag = m.Groups["flag"].Value;
            name = m.Groups["name"].Value;
            if (m.Groups["sep"].Success && m.Groups["value"].Success)
            {
                sep = m.Groups["sep"].Value;
                value = m.Groups["value"].Value;
            }

            return true;
        }

        protected virtual bool Parse(string argument, OptionContext c)
        {
            if (c.Option != null)
            {
                ParseValue(argument, c);
                return true;
            }

            string f, n, s, v;
            if (!GetOptionParts(argument, out f, out n, out s, out v)) return false;
            Option p;
            if (Contains(n))
            {
                p = this[n];
                c.OptionName = f + n;
                c.Option = p;
                switch (p.OptionValueType)
                {
                    case OptionValueType.None:
                        c.OptionValues.Add(n);
                        c.Option.Invoke(c);
                        break;
                    case OptionValueType.Optional:
                    case OptionValueType.Required:
                        ParseValue(v, c);
                        break;
                }

                return true;
            }

            // no match; is it a bool option?
            if (ParseBool(argument, n, c)) return true;
            // is it a bundled option?
            if (ParseBundledValue(f, string.Concat(n + s + v), c)) return true;
            return false;
        }

        private void ParseValue(string option, OptionContext c)
        {
            if (option != null)
                foreach (var o in c.Option.ValueSeparators != null
                             ? option.Split(c.Option.ValueSeparators, StringSplitOptions.None)
                             : new string[] { option })
                {
                    c.OptionValues.Add(o);
                }

            if (c.OptionValues.Count == c.Option.MaxValueCount || c.Option.OptionValueType == OptionValueType.Optional)
                c.Option.Invoke(c);
            else if (c.OptionValues.Count > c.Option.MaxValueCount)
            {
                throw new OptionException(
                    MessageLocalizer(string.Format("Error: Found {0} option values when expecting {1}.", c.OptionValues.Count,
                        c.Option.MaxValueCount)), c.OptionName);
            }
        }

        private bool ParseBool(string option, string n, OptionContext c)
        {
            Option p;
            string rn;
            if (n.Length >= 1 && (n[n.Length - 1] == '+' || n[n.Length - 1] == '-') &&
                Contains((rn = n.Substring(0, n.Length - 1))))
            {
                p = this[rn];
                var v = n[n.Length - 1] == '+' ? option : null;
                c.OptionName = option;
                c.Option = p;
                c.OptionValues.Add(v);
                p.Invoke(c);
                return true;
            }

            return false;
        }

        private bool ParseBundledValue(string f, string n, OptionContext c)
        {
            if (f != "-") return false;
            for (var i = 0; i < n.Length; ++i)
            {
                Option p;
                var opt = f + n[i].ToString();
                var rn = n[i].ToString();
                if (!Contains(rn))
                {
                    if (i == 0) return false;
                    throw new OptionException(string.Format(MessageLocalizer("Cannot bundle unregistered option '{0}'."), opt),
                        opt);
                }

                p = this[rn];
                switch (p.OptionValueType)
                {
                    case OptionValueType.None:
                        Invoke(c, opt, n, p);
                        break;
                    case OptionValueType.Optional:
                    case OptionValueType.Required:
                    {
                        var v = n.Substring(i + 1);
                        c.Option = p;
                        c.OptionName = opt;
                        ParseValue(v.Length != 0 ? v : null, c);
                        return true;
                    }
                    default: throw new InvalidOperationException("Unknown OptionValueType: " + p.OptionValueType);
                }
            }

            return true;
        }

        private static void Invoke(OptionContext c, string name, string value, Option option)
        {
            c.OptionName = name;
            c.Option = option;
            c.OptionValues.Add(value);
            option.Invoke(c);
        }

        private const int OptionWidth = 29;

        public void WriteOptionDescriptions(TextWriter o)
        {
            foreach (var p in this)
            {
                var written = 0;
                if (!WriteOptionPrototype(o, p, ref written)) continue;
                if (written < OptionWidth) o.Write(new string(' ', OptionWidth - written));
                else
                {
                    o.WriteLine();
                    o.Write(new string(' ', OptionWidth));
                }

                var indent = false;
                var prefix = new string(' ', OptionWidth + 2);
                foreach (var line in GetLines(MessageLocalizer(GetDescription(p.Description))))
                {
                    if (indent) o.Write(prefix);
                    o.WriteLine(line);
                    indent = true;
                }
            }
        }

        private bool WriteOptionPrototype(TextWriter o, Option p, ref int written)
        {
            var names = p.Names;
            var i = GetNextOptionIndex(names, 0);
            if (i == names.Length) return false;
            if (names[i].Length == 1)
            {
                Write(o, ref written, "  -");
                Write(o, ref written, names[0]);
            }
            else
            {
                Write(o, ref written, "      --");
                Write(o, ref written, names[0]);
            }

            for (i = GetNextOptionIndex(names, i + 1); i < names.Length; i = GetNextOptionIndex(names, i + 1))
            {
                Write(o, ref written, ", ");
                Write(o, ref written, names[i].Length == 1 ? "-" : "--");
                Write(o, ref written, names[i]);
            }

            if (p.OptionValueType == OptionValueType.Optional || p.OptionValueType == OptionValueType.Required)
            {
                if (p.OptionValueType == OptionValueType.Optional)
                {
                    Write(o, ref written, MessageLocalizer("["));
                }

                Write(o, ref written, MessageLocalizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
                var sep = p.ValueSeparators != null && p.ValueSeparators.Length > 0 ? p.ValueSeparators[0] : " ";
                for (var c = 1; c < p.MaxValueCount; ++c)
                {
                    Write(o, ref written, MessageLocalizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
                }

                if (p.OptionValueType == OptionValueType.Optional)
                {
                    Write(o, ref written, MessageLocalizer("]"));
                }
            }

            return true;
        }

        private static int GetNextOptionIndex(string[] names, int i)
        {
            while (i < names.Length && names[i] == "<>")
            {
                ++i;
            }

            return i;
        }

        private static void Write(TextWriter o, ref int n, string s)
        {
            n += s.Length;
            o.Write(s);
        }

        private static string GetArgumentName(int index, int maxIndex, string description)
        {
            if (description == null) return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
            string[] nameStart;
            if (maxIndex == 1) nameStart = new string[] { "{0:", "{" };
            else nameStart = new string[] { "{" + index + ":" };
            for (var i = 0; i < nameStart.Length; ++i)
            {
                int start, j = 0;
                do
                {
                    start = description.IndexOf(nameStart[i], j);
                } while (start >= 0 && j != 0 ? description[j++ - 1] == '{' : false);

                if (start == -1) continue;
                var end = description.IndexOf("}", start);
                if (end == -1) continue;
                return description.Substring(start + nameStart[i].Length, end - start - nameStart[i].Length);
            }

            return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
        }

        private static string GetDescription(string description)
        {
            if (description == null) return string.Empty;
            var sb = new StringBuilder(description.Length);
            var start = -1;
            for (var i = 0; i < description.Length; ++i)
            {
                switch (description[i])
                {
                    case '{':
                        if (i == start)
                        {
                            sb.Append('{');
                            start = -1;
                        }
                        else if (start < 0) start = i + 1;

                        break;
                    case '}':
                        if (start < 0)
                        {
                            if ((i + 1) == description.Length || description[i + 1] != '}')
                                throw new InvalidOperationException("Invalid option description: " + description);
                            ++i;
                            sb.Append("}");
                        }
                        else
                        {
                            sb.Append(description.Substring(start, i - start));
                            start = -1;
                        }

                        break;
                    case ':':
                        if (start < 0) goto default;
                        start = i + 1;
                        break;
                    default:
                        if (start < 0) sb.Append(description[i]);
                        break;
                }
            }

            return sb.ToString();
        }

        private static IEnumerable<string> GetLines(string description)
        {
            return StringCoda.WrappedLines(description, 80 - OptionWidth, 80 - OptionWidth - 2);
        }
    }
}