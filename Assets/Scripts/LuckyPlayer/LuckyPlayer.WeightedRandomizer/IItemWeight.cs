namespace JLChnToZ.LuckyPlayer.WeightedRandomizer {
    /// <summary>
    /// Interface for dynamic weight controller.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IItemWeight<T> {
        /// <summary>
        /// Gets the weight current defines
        /// </summary>
        /// <param name="item">The item current querying</param>
        /// <returns>The weight of the item</returns>
        double GetWeight(T item);
    }
}
