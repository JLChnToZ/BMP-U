using System;
using System.Collections.Generic;

public static class BinarySearch {
    public static int BinarySearchIndex<T>(
        this IList<T> sortedList, T key,
        BinarySearchMethod method = BinarySearchMethod.Exact,
        int lowerBoundIndex = 0,
        int upperBoundIndex = -1,
        IComparer<T> comparer = null) {
        if(sortedList == null)
            throw new ArgumentNullException("sortedList");
        if(comparer == null)
            comparer = Comparer<T>.Default;
        int count = sortedList.Count;
        if(count == 0) return -1;
        if(lowerBoundIndex < 0 || lowerBoundIndex >= count)
            throw new ArgumentOutOfRangeException(
                "lowerBoundIndex",
                lowerBoundIndex,
                string.Format("Lower bound index must be greater or equals to 0 but smaller than input length ({0}).", count)
            );
        if(upperBoundIndex < 0) upperBoundIndex = count + upperBoundIndex;
        if(upperBoundIndex < lowerBoundIndex || upperBoundIndex >= count)
            throw new ArgumentOutOfRangeException(
                "upperBoundIndex",
                upperBoundIndex,
                string.Format("Upper bound index must be greater or equals to lower bound index ({0}) but smaller than input length ({1}).", lowerBoundIndex, count)
            );
        bool isFirst = (method & BinarySearchMethod.FirstExact) == BinarySearchMethod.FirstExact;
        bool isLast  = (method & BinarySearchMethod.LastExact) == BinarySearchMethod.LastExact;
        while(lowerBoundIndex <= upperBoundIndex) {
            int middleIndex = lowerBoundIndex + (upperBoundIndex - lowerBoundIndex) / 2;
            int comparison = comparer.Compare(sortedList[middleIndex], key);
            if(comparison < 0)
                lowerBoundIndex = middleIndex + 1;
            else if(comparison > 0)
                upperBoundIndex = middleIndex - 1;
            else if(upperBoundIndex - lowerBoundIndex < 2) {
                if(isFirst && comparer.Compare(sortedList[lowerBoundIndex], key) == 0)
                    return lowerBoundIndex;
                if(isLast && comparer.Compare(key, sortedList[upperBoundIndex]) == 0)
                    return upperBoundIndex;
                return middleIndex;
            } else if(isLast)
                lowerBoundIndex = middleIndex;
            else if(isFirst)
                upperBoundIndex = middleIndex;
            else
                return middleIndex;
        }
        if((method & BinarySearchMethod.FloorClosest) == BinarySearchMethod.FloorClosest)
            return upperBoundIndex;
        if((method & BinarySearchMethod.CeilClosest) == BinarySearchMethod.CeilClosest)
            return lowerBoundIndex;
        return -1;
    }
    
    public static T FindClosestValue<T>(
        this IList<T> sortedList, T key,
        bool findLarger,
        int lowerBoundIndex = 0,
        int upperBoundIndex = -1,
        IComparer<T> comparer = null,
        T defaultValue = default(T)) {
        int resultIdx = BinarySearchIndex(
            sortedList, key,
            findLarger ? BinarySearchMethod.CeilClosest : BinarySearchMethod.FloorClosest,
            lowerBoundIndex,
            upperBoundIndex,
            comparer
        );
        return resultIdx < 0 || sortedList.Count < 1 ? defaultValue : sortedList[resultIdx];
    }

    public static int InsertInOrdered<T>(this IList<T> sortedList, T item, IComparer<T> comparer = null, int fromIndex = 0, int toIndex = -1) {
        if(sortedList == null)
            throw new ArgumentNullException("sortedList");
        if(sortedList.IsReadOnly)
            throw new ArgumentException("Cannot alter a read-only collection.");
        int index = BinarySearchIndex(sortedList, item, BinarySearchMethod.CeilClosest | BinarySearchMethod.FirstExact, fromIndex, toIndex, comparer);
        if(index >= sortedList.Count) {
            index = sortedList.Count;
            sortedList.Add(item);
        } else if(index < 0) {
            index = 0;
            sortedList.Insert(0, item);
        } else {
            sortedList.Insert(index, item);
        }
        return index;
    }
    
    public static void InsertInOrdered<T>(this IList<T> sortedList, IEnumerable<T> items, IComparer<T> comparer = null, int fromIndex = 0, int toIndex = -1) {
        if(sortedList == null)
            throw new ArgumentNullException("sortedList");
        if(items == null)
            throw new ArgumentNullException("items");
        if(sortedList.IsReadOnly)
            throw new ArgumentException("Cannot alter a read-only collection.");
        if(comparer == null)
            comparer = Comparer<T>.Default;
        const BinarySearchMethod method = BinarySearchMethod.CeilClosest | BinarySearchMethod.FirstExact;
        int index = -1;
        bool hasPreviousItem = false;
        T previousItem = default(T);
        foreach(var item in items) {
            if(!hasPreviousItem)
                index = BinarySearchIndex(sortedList, item, method, fromIndex, toIndex, comparer);
            else {
                int comparison = comparer.Compare(previousItem, item);
                if(comparison < 0)
                    index = BinarySearchIndex(sortedList, item, method, index, toIndex, comparer);
                else if(comparison > 0)
                    index = BinarySearchIndex(sortedList, item, method, fromIndex, index, comparer);
            }
            previousItem = item;
            hasPreviousItem = true;
            if(index >= sortedList.Count)
                sortedList.Add(item);
            else if(index < 0) {
                index = 0;
                sortedList.Insert(0, item);
            } else
                sortedList.Insert(index, item);
            if(toIndex >= 0) toIndex++;
        }
    }
}
    
[Flags]
public enum BinarySearchMethod {
    Exact        = 0x00, // 00 0000
    FloorClosest = 0x05, // 00 0101
    CeilClosest  = 0x06, // 00 0110
    FirstExact   = 0x28, // 10 1000
    LastExact    = 0x30  // 11 0000
}