namespace JLChnToZ.LuckyPlayer {
    /// <summary>
    /// An alternative of <see cref="LuckyController{T}"/> but with limited supply,
    /// which means the item binded will be available in limited amount.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>You can inherit this class to add customizaton</remarks>
    public class LimitedLuckyController<T>: LuckyController<T> {
        internal protected int amount;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rare">The rarity which will affects the player's luckyness.</param>
        /// <param name="initialAmount">How many of the item initially have?</param>
        /// <param name="baseRarity">Alterable rarity value</param>
        public LimitedLuckyController(double rare, int initialAmount = 1, double baseRarity = 1) : base(rare, baseRarity) {
            amount = initialAmount;
        }

        /// <summary>
        /// Same usage as <see cref="IItemWeight{T}.GetWeight(T)"/>
        /// </summary>
        public override double GetWeight(T item) {
            return base.GetWeight(item) * amount;
        }

        /// <summary>
        /// Calles when on item successfully selected, the amount will minus one when called.
        /// </summary>
        /// <param name="item">The selected item</param>
        public override void OnSuccess(T item) {
            if(amount > 0) amount--;
            base.OnSuccess(item);
        }
    }
}
