# Status effects

Apply status effects the same way food, potions and hits do. Use it for a mod's own buffs, debuffs, traps
or rewards.

Entry point: `Drova_Modding_API.Access.StatusEffectAccess` (static).

## Quick example

```csharp
using Drova_Modding_API.Access;
using Drova_Modding_API.GlobalFields;
using Il2CppDrova;
using Il2CppDrova.StatusEffects;

if (PlayerAccess.TryGetPlayer(out Actor? player))
{
    // The windwalker potion's effect, with the potion as tooltip.
    if (StatusEffectAccess.TryGet(StatusEffectGuids.Consumable.PotionWindwalker, out StatusEffect? windwalker))
    {
        StatusEffectAccess.TryApply(player, windwalker, ProviderAccess.ItemDatabase?.GetItemByReadableId("item_potion_windwalker"));
    }

    // Bleed for 30 health in total.
    StatusEffectAccess.TryApplyBleeding(player, 30);
}
```

## API

| Member                                                                 | What it does                                                                                                                                                      |
|------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `TryGet(guid, out effect)`                                             | Looks up an effect by GUID. `StatusEffectGuids` lists every GUID, grouped into `Buff`, `Consumable`, `Talent`, `Trinket` and `Other`.                             |
| `TryApply(actor, effect, relatedItem = null, stacks = 0, seconds = 0)` | Applies the effect. `stacks` and `seconds` override the effect's own values when above 0. Returns false when the game refused it (actor dead, dodging or immune). |
| `TryApplyBleeding(actor, damage)`                                      | Makes the actor bleed for `damage` health in total.                                                                                                               |

## Things to know

- Effect names are not unique, so always use the GUID constants.
- Applying a food or potion effect is the same as using the item: the item itself has no healing.
- Most food and potion effects have no tooltip text. Pass the item as `relatedItem`, or the HUD icon shows
  an empty tooltip.
- Only one food effect is active at a time, and its timer only runs while the actor is hurt.
- Short effects such as poison (5 s) are easy to miss. Pass `seconds` to make them last longer.
- Effects with a HUD icon are saved and restored with the save game automatically.
- In co-op an effect on the local player runs on that machine only. Teammates see its result through the
  synced health, not the icon.
