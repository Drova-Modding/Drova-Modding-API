using Drova_Modding_API.Access;
using Il2CppDrova;
using Il2CppDrova.InventorySystem;
using Il2CppDrova.Items;
using MelonLoader;
using UnityEngine;
using Il2CppCollections = Il2CppSystem.Collections.Generic;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Turns a spawned NPC into a trader, stocked in the game's own <see cref="Inventory_Trading"/>.
    /// Stock can wait for a chapter.
    /// The trade window still needs a trade-window node in the NPC's dialogue.
    /// </summary>
    public class TraderModule : INpcModule
    {
        /// <summary>Vanilla traders get a quarter of their starting money on every chapter change.</summary>
        private const float MoneyEachChapterShare = 0.25f;

        private float _money;
        private readonly List<(string ReadableId, int Amount, int Chapter)> _items = [];

        /// <summary>
        /// Ignored: the trade window shows the NPC's name, as for vanilla traders.
        /// </summary>
        /// <returns>This module for chaining</returns>
        [Obsolete("The trade window shows the NPC's name.")]
        public TraderModule WithName(string name)
        {
            return this;
        }

        /// <summary>
        /// Sets the amount of money the trader has available.
        /// </summary>
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
        /// <param name="chapter">First chapter the item is on sale, as <c>WorldVars.CurrentChapter</c> counts. 0 and 1 sell it from the start.</param>
        /// <returns>This module for chaining</returns>
        public TraderModule WithItem(string readableId, int amount = 1, int chapter = 0)
        {
            if (!string.IsNullOrWhiteSpace(readableId))
                _items.Add((readableId, Math.Max(1, amount), Math.Max(0, chapter)));

            return this;
        }

        /// <inheritdoc />
        public void Apply(ModuleContext context)
        {
            Actor? actor = context.GetComponent<Actor>();
            if (actor == null)
                return;

            InventoryActorModule? module = actor.GetInventoryActorModule().TryCast<InventoryActorModule>();
            if (module == null)
                return;

            // Human_Template carries no trade inventory. With one, Actor.GetTraderActor builds the vanilla adapter,
            // which filters stock by chapter and saves what was bought.
            Inventory_Trading? inventory = actor.GetComponentInChildren<Inventory_Trading>(true);
            if (inventory == null)
                inventory = actor.gameObject.AddComponent<Inventory_Trading>();
            module._tradeInventory = inventory;

            Il2CppCollections.List<ConditionalItemStack> stock = new();
            SubDatabase_Item? database = ProviderAccess.ItemDatabase;
            foreach ((string readableId, int amount, int chapter) in _items)
            {
                Item? item = database != null ? database.GetItemByReadableId(readableId) : null;
                if (item == null)
                {
                    MelonLogger.Warning($"{nameof(TraderModule)}: could not resolve item readable id '{readableId}'.");
                    continue;
                }

                stock.Add(new ConditionalItemStack(item, amount, chapter));
            }

            if (_money > 0f)
            {
                stock.Add(new ConditionalItemStack(ItemUtil.GetMoneyItem(), Mathf.RoundToInt(_money), 0));
                inventory._moneyEachChapter = Mathf.RoundToInt(_money * MoneyEachChapterShare);
            }

            inventory.SetItems(stock);
            actor._tradeActor = null;
        }
    }
}
