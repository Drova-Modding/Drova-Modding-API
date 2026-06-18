using Drova_Modding_API.Access;
using Il2CppDrova;
using Il2CppDrova.Items;
using Il2CppTradingSystem.ItemContainers;
using Il2CppTradingSystem.Services;
using MelonLoader;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Turns a spawned NPC into a trader by constructing a <see cref="Trader"/>, stocking it with
    /// configurable items (with per-item amounts) and money, and assigning it to
    /// <see cref="Actor._tradeActor"/>.
    /// </summary>
    /// <remarks>
    /// Setting up the trader only populates <see cref="Actor._tradeActor"/>. To actually open the
    /// trade window in-game the NPC's dialogue still needs a trade-window node.
    /// </remarks>
    public class TraderModule : INpcModule
    {
        private string? _name;
        private float _money;
        private readonly List<(string ReadableId, int Amount)> _items = [];

        /// <summary>
        /// Sets the trader display name. When left blank, the NPC's name is used at apply time.
        /// </summary>
        /// <param name="name">Trader display name</param>
        /// <returns>This module for chaining</returns>
        public TraderModule WithName(string name)
        {
            _name = name;
            return this;
        }

        /// <summary>
        /// Sets the amount of money the trader has available.
        /// </summary>
        /// <param name="money">Trader money</param>
        /// <returns>This module for chaining</returns>
        public TraderModule WithMoney(float money)
        {
            _money = money;
            return this;
        }

        /// <summary>
        /// Adds an item to the trader's stock.
        /// </summary>
        /// <param name="readableId">Readable id from the item database</param>
        /// <param name="amount">Stock amount, clamped to at least 1</param>
        /// <returns>This module for chaining</returns>
        public TraderModule WithItem(string readableId, int amount = 1)
        {
            if (!string.IsNullOrWhiteSpace(readableId))
                _items.Add((readableId, Math.Max(1, amount)));

            return this;
        }

        /// <inheritdoc />
        public void Apply(ModuleContext context)
        {
            var actor = context.GetComponent<Actor>();
            if (actor == null)
                return;

            var items = new Il2CppSystem.Collections.Generic.List<IItemContainer>();
            foreach (var (readableId, amount) in _items)
            {
                var item = ProviderAccess.ItemDatabase.GetItemByReadableId(readableId);
                if (item == null)
                {
                    MelonLogger.Warning($"{nameof(TraderModule)}: could not resolve item readable id '{readableId}'.");
                    continue;
                }

                var container = new ItemContainer(item.TryCast<IItem>(), amount);
                items.Add(container.TryCast<IItemContainer>());
            }

            string traderName = !string.IsNullOrWhiteSpace(_name) ? _name! : actor.gameObject.name;

            // The float ctor argument is ignored by the game; money must be added explicitly.
            var trader = new Trader(traderName, 0f, items);
            if (_money > 0f)
                trader.AddTraderMoney(_money);

            actor._tradeActor = trader.TryCast<ITrader>();
        }
    }
}
