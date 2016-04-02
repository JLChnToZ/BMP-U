using System;
using System.Collections;
using System.Collections.Generic;

namespace JLChnToZ.LuckyPlayer.WeightedRandomizer {
    /// <summary>
    /// A pool of items with dynamic weights defined. 
    /// </summary>
    /// <typeparam name="T">Generic type</typeparam>
    /// <remarks>This class behave like a set.</remarks>
    public class WeightedCollection<T>: ICollection<T>, IDictionary<T, IItemWeight<T>>, IDictionary<T, double>, ICloneable {
        static Random defaultRandomizer;
        object locker = new object();
        internal readonly Dictionary<T, IItemWeight<T>> baseDict;

        #region Constructors
        /// <summary>
        /// Constructor.
        /// </summary>
        public WeightedCollection() {
            baseDict = new Dictionary<T, IItemWeight<T>>();
        }

        /// <summary>
        /// Constructor with capacity defined.
        /// </summary>
        /// <param name="capacity">Initial capacity of the pool</param>
        public WeightedCollection(int capacity) {
            baseDict = new Dictionary<T, IItemWeight<T>>(capacity);
        }

        /// <summary>
        /// Constructor with <paramref name="comparer"/> defined.
        /// </summary>
        /// <param name="comparer">Custom equality comparer for checking between two <typeparamref name="T"/>s are equal.</param>
        public WeightedCollection(IEqualityComparer<T> comparer) {
            baseDict = new Dictionary<T, IItemWeight<T>>(comparer);
        }

        /// <summary>
        /// Constructor with <paramref name="capacity"/> and <paramref name="comparer"/> defined.
        /// </summary>
        /// <param name="capacity">Initial capacity of the pool</param>
        /// <param name="comparer">Custom equality comparer for checking between two <typeparamref name="T"/>s are equal.</param>
        public WeightedCollection(int capacity, IEqualityComparer<T> comparer) {
            baseDict = new Dictionary<T, IItemWeight<T>>(capacity, comparer);
        }

        /// <summary>
        /// Constructor which will initialize the pool with an array of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="source">Array of objects which will initially put into the pool.</param>
        public WeightedCollection(IEnumerable<T> source) {
            AddRange(source);
        }

        /// <summary>
        /// Constructor which will initialize the pool with an array of <typeparamref name="T"/> with enough <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">Initial capacity of the pool</param>
        /// <param name="source">Array of objects which will initially put into the pool.</param>
        public WeightedCollection(int capacity, IEnumerable<T> source) : this(capacity) {
            AddRange(source);
        }

        /// <summary>
        /// Constructor which will initialize the pool with an array of <typeparamref name="T"/> with enough <paramref name="capacity"/> and compare by <paramref name="comparer"/>.
        /// </summary>
        /// <param name="capacity">Initial capacity of the pool</param>
        /// <param name="comparer">Custom equality comparer for checking between two <typeparamref name="T"/>s are equal.</param>
        /// <param name="source">Array of objects which will initially put into the pool.</param>
        public WeightedCollection(int capacity, IEqualityComparer<T> comparer, IEnumerable<T> source) : this(capacity, comparer) {
            AddRange(source);
        }

        protected WeightedCollection(IDictionary<T, IItemWeight<T>> clone) {
            baseDict = new Dictionary<T, IItemWeight<T>>(clone);
        }
        #endregion

        #region Interface Methods
        IItemWeight<T> IDictionary<T, IItemWeight<T>>.this[T key] {
            get { return GetWeight(key); }
            set { SetWeight(key, value); }
        }

        double IDictionary<T, double>.this[T key] {
            get { return GetCurrentWeight(key); }
            set { SetWeight(key, value); }
        }

        public int Count {
            get { return baseDict.Count; }
        }

        bool ICollection<T>.IsReadOnly {
            get { return false; }
        }

        bool ICollection<KeyValuePair<T, double>>.IsReadOnly {
            get { return false; }
        }

