# Player Ability System — Core

A four-slot ability bar (Primary / Secondary / Movement / Special), RoR2 / Diablo style. Cooldown-gated, with optional multi-charge abilities. Player-only — enemies have their own `EnemyMeleeAttack` / `EnemyRangedAttack` and don't touch this.

This is the **core only**. The concrete effects and ability assets are yours to build.

## The idea: data vs. state vs. behaviour

- **Data** — `AbilityDefinition`, a ScriptableObject asset. Cooldown, damage number, icon, and the list of effects. Shared by anything that uses it; never modified at runtime.
- **Runtime state** — `AbilityInstance`, a plain C# object, one per slot. Holds the live cooldown timer and charges. The only place mutable state lives.
- **Behaviour** — `AbilityEffect`, composable pieces. An ability is a *list* of effects run in order, so a fireball and a dash are the same `AbilityDefinition` class with different effects (no subclass-per-ability).

Why the split: a ScriptableObject is one shared asset. If cooldown lived on it, every user would share the same timer (and it'd persist between play sessions). So per-entity state goes on `AbilityInstance` instead. Treat `AbilityDefinition` and every `AbilityEffect` as read-only at runtime.

## Flow, one activation

`AbilityController` reads input → if the slot is ready, builds an `AbilityContext` (caster, aim, damage number) → `AbilityInstance.TryActivate` spends a charge and starts the cooldown → `AbilityDefinition.Execute` runs each effect.

Damage detail: the asset stores only `baseDamage` (a number), because `DamageInfo.Source` (the caster) and `IsCrit` (a roll) are runtime-only. `AbilityContext.BuildDamage()` is the single place a `DamageInfo` is assembled — every effect funnels through it.

## Files

Three files. The two Unity-tracked types each get their own file (Unity wants the file name to match a ScriptableObject / MonoBehaviour class name); the plain types are grouped together.

```
AbilityCore.cs        the plain types, grouped:
                        AbilitySlot     enum: Primary / Secondary / Movement / Special
                        AbilityContext  readonly struct passed to effects; has BuildDamage()
                        AbilityEffect   abstract base — subclass this for concrete effects
                        AbilityInstance per-slot runtime state (cooldown + charges)
AbilityDefinition.cs  the ScriptableObject (data) — solo (Unity tracks the asset)
AbilityController.cs  MonoBehaviour on the player — owns 4 instances, reads input — solo
```

UI reads via `AbilityController.GetInstance(slot)` → `CurrentCharges` (pips) and `RechargeProgress01` (0..1 radial fill). The controller never references the UI.

See `TODO.md` for what to implement.