using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Touches: IList<Touch> {
    class TouchEnumerator: IEnumerator<Touch> {
        int index;
        public TouchEnumerator() {
            Reset();
        }

        public Touch Current {
            get { return Input.GetTouch(index); }
        }

        object IEnumerator.Current {
            get { return Input.GetTouch(index); }
        }

        public bool MoveNext() {
            return ++index < Input.touchCount;
        }

        public void Reset() {
            index = -1;
        }

        void IDisposable.Dispose() { }
    }

    static Touches instance;
    public static Touches Instance {
        get {
            if(instance == null) instance = new Touches();
            return instance;
        }
    }

    public int Count {
        get { return Input.touchCount; }
    }

    public Touch this[int index] {
        get { return Input.GetTouch(index); }
    }

    private Touches() { }

    public int IndexOf(Touch item) {
        for(int i = 0, l = Input.touchCount; i < l; i++)
            if(Input.GetTouch(i).Equals(item)) return i;
        return -1;
    }

    public bool Contains(Touch item) {
        for(int i = 0, l = Input.touchCount; i < l; i++)
            if(Input.GetTouch(i).Equals(item)) return true;
        return false;
    }

    public void CopyTo(Touch[] array, int arrayIndex) {
        for(int i = 0, l = Math.Min(array.Length - arrayIndex, Input.touchCount); i < l; i++)
            array[i + arrayIndex] = Input.GetTouch(i);
    }

    public IEnumerator<Touch> GetEnumerator() {
        return new TouchEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return new TouchEnumerator();
    }

    Touch IList<Touch>.this[int index] {
        get { return Input.GetTouch(index); }
        set { throw new NotSupportedException(); }
    }

    bool ICollection<Touch>.IsReadOnly {
        get { return true; }
    }

    void ICollection<Touch>.Add(Touch item) {
        throw new NotSupportedException();
    }

    void IList<Touch>.Insert(int index, Touch item) {
        throw new NotSupportedException();
    }

    bool ICollection<Touch>.Remove(Touch item) {
        throw new NotSupportedException();
    }

    void IList<Touch>.RemoveAt(int index) {
        throw new NotSupportedException();
    }

    void ICollection<Touch>.Clear() {
        throw new NotSupportedException();
    }
}