        bool ICollection<KeyValuePair<T, IItemWeight<T>>>.IsReadOnly {
            get { return false; }
        }

        ICollection<T> IDictionary<T, double>.Keys {
            get { return baseDict.Keys; }
        }

        ICollection<T> IDictionary<T, IItemWeight<T>>.Keys {
            get { return baseDict.Keys; }
        }

        ICollection<double> IDictionary<T, double>.Values {
            get { return new ItemWeightCollection<T>(this); }
        }

        ICollection<IItemWeight<T>> IDictionary<T, IItemWeight<T>>.Values {
            get { return baseDict.Values; }
        }

        /// <summary>
        /// Adds an item into the pool with default weight.
        /// </summary>
        /// <param name="item">The item will add into the pool</param>
        public void Add(T item) {
            lock (locker) baseDict.Add(item, new FixedItemWeight<T>(1));
        }

        /// <summary>
        /// Adds an item into the pool with static weight.
        /// </summary>
        /// <param name="item">The item will add into the pool</param>
        /// <param name="weight">Static weight</param>
        public void Add(T item, double weight) {
            lock (locker) baseDict.Add(item, new FixedItemWeight<T>(weight));
        }

        /// <summary>
        /// Adds an item into the pool with dynamic weight definition.
        /// </summary>
        /// <param name="item">The item will add into the pool</param>
        /// <param name="weight">Weight object which will controls the weight of this item</param>
        public void Add(T item, IItemWeight<T> weight) {
            lock (locker) baseDict.Add(item, weight ?? new FixedItemWeight<T>(0));
        }

        void ICollection<KeyValuePair<T, double>>.Add(KeyValuePair<T, double> item) {
            lock (locker) baseDict.Add(item.Key, new FixedItemWeight<T>(item.Value));
        }

        void ICollection<KeyValuePair<T, IItemWeight<T>>>.Add(KeyValuePair<T, IItemWeight<T>> item) {
            lock (locker) baseDict.Add(item.Key, item.Value ?? new FixedItemWeight<T>(0));
        }

        /// <summary>
        /// Is the pool contains the specified item?
        /// </summary>
        /// <param name="item">The item that need to checking existance</param>
        /// <returns><c>true</c> if found, otherwise <c>false</c>.</returns>
        public bool Contains(T item) {
            return baseDict.ContainsKey(item);
        }

        bool ICollection<KeyValuePair<T, double>>.Contains(KeyValuePair<T, double> item) {
            return baseDict.ContainsKey(item.Key);
        }

        bool ICollection<KeyValuePair<T, IItemWeight<T>>>.Contains(KeyValuePair<T, IItemWeight<T>> item) {
            return baseDict.ContainsKey(item.Key);
        }

        bool IDictionary<T, double>.ContainsKey(T key) {
            return baseDict.ContainsKey(key);
        }

        bool IDictionary<T, IItemWeight<T>>.ContainsKey(T key) {
            return baseDict.ContainsKey(key);
        }

        bool IDictionary<T, double>.TryGetValue(T key, out double value) {
            IItemWeight<T> rawValue;
            if(baseDict.TryGetValue(key, out rawValue) && rawValue != null) {
                value = rawValue.GetWeight(key);
                return true;
            }
            value = 0;
            return false;
        }

        bool IDictionary<T, IItemWeight<T>>.TryGetValue(T key, out IItemWeight<T> value) {
            return baseDict.TryGetValue(key, out value) && value != null;
        }

        /// <summary>
        /// Copy the pool of items into an array starting from <paramref name="arrayIndex"/>.
        /// </summary>
        /// <param name="array">The array need to contains the copy of the items.</param>
        /// <param name="arrayIndex">Starts from where?</param>
        public void CopyTo(T[] array, int arrayIndex) {
            baseDict.Keys.CopyTo(array, arrayIndex);
        }

        void ICollection<KeyValuePair<T, double>>.CopyTo(KeyValuePair<T, double>[] array, int arrayIndex) {
            foreach(var kv in IterateAsStaticWeight()) array[arrayIndex++] = kv;
        }

