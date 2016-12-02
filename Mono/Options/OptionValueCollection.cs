﻿// Decompiled with JetBrains decompiler
// Type: Mono.Options.OptionValueCollection
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;
using System.Collections;
using System.Collections.Generic;

namespace Mono.Options
{
    public class OptionValueCollection : IList, ICollection, IList<string>, ICollection<string>, IEnumerable<string>,
        IEnumerable
    {
        private readonly OptionContext c;
        private readonly List<string> values = new List<string>();

        internal OptionValueCollection(OptionContext c)
        {
            this.c = c;
        }

        bool ICollection.IsSynchronized
        {
            get { return ((ICollection) values).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return ((ICollection) values).SyncRoot; }
        }

        public int Count
        {
            get { return values.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { ((IList) values)[index] = value; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection) values).CopyTo(array, index);
        }

        public void Clear()
        {
            values.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        int IList.Add(object value)
        {
            return ((IList) values).Add(value);
        }

        bool IList.Contains(object value)
        {
            return ((IList) values).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList) values).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ((IList) values).Insert(index, value);
        }

        void IList.Remove(object value)
        {
            ((IList) values).Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            values.RemoveAt(index);
        }

        public string this[int index]
        {
            get
            {
                AssertValid(index);
                if (index < values.Count)
                    return values[index];
                return null;
            }
            set { values[index] = value; }
        }

        public void Add(string item)
        {
            values.Add(item);
        }

        public bool Contains(string item)
        {
            return values.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            values.CopyTo(array, arrayIndex);
        }

        public bool Remove(string item)
        {
            return values.Remove(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return values.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            values.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            values.RemoveAt(index);
        }

        private void AssertValid(int index)
        {
            if (c.Option == null)
                throw new InvalidOperationException("OptionContext.Option is null.");
            if (index >= c.Option.MaxValueCount)
                throw new ArgumentOutOfRangeException("index");
            if ((c.Option.OptionValueType == OptionValueType.Required) && (index >= values.Count))
                throw new OptionException(
                    string.Format(c.OptionSet.MessageLocalizer("Missing required value for option '{0}'."), c.OptionName),
                    c.OptionName);
        }

        public List<string> ToList()
        {
            return new List<string>(values);
        }

        public string[] ToArray()
        {
            return values.ToArray();
        }

        public override string ToString()
        {
            return string.Join(", ", values.ToArray());
        }
    }
}