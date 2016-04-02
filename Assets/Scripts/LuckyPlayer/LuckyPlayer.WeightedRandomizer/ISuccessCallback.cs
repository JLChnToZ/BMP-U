namespace JLChnToZ.LuckyPlayer.WeightedRandomizer {

    /// <summary>
    /// Interface for dynamic weight controller with success callback.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISuccessCallback<T>: IItemWeight<T> {
        /// <summary>
        /// Callback for success selected the item when gets an item randomly from <see cref="WeightedCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item which successfully selected</param>
        /// <remarks>This will only be called when the selected item is binded with current weight controller</remarks>
        void OnSuccess(T item);
    }
}