        void ICollection<KeyValuePair<T, IItemWeight<T>>>.CopyTo(KeyValuePair<T, IItemWeight<T>>[] array, int arrayIndex) {
            (baseDict as IDictionary<T, IItemWeight<T>>).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes an item from the pool.
        /// </summary>
        /// <param name="item">The item have to remove.</param>
        /// <returns><c>true</c> if successfully remove, otherwise <c>false</c>.</returns>
        public bool Remove(T item) {
            lock (locker) return baseDict.Remove(item);
        }

        bool ICollection<KeyValuePair<T, double>>.Remove(KeyValuePair<T, double> item) {
            lock (locker) return baseDict.Remove(item.Key);
        }

        bool ICollection<KeyValuePair<T, IItemWeight<T>>>.Remove(KeyValuePair<T, IItemWeight<T>> item) {
            lock (locker) return baseDict.Remove(item.Key);
        }

        /// <summary>
        /// Removes everything from the pool.
        /// </summary>
        public void Clear() {
            lock (locker) baseDict.Clear();
        }

        /// <summary>
        /// Gets an enumerator object for iterates through every items in the pool, even the weight is zero.
        /// </summary>
        /// <returns>Enumerator objects which iterates through every items in the pool</returns>
        public IEnumerator<T> GetEnumerator() {
            return baseDict.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return baseDict.Keys.GetEnumerator();
        }

        IEnumerator<KeyValuePair<T, IItemWeight<T>>> IEnumerable<KeyValuePair<T, IItemWeight<T>>>.GetEnumerator() {
            return baseDict.GetEnumerator();
        }

        IEnumerator<KeyValuePair<T, double>> IEnumerable<KeyValuePair<T, double>>.GetEnumerator() {
            return IterateAsStaticWeight().GetEnumerator();
        }

        /// <summary>
        /// Creates a copy of current pool.
        /// </summary>
        /// <returns>A copy of the pool</returns>
        public object Clone() {
            return new WeightedCollection<T>(baseDict);
        }
        #endregion

        /// <summary>
        /// Batch add items into the pool
        /// </summary>
        /// <param name="items">An enumerable object contains all item wanted to add</param>
        public void AddRange(IEnumerable<T> items) {
            lock (locker) {
                if(items == null) throw new ArgumentNullException("items");
                foreach(var item in items) baseDict[item] = new FixedItemWeight<T>(1);
            }
        }

        /// <summary>
        /// Batch remove items from the pool. (if exists)
        /// </summary>
        /// <param name="items">An enumerable object contains all item wanted to remove</param>
        public void RemoveRange(IEnumerable<T> items) {
            lock (locker) {
                if(items == null) throw new ArgumentNullException("items");
                foreach(var item in items) baseDict.Remove(item);
            }
        }

        IEnumerable<KeyValuePair<T, double>> IterateAsStaticWeight() {
            foreach(var kv in baseDict)
                yield return new KeyValuePair<T, double>(kv.Key, kv.Value.GetWeight(kv.Key));
        }

        /// <summary>
        /// Gets the weight controller object of an item.
        /// </summary>
        /// <param name="item">The item</param>
        /// <returns>The weight controller object</returns>
        public IItemWeight<T> GetWeight(T item) {
            IItemWeight<T> weight;
            return baseDict.TryGetValue(item, out weight) && weight != null ? weight : new FixedItemWeight<T>(0);
        }

        /// <summary>
        /// Gets the weight controller object of an item with specific controller type requested.
        /// </summary>
        /// <typeparam name="TItemWeight">A type that implements weight controller interface.</typeparam>
        /// <param name="item">The item</param>
        /// <returns>The weight controller object, <c>null</c> (or default value if type is valuetype such as structure) if item not found or type of weight controller does not match.</returns>
        public TItemWeight GetWeight<TItemWeight>(T item) where TItemWeight : IItemWeight<T> {
            IItemWeight<T> weight;
            return baseDict.TryGetValue(item, out weight) && weight is TItemWeight ? (TItemWeight)weight : default(TItemWeight);
        }

        /// <summary>
        /// Gets the weight of an item. If dynamic object is defined, the value will be fetched immediately.
        /// </summary>
        /// <param name="item">The item</param>
        /// <returns>The weight value</returns>
        public double GetCurrentWeight(T item) {
            IItemWeight<T> weight;
            return baseDict.TryGetValue(item, out weight) && weight != null ? weight.GetWeight(item) : 0;
        }

        /// <summary>
        /// Binds the item with a dynamic weight controller
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="weight">The dynamic weight controller</param>
        /// <returns><c>true</c> if object found and bind successfully, otherwise <c>false</c>.</returns>
        /// <remarks>If <c>null</c> is passed in <paramref name="weight"/>, it will treat as immutable zero weight.</remarks>
        public bool SetWeight(T item, IItemWeight<T> weight) {
            lock (locker) {
                if(!baseDict.ContainsKey(item)) return false;
                baseDict[item] = weight ?? new FixedItemWeight<T>(0);
            }
            return true;
        }

        /// <summary>
        /// Sets a static weight of an item.
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="weight">The weight</param>
        /// <returns><c>true</c> if object found and sets successfully, otherwise <c>false</c>.</returns>
        /// <remarks>If <see cref="ItemWeight{T}"/> is binded, it will change the value of it, otherwise it will bind an immutable static weight controller with the <paramref name="weight"/> defined.</remarks>
        public bool SetWeight(T item, double weight) {
            IItemWeight<T> weightRaw;
            lock (locker) {
                if(!baseDict.TryGetValue(item, out weightRaw)) return false;
                if(weightRaw != null) {
                    var flexibleWeight = weightRaw as ItemWeight<T>;
                    if(flexibleWeight != null) {
                        flexibleWeight.Weight = weight;
                        return true;
                    }
                }
                baseDict[item] = new FixedItemWeight<T>(weight);
            }
            return true;
        }

        /// <summary>
        /// Get a random item from the pool.
        /// </summary>
        /// <param name="random">Optional randomizer, it will use the default one if <c>null</c> is passed or ignored.</param>
        /// <returns>The random item</returns>
        public T GetRandomItem(Random random = null) {
            if(random == null) {
                if(defaultRandomizer == null)
                    defaultRandomizer = new Random();
                random = defaultRandomizer;
            }
            return GetRandomItem(random.NextDouble());
        }

        /// <summary>
        /// Get an item from the pool with random value passed.
        /// </summary>
        /// <param name="randomValue">The randomized value, should be between <c>0</c> and <c>1</c>.</param>
        /// <returns>The random item</returns>
        /// <remarks>This overloaded method is for more advanced uses, which require callers to generate the random number by themself then passing it in.</remarks>
        public virtual T GetRandomItem(double randomValue) {
            T result;
            IItemWeight<T> weight;
            lock (locker) {
                int i = 0, count = baseDict.Count;
                if(count < 1) return default(T);
                double totalWeight = 0, countedWeight = 0;
                var tempList = new KeyValuePair<T, double>[count];
                foreach(var kv in IterateAsStaticWeight()) {
                    tempList[i++] = kv;
                    totalWeight += kv.Value;
                }
                if(count == 1) {
                    result = tempList[0].Key;
                } else {
                    randomValue = ((randomValue % 1 + 1) % 1) * totalWeight;
                    result = tempList[count - 1].Key;
                    for(i = 0; i < count; i++) {
                        countedWeight += tempList[i].Value;
                        if(countedWeight > randomValue) {
                            result = tempList[i].Key;
                            break;
                        }
                    }
                }
                baseDict.TryGetValue(result, out weight);
            }
            var callback = weight as ISuccessCallback<T>;
            if(callback != null) callback.OnSuccess(result);
            return result;
        }
    }
}