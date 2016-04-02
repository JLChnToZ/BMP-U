namespace JLChnToZ.LuckyPlayer.WeightedRandomizer {
    /// <summary>
    /// Default implementation of dynamic controller which allows changes weight without touching the <see cref="WeightedCollection{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ItemWeight<T>: IItemWeight<T> {
        double weight;
        /// <summary>
        /// The weight.
        /// </summary>
        public double Weight {
            get { return weight; }
            set { weight = value; }
        }

        /// <summary>
        /// Constructor, defaults the weight is <c>1</c>.
        /// </summary>
        public ItemWeight() { weight = 1; }

        /// <summary>
        /// Constructor with custom weight defined.
        /// </summary>
        /// <param name="weight">The initial weight</param>
        public ItemWeight(double weight) { this.weight = weight; }

        /// <summary>
        /// Gets the current weight
        /// </summary>
        /// <param name="item">The item need to check</param>
        /// <returns>The weight</returns>
        /// <remarks>This is the interface method for fetching weight.</remarks>
        public virtual double GetWeight(T item) {
            return weight;
        }
    }
}
