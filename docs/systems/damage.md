# Damage

**What it does:** deals damage to an actor, and heals one, through the game's own health
calculation. Use it whenever a mod needs to hurt or heal something — a custom trap, a scripted
event, a spell, a co-op mod replaying a blow that landed on another machine.

Entry point: `Drova_Modding_API.Access.DamageAccess` (static).

## Quick example

```csharp
using Drova_Modding_API.Access;
using Il2CppDrova;

if (PlayerAccess.TryGetPlayer(out Actor player))
{
    DamageAccess.TryApplyPhysicalDamage(player, 25);
}
```

## How do I…?

### Deal a specific kind of damage

```csharp
if (DamageAccess.TryGetMagicalDamageType(out DamageStatDesc magical))
{
    DamageAccess.TryApplyDamage(target, 40, magical);
}
```

### Deal damage nothing resists

```csharp
if (DamageAccess.TryGetPureDamageType(out DamageStatDesc pure))
{
    DamageAccess.TryApplyDamage(target, 10, pure);
}
```

The target's armor and resistances stay out of it, but the blockers inside the health calculation
still run — which is the reason to go through this rather than write the field.

### Heal

```csharp
DamageAccess.TryHeal(player, 30);
```

### Attribute the blow to an attacker

```csharp
DamageAccess.TryApplyPhysicalDamage(target, 25, attacker);
```

Read the first note below before you do. A source is not only a name on the blow.

## API reference

| Member                                                                                             | Description                                                                                       |
|----------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------|
| `bool TryApplyDamage(Actor target, int amount, DamageStatDesc damageType, Entity? source = null)`  | Deals damage of a given type. `false` when the actor has no health component or the type is null. |
| `bool TryApplyDamage(Health target, int amount, DamageStatDesc damageType, Entity? source = null)` | Same, for callers that already hold the health component.                                         |
| `bool TryApplyPhysicalDamage(Actor target, int amount, Entity? source = null)`                     | Deals physical damage, the type essentially every weapon deals.                                   |
| `bool TryHeal(Actor target, int amount, Entity? source = null)`                                    | Restores health through the same calculation.                                                     |
| `bool TryGetPhysicalDamageType(out DamageStatDesc damageType)`                                     | The type answered by armor and physical resistance.                                               |
| `bool TryGetMagicalDamageType(out DamageStatDesc damageType)`                                      | The type answered by magical resistance. What most spells deal.                                   |
| `bool TryGetPureDamageType(out DamageStatDesc damageType)`                                         | The type nothing resists.                                                                         |
| `bool TryGetHealingType(out DamageStatDesc damageType)`                                            | The type that makes a health change count as healing.                                             |

## Notes & gotchas

- **A source is not just attribution.** The game runs that entity's offensive stats over the value,
  adding its damage stat for this type and scaling by its damage factor. If the number you have
  already reflects the attacker's gear, naming them counts their weapon twice. Leave `source` null
  whenever the figure is final.
- **What a null source costs** is only the attacker's side of the blow: the aggro tag, the killed-by
  credit, the block punishment, and the crit and bleed rolls, which the game only makes for a source
  it recognizes as the player. The target's guard still works — the block angle is only measured for
  projectiles — so blocking, stamina and the perfect-block window all still decide the outcome.
- **The amount is a positive magnitude.** Whether it heals or hurts is a property of the
  `DamageStatDesc`, not of the sign; `HealthChange` negates it itself for a damaging type. Passing a
  negative amount does not heal, it applies a nonsensical figure.
- **This deals a blow, it does not set an outcome.** The health actually lost is decided by the
  target's resistances, armor and active blockers. If you need an exact figure, use the pure damage
  type, and even then an invincible or blocking target may lose nothing.
- **Never write `Health._curHealth` or call `SetHealth` to apply damage.** Both skip the calculation,
  and the damage blockers live inside it. One of those blockers is what turns a killing blow into
  something other than death, so a raw write reaching zero latches `_isDead` — which nothing in the
  game ever clears — and the actor is permanently dead with every revive path standing by unused.
- **The four damage types live on two different handlers.** Physical and magical come from
  `GameplaySettingsHandler`, pure and healing from `GlobalAssetsGameHandler`. That is the only reason
  they have four accessors instead of one enum. The full set of damage types is authored data
  (`DamageStatDesc` assets), not an enum, so a mod cannot enumerate them — these four are the ones
  the game itself reaches for by name.
- All accessors return `false` while the game is bootstrapping or no world is loaded. Check it.
