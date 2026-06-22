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

### `Health` (MonoBehaviour, `IDamageable`, `IPoolable`)
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
- `OnSpawn` (IPoolable) resets `isDead` and refills `CurrentHP` to `MaxHP`. Refills
  to `MaxHP` not `baseMaxHP`, so it must run **after** affixes set max (see spawner).

### `Enemy` (MonoBehaviour, `RequireComponent(Health)`)
Thin shared base. Holds only what every enemy needs for pooling/generalization
(`movementSpeed`, `XPValue`, `AffixEligible`).
- `Awake` is `protected virtual` so `Boss` can override (phase data) and call base.
- Subscribes to `Health.OnDeath` in **`OnEnable`/`OnDisable`**, not `Awake` —
  correct for pooled objects, which re-enable many times but only `Awake` once.
- Exposes its own `OnDied(Enemy)` event, distinct from `Health.OnDeath`. The chain:
  `Health.OnDeath` → `Enemy.HandleDeath` (enemy-specific work) → `Enemy.OnDied`
  (spawner listens, releases to pool). Two layers, each with one job.
- `HandleDeath` order is load-bearing: **(1) XP → (2) drops → (3) release**. Death
  work runs while the enemy is still active and positioned; release is always last.

**Death events (seams for systems that don't exist yet):**
- `static event OnAnyEnemyGrantedXP(int xp, GameObject killer)` — **static** so the
  player progression system subscribes **once at startup** instead of per-enemy. Carries
  the XP amount and the killer (`killingBlow.Source`).
- `event OnDropLoot(Vector3 position, bool affixEligible)` — **instance** event; loot
  is naturally per-enemy/per-spawner. Fires at death position; `affixEligible` lets a
  loot system give elites better drops.
- **Why static for XP, instance for drops:** matches who listens. XP → one global
  receiver (subscribe once). Drops → local/per-spawner receivers. Not inconsistency.
- **Null credit is forwarded honestly.** `killingBlow.Source` may be null (traps, DoT
  outliving its caster, recycled projectile source). The event passes null through; the
  progression system decides what "killed by nobody" means. Don't fake it to the player here.
- **Static-event leak caveat:** the *subscriber* (progression system) must unsubscribe in
  `OnDestroy`. `Enemy` only invokes the static event, never subscribes, so no leak on this side.

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
- Owns an `ObjectPool<Projectile>` (Unity 6 built-in). On hit: `pool.Get()`, place at
  `firePoint`, `Launch(...)` with `pool.Release` as the release callback.
- **Scope note:** one pool per component is fine for a few archer types. Hundreds of
  ranged enemies sharing a projectile type → move to a shared `ProjectilePoolManager`.
  Projectile code doesn't change (it's pool-agnostic); only pool ownership moves.

### `Projectile` (MonoBehaviour, `RequireComponent(Collider, Rigidbody)`)
Pool-agnostic flying damage-dealer. Single-hit, trigger-based.
- `Launch(direction, speed, DamageInfo, releaseCallback)` — spawner injects everything,
  including *how to release itself*. Projectile never references the pool directly.
- `OnTriggerEnter`: layer-filter → `TakeDamage` → despawn. `isSpent` guard prevents
  double-release on same-frame multi-collider contact (same pattern as `Health.isDead`).
- Moves kinematically via `transform.position` (predictable straight line). **Rigidbody
  must be Is Kinematic = true**; it exists only because `OnTriggerEnter` requires a
  Rigidbody on one of the two colliders. Most common "trigger won't fire" gotcha.
- `lifetime` timer despawns no-hit projectiles so they don't leak out of the pool.

### `IPoolable` (interface)
Lifecycle contract for pooled-object reset: `OnSpawn()` / `OnDespawn()`.
- Reset is **distributed** — each component resets only its own state. `Health` resets
  HP; attack components clear target; future affixes strip themselves. No single class
  knows how to reset everything.
- Spawner calls `OnSpawn` on every `IPoolable` after `Get`, `OnDespawn` before `Release`,
  via `GetComponentsInChildren<IPoolable>()` — so affix components added later are reset
  automatically with zero extra wiring.

### `EnemySpawner`
Owns `ObjectPool<Enemy>`; conducts spawn/reset/release.
- **`OnDied += ReleaseEnemy` is subscribed once in `createFunc`**, never per-spawn —
  re-subscribing on reuse would stack handlers and cause multi-release.
- **Spawn ordering is load-bearing:** (1) apply affixes [future hook], (2) `OnSpawn` all
  poolables (Health refills to the affix-bumped MaxHP), (3) inject player as target into
  all `AttackBehaviour`. Reverse 1↔2 and elites spawn at base HP.
- Release: `OnDespawn` all poolables, clear attack targets (null), `pool.Release`.

---

## `range` vs `proximityToAttack`

`range` was **dropped from the base** — it was confusing and unused.
- `proximityToAttack` = the **decision** ("am I close enough to start the attack?").
- Reach is now per-subclass: melee has its own `swingReach`; ranged delegates reach
  to the projectile's own travel.

---

## Physics query note (NonAlloc)

- **3D (this project):** `Physics.OverlapSphereNonAlloc` is **Supported** in Unity 6.x.
  Correct choice. Reuse the buffer; iterate `0..count`, never `buffer.Length`
  (stale colliders live past `count`). `maxHits` is a silent hard cap — size for the
  worst realistic crowd.
- **2D caveat (future):** the *2D* NonAlloc queries are tentatively deprecated; the
  modern path is the `List`-based overload (e.g. `Physics2D.OverlapCircle(... List)`).
  Only relevant if a system ever goes 2D.
- If physics queries ever profile as a real cost, the Jobs-based batch query API
  is the next step — not before.

---

## Build status

| Piece                          | Status |
| ------------------------------ | ------ |
| `IDamageable` / `DamageInfo`   | Done   |
| `Health`                       | Done, tested (HealthTester) |
| `Health` IPoolable reset       | Done   |
| `Enemy` (slimmed)              | Done   |
| `Enemy.OnDied` + HandleDeath wiring | Done   |
| XP grant event (`OnAnyEnemyGrantedXP`) | Done (event emitted; receiver TODO) |
| Drop hook (`OnDropLoot`)       | Done (event emitted; receiver TODO) |
| `AttackBehaviour` (telegraphed)| Done   |
| `MeleeAttack` (NonAlloc)       | Done   |
| `RangedAttack` + projectile pool | Done   |
| `Projectile` (single-hit trigger) | Done   |
| `IPoolable`                    | Done   |
| `EnemySpawner` (pool + reset)  | Done   |
| XP grant / drop logic          | **Next up** (inside HandleDeath) |
| Affix system (modifier stack)  | Not started (hook in place) |
| `Boss` (phases)                | Not started |

---

## Known TODOs / next decisions

- **Player progression system** — subscribe to `Enemy.OnAnyEnemyGrantedXP` once at
  startup to award XP to the killer. **Must unsubscribe in `OnDestroy`** (static event).
  Decide what null killer means (no XP, or award nearest player).
- **Loot/pickup system** — subscribe a listener to `Enemy.OnDropLoot` (per-spawner or
  global) to spawn pickups at the death position; use `affixEligible` for elite drops.
- **Projectile prefab setup** — Rigidbody **Is Kinematic = true**, Collider **Is Trigger**,
  hittable layers assigned. Without kinematic Rigidbody, `OnTriggerEnter` won't fire.
- **Affix system** — slots into the `EnemySpawner.Spawn` step-1 hook (before `OnSpawn`).
  When built, `Health.MaxHP` becomes a computed property over a modifier list and
  `SetMaxHP` becomes `AddModifier`. Affix components implement `IPoolable` to self-strip.
- **Shared projectile pool** — only if ranged-enemy counts get large; see RangedAttack note.
- **`Boss`** — override `Enemy.Awake` for phase data; otherwise an Enemy + components.
- **Animation gotcha** — `OnAttackHit` only fires if the attack clip has an Animation
  Event wired to it. No event = enemy decides to attack but never connects (not a bug).