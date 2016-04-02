using System;
using System.Collections;
using System.Collections.Generic;

namespace JLChnToZ.LuckyPlayer.WeightedRandomizer {
    sealed class ItemWeightCollection<T>: ICollection<double> {
        readonly WeightedCollection<T> parent;

        internal ItemWeightCollection(WeightedCollection<T> parent) {
            this.parent = parent;
        }

        public int Count {
            get { return parent.baseDict.Count; }
        }

        public bool IsReadOnly {
            get { return true; }
        }

        public void Add(double item) {
            throw new NotSupportedException();
        }

        public bool Contains(double item) {
            foreach(var kv in parent.baseDict)
                if(kv.Value.GetWeight(kv.Key) == item) return true;
            return false;
        }

        public bool Remove(double item) {
            throw new NotSupportedException();
        }

        public void Clear() {
            throw new NotSupportedException();
        }

        public void CopyTo(double[] array, int arrayIndex) {
            foreach(var kv in parent.baseDict)
                array[arrayIndex++] = kv.Value.GetWeight(kv.Key);
        }

        public IEnumerator<double> GetEnumerator() {
            foreach(var kv in parent.baseDict)
                yield return kv.Value.GetWeight(kv.Key);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
