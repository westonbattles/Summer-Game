# Enemy Combat Architecture

Composition-first enemy system for the roguelike (RoR2 / Diablo-style). Inheritance
is used only where it earns its keep; everything else is components.

---

## Design principles

- **Composition over inheritance.** `Enemy` is a thin shared base for pooling and
  generalization. Capabilities (HP, attacks, affixes) are separate components.
- **Two deliberate inheritance exceptions:**
  1. `Enemy` base class — shared spine for all enemies including bosses.
  2. `AttackBehaviour` base → `MeleeAttack` / `RangedAttack` — a shallow
     template-method hierarchy that exists purely to dedupe the attack loop.
     Subclasses differ only in the single hit step.
- **Damage flows through `IDamageable`.** Anything that can be hurt implements it,
  so melee, projectiles, environment, and affixes all funnel through one contract.
- **Affixes attach as components at spawn.** HP and stats expose modifier-friendly
  seams (e.g. `Health.SetMaxHP`) rather than locked-down fields.

---

## Components

### `IDamageable`
The damage contract. Implemented by `Health` now; player/destructibles later.
- `void TakeDamage(DamageInfo info)`
- `bool IsAlive { get; }`

### `DamageInfo` (struct)
The envelope carried by every hit — not a bare float, so receivers know *what kind*
of hit it was.
- `float Amount`
- `GameObject Source` — for kill credit, thorns, aggro
- `bool IsCrit`
- *(future)* `DamageType Type` — add an enum when elemental affixes land.

### `Health` (MonoBehaviour, `IDamageable`)
Owns HP. Pulled out of `Enemy` so affixes and damage sources can interact with it.
- Floats throughout (DoT, percent affixes, crit multipliers produce fractions).
- `isDead` guard ensures `OnDeath` fires **exactly once** even on same-frame overkill
  (real concern with pooled enemies + damage bursts — prevents double XP / double VFX).
- Events (C# `Action`):
  - `OnHealthChanged(current, max)` — for health bars.
  - `OnDeath(DamageInfo killingBlow)` — carries the killing blow for kill credit.
- `SetMaxHP(newMax)` refills on purpose — affixes apply at spawn, pre-combat.
  This is the seam that later becomes `AddModifier` when the modifier-stack
  affix system is built. Nothing else changes because everyone reads `MaxHP`.

### `Enemy` (MonoBehaviour, `RequireComponent(Health)`)
Thin shared base. Holds only what every enemy needs for pooling/generalization
(`movementSpeed`, `XPValue`, `AffixEligible`).
- `Awake` is `protected virtual` so `Boss` can override (phase data) and call base.
- Subscribes to `Health.OnDeath` in **`OnEnable`/`OnDisable`**, not `Awake` —
  correct for pooled objects, which re-enable many times but only `Awake` once.
- `HandleDeath` is currently a TODO stub (XP grant, drops, return-to-pool).

### `AttackBehaviour` (abstract, `RequireComponent(Animator)`)
Owns the shared attack loop: target tracking, engagement gate, cooldown.
- Target injected by spawner via `SetTarget` (pooling-friendly; pass null to clear).
- **Telegraphed attacks** — the attack is split into two moments:
  - `StartAttack()` — decision; triggers the animation + starts cooldown.
    Called from `Update` when in range and off cooldown.
  - `OnAttackHit()` — contact; lands the hit. **Public** because Unity Animation
    Events invoke it by name via reflection (can't call protected). Called by an
    Animation Event on the swing/cast's contact frame.
  - Without the wind-up these are the same instant; the animation separates them,
    which is what makes melee dodgeable.
- `attackCooldown` = **seconds between attacks** (not frequency). Cooldown starts
  at commit (`StartAttack`), so a slow wind-up doesn't double-penalize attack speed.
- Caches the animator trigger hash (`static readonly int`) to avoid per-call string hashing.

### `MeleeAttack : AttackBehaviour`
Deals damage itself (it's the thing in contact).
- `OnAttackHit` does an overlap check at `swingReach` and calls `TakeDamage` on
  every `IDamageable` found.
- Uses **`Physics.OverlapSphereNonAlloc`** with a pre-allocated reusable buffer —
  zero steady-state allocation, which matters for hordes of melee enemies on older
  devices (GC pauses are the frame hitches players feel).
- Overrides `Awake` → **must call `base.Awake()`** (base grabs the animator) and
  allocates the hit buffer once.

### `RangedAttack : AttackBehaviour`
Does **not** deal damage itself — hands its `DamageInfo` to a projectile, which
calls `TakeDamage` on contact (matches the diagram: projectile "calls getter from
Ranged Attack").
- `proximityToAttack` means **engagement distance** here — tune it large (15–20) so
  archers shoot from afar instead of walking into melee range.
- Aim direction is computed in `OnAttackHit` (fire time), not at decision time, so
  the shot leads toward where the player is *after* the wind-up — emergent dodge window.
- Projectile spawn is a **TODO** pending the `Projectile` class + pool.

---

## `range` vs `proximityToAttack`

`range` was **dropped from the base** — it was confusing and unused.
- `proximityToAttack` = the **decision** ("am I close enough to start the attack?").
- Reach is now per-subclass: melee has its own `swingReach`; ranged delegates reach
  to the projectile's own travel.

---

## Physics query note (NonAlloc)

- **3D:** `Physics.OverlapSphereNonAlloc` is **Supported** in Unity 6.x.
  Correct choice. Reuse the buffer; iterate `0..count`, never `buffer.Length`
  (stale colliders live past `count`). `maxHits` is a silent hard cap — size for the
  worst realistic crowd.
- If physics queries ever profile as a real cost, the Jobs-based batch query API
  is the next step — not before.

---

## Build status

| Piece                          | Status |
| ------------------------------ | ------ |
| `IDamageable` / `DamageInfo`   | Done   |
| `Health`                       | Done, tested (HealthTester) |
| `Enemy` (slimmed)              | Done   |
| `AttackBehaviour` (telegraphed)| Done   |
| `MeleeAttack` (NonAlloc)       | Done   |
| `RangedAttack`                 | Logic done; projectile spawn stubbed |
| `Projectile` + pool            | Not started |
| `HandleDeath` (XP / pooling)   | Stub   |
| Affix system (modifier stack)  | Not started |
| `Boss` (phases)                | Not started |

---

## Known TODOs / next decisions

- **`Projectile` class + pool** — the natural next build; `RangedAttack` waits on it.
- **`HandleDeath`** — wire XP grant (credit `killingBlow.Source`), drops, return-to-pool.
- **Pooling reset** — on reuse, `Health` must reset `currentHP` and `isDead`;
  attack components re-inject target. (Pooling is its own architecture conversation.)
- **Affix system** — when built, `Health.MaxHP` becomes a computed property over a
  modifier list and `SetMaxHP` becomes `AddModifier`.
- **`Boss`** — override `Enemy.Awake` for phase data; otherwise an Enemy + components.
- **Animation gotcha** — `OnAttackHit` only fires if the attack clip has an Animation
  Event wired to it. No event = enemy decides to attack but never connects (not a bug).
