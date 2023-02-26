using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mono.Options
{
    public abstract class Option
    {
        private static readonly char[] NameTerminator = new char[2]
        {
            '=',
            ':'
        };

        protected Option(string prototype, string description)
            : this(prototype, description, 1, false)
        {
        }

        protected Option(string prototype, string description, int maxValueCount)
            : this(prototype, description, maxValueCount, false)
        {
        }

        protected Option(string prototype, string description, int maxValueCount, bool hidden)
        {
            if (prototype == null)
                throw new ArgumentNullException("prototype");
            if (prototype.Length == 0)
                throw new ArgumentException("Cannot be the empty string.", "prototype");
            if (maxValueCount < 0)
                throw new ArgumentOutOfRangeException("maxValueCount");
            Prototype = prototype;
            Description = description;
            MaxValueCount = maxValueCount;
            string[] strArray;
            if (!(this is OptionSet.Category))
                strArray = prototype.Split('|');
            else
                strArray = new string[1]
                {
                    prototype + GetHashCode()
                };
            Names = strArray;
            if (this is OptionSet.Category)
                return;
            OptionValueType = ParsePrototype();
            Hidden = hidden;
            if ((MaxValueCount == 0) && (OptionValueType != OptionValueType.None))
                throw new ArgumentException(
                    "Cannot provide maxValueCount of 0 for OptionValueType.Required or OptionValueType.Optional.",
                    "maxValueCount");
            if ((OptionValueType == OptionValueType.None) && (maxValueCount > 1))
                throw new ArgumentException(
                    string.Format("Cannot provide maxValueCount of {0} for OptionValueType.None.", maxValueCount),
                    "maxValueCount");
            if ((Array.IndexOf(Names, "<>") >= 0) &&
                (((Names.Length == 1) && (OptionValueType != OptionValueType.None)) ||
                 ((Names.Length > 1) && (MaxValueCount > 1))))
                throw new ArgumentException("The default option handler '<>' cannot require values.", "prototype");
        }

        public string Prototype { get; }

        public string Description { get; }

        public OptionValueType OptionValueType { get; }

        public int MaxValueCount { get; }

        public bool Hidden { get; }

        internal string[] Names { get; }

        internal string[] ValueSeparators { get; private set; }

        public string[] GetNames()
        {
            return (string[]) Names.Clone();
        }

        public string[] GetValueSeparators()
        {
            if (ValueSeparators == null)
                return new string[0];
            return (string[]) ValueSeparators.Clone();
        }

        protected static T Parse<T>(string value, OptionContext c)
        {
            var type1 = typeof(T);
            var type2 = type1.IsValueType && type1.IsGenericType && !type1.IsGenericTypeDefinition &&
                        (type1.GetGenericTypeDefinition() == typeof(Nullable<>))
                ? type1.GetGenericArguments()[0]
                : typeof(T);
            var converter = TypeDescriptor.GetConverter(type2);
            var obj = default(T);
            try
            {
                if (value != null)
                    obj = (T) converter.ConvertFromString(value);
            }
            catch (Exception ex)
            {
                throw new OptionException(
                    string.Format(
                        c.OptionSet.MessageLocalizer("Could not convert string `{0}' to type {1} for option `{2}'."),
                        value, type2.Name, c.OptionName), c.OptionName, ex);
            }
            return obj;
        }

        private OptionValueType ParsePrototype()
        {
            var ch = char.MinValue;
            var list = new List<string>();
            for (var index1 = 0; index1 < Names.Length; ++index1)
            {
                var name = Names[index1];
                if (name.Length == 0)
                    throw new ArgumentException("Empty option names are not supported.", "prototype");
                var index2 = name.IndexOfAny(NameTerminator);
                if (index2 != -1)
                {
                    Names[index1] = name.Substring(0, index2);
                    if ((ch != 0) && (ch != name[index2]))
                        throw new ArgumentException(
                            string.Format("Conflicting option types: '{0}' vs. '{1}'.", ch, name[index2]), "prototype");
                    ch = name[index2];
                    AddSeparators(name, index2, list);
                }
            }
            if (ch == 0)
                return OptionValueType.None;
            if ((MaxValueCount <= 1) && (list.Count != 0))
                throw new ArgumentException(
                    string.Format("Cannot provide key/value separators for Options taking {0} value(s).", MaxValueCount),
                    "prototype");
            if (MaxValueCount > 1)
                if (list.Count == 0)
                    ValueSeparators = new string[2]
                    {
                        ":",
                        "="
                    };
                else
                    ValueSeparators = (list.Count != 1) || (list[0].Length != 0) ? list.ToArray() : null;
            return (int) ch != 61 ? OptionValueType.Optional : OptionValueType.Required;
        }

        private static void AddSeparators(string name, int end, ICollection<string> seps)
        {
            var startIndex = -1;
            for (var index = end + 1; index < name.Length; ++index)
                switch (name[index])
                {
                    case '{':
                        if (startIndex != -1)
                            throw new ArgumentException(
                                string.Format("Ill-formed name/value separator found in \"{0}\".", name), "prototype");
                        startIndex = index + 1;
                        break;
                    case '}':
                        if (startIndex == -1)
                            throw new ArgumentException(
                                string.Format("Ill-formed name/value separator found in \"{0}\".", name), "prototype");
                        seps.Add(name.Substring(startIndex, index - startIndex));
                        startIndex = -1;
                        break;
                    default:
                        if (startIndex == -1)
                            seps.Add(name[index].ToString());
                        break;
                }
            if (startIndex != -1)
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
}