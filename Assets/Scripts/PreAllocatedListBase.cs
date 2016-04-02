using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public abstract class PreAllocatedListBase<T> : IList<T>, ICloneable {
    protected class Enumerator: IEnumerator<T> {
        readonly PreAllocatedListBase<T> parent;
        int index;

        protected internal Enumerator(PreAllocatedListBase<T> parent) {
            this.parent = parent;
            Reset();
        }

        public T Current {
            get { return parent.results[index]; }
        }

        object IEnumerator.Current {
            get { return parent.results[index]; }
        }

        public bool MoveNext() {
            return ++index < parent.count;
        }

        public void Reset() {
            index = -1;
        }

        void IDisposable.Dispose() { }
    }
    protected const int defaultCapacity = 8;

    T[] results;
    int count;
    int lostDataCount;

    protected PreAllocatedListBase() : this(defaultCapacity) { }

    protected PreAllocatedListBase(int capacity) {
        if(capacity <= 0)
            throw new ArgumentOutOfRangeException("capacity");
        results = new T[capacity];
        Clear();
    }

    public T this[int index] {
        get { return results[index]; }
    }

    public int Count {
        get { return count; }
        protected set {
            if(value < 0)
                throw new ArgumentOutOfRangeException("count");
            lostDataCount = Math.Max(0, value - count);
            count = value;
            if(count > results.Length)
                Array.Resize(ref results, count);
        }
    }

    protected T[] Results {
        get { return results; }
    }

    public int LostDataCount {
        get { return lostDataCount; }
    }

    public bool Contains(T item) {
        return Array.IndexOf(results, item) >= 0;
    }

    public int IndexOf(T item) {
        return Array.IndexOf(results, item);
    }

    public void CopyTo(T[] array, int arrayIndex) {
        if(array == null) throw new ArgumentNullException("array");
        Array.Copy(results, 0, array, arrayIndex, Math.Min(array.Length - arrayIndex, count));
    }

    public virtual object Clone() {
        var clone = MemberwiseClone() as PreAllocatedListBase<T>;
        clone.results = results.Clone() as T[];
        return clone;
    }

    public virtual void Clear() {
        count = 0;
        lostDataCount = 0;
    }

    protected void ResetLostDataCount() {
        lostDataCount = 0;
    }

    public IEnumerator<T> GetEnumerator() {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return new Enumerator(this);
    }

    public override string ToString() {
        var sb = new StringBuilder();
        bool isFirst = true;
        foreach(T item in this) {
            if(!isFirst) sb.Append(", ");
            sb.Append(item);
            isFirst = false;
        }
        return string.Format("[{0}]", sb);
    }

    #region Unused Interface Stubs
    T IList<T>.this[int index] {
        get { return results[index]; }
        set { throw new NotSupportedException(); }
    }

    bool ICollection<T>.IsReadOnly {
        get { return true; }
    }

    void ICollection<T>.Add(T item) {
        throw new NotSupportedException();
    }

    void IList<T>.Insert(int index, T item) {
        throw new NotSupportedException();
    }

    bool ICollection<T>.Remove(T item) {
        throw new NotSupportedException();
    }

    void IList<T>.RemoveAt(int index) {
        throw new NotSupportedException();
    }
    #endregion
}
