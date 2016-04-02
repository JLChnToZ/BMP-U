namespace JLChnToZ.LuckyPlayer.WeightedRandomizer {
    sealed class FixedItemWeight<T>: IItemWeight<T> {
        readonly double weight;

        internal FixedItemWeight(double weight) {
            this.weight = weight;
        }

        public double GetWeight(T item) {
            return weight;
        }
    }
}
