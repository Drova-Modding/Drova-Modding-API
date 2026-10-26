# Actor buffs and enemies

Make an actor tougher or stronger at runtime, and find the nearest enemy of a player. Use it for elite
enemies, boss phases or a temporary blessing on the player.

Entry points: `Drova_Modding_API.Access.ActorBuffAccess` and `Drova_Modding_API.Access.EnemyAccess` (static).

## Quick example

```csharp
using Drova_Modding_API.Access;
using Il2CppDrova;

if (PlayerAccess.TryGetPlayer(out Actor? player)
    && EnemyAccess.TryFindNearest(player, 400f, null, out Actor? enemy))
{
    // Double health and damage, plus 8 armor and 8 magic resistance.
    ActorBuffAccess.TryApply(enemy, new ActorBuff(2f, 2f, 8, 8));
}
```

## API

| Member                                                                          | What it does                                                                                                |
|---------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------|
| `ActorBuff(HealthFactor = 1, DamageFactor = 1, Armor = 0, MagicResistance = 0)` | The buff to apply.                                                                                          |
| `HealthFactor`                                                                  | Multiplies max health, then heals to full.                                                                  |
| `DamageFactor`                                                                  | Multiplies all damage the actor deals: melee, ranged and spells.                                            |
| `Armor` / `MagicResistance`                                                     | Flat points added to physical / magical resistance. 10 points block about a quarter of a hit, 25 about 40%. |
| `ActorBuffAccess.TryApply(actor, buff)`                                         | Applies the buff.                                                                                           |
| `EnemyAccess.TryFindNearest(player, radius, filter, out enemy)`                 | Closest hostile actor within `radius`. `filter` is an optional extra condition.                             |

## Things to know

- Nothing is saved. After a save load, or when the actor's scene unloads, the actor is normal again.
- Applying twice stacks. Track which actors you buffed if that matters.
- Stats are capped by the game. When a damage factor hits the cap, a warning in the log names the stat.
- In co-op each machine has its own copy of an enemy. Apply the buff on every machine, the API does not
  sync it.
- `TryFindNearest` skips brawl opponents and knocked out actors. A radius of 400 reaches past the screen edge
  at 720p.
