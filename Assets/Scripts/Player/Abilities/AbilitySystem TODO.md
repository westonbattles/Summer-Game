# TODO

## Done (core)
- [x] Slot model — `AbilitySlot` enum (Primary / Secondary / Movement / Special).
- [x] Data layer — `AbilityDefinition` ScriptableObject (cooldown, charges, baseDamage, icon, effects list).
- [x] Composition seam — `AbilityEffect` abstract base; abilities are a list of effects.
- [x] Runtime state — `AbilityInstance` with the cooldown + charge model.
- [x] Activation context — `AbilityContext` + `BuildDamage()` as the single damage-assembly point.
- [x] Controller — `AbilityController` owns the 4 instances, ticks cooldowns, exposes `GetInstance(slot)` for UI.
- [x] Scope decided — player-only; enemies stay on `EnemyMeleeAttack` / `EnemyRangedAttack`.
- [x] File layout — plain types merged into `AbilityCore.cs`; `AbilityDefinition` and `AbilityController` kept solo (Unity-tracked). Three files total.

The core compiles and does nothing until the two seams in `AbilityController` are wired and at least one effect exists.

## 1. Input — `AbilityController.WasSlotPressed`
- [ ] Add four actions to the project-wide input asset: `Primary`, `Secondary`, `Movement`, `Special`.
- [ ] Expose them on `PlayerInputReader` like `Move` / `Look` / `JumpPressed` already are (use `WasPressedThisFrame()` for tap-to-fire).
- [ ] Return the matching pressed flag from `WasSlotPressed` (a `switch` on the slot).

## 2. Aim — `AbilityController.BuildContext`
- [ ] Set `origin` + `direction` from the real aim source. Third-person usually wants camera-through-crosshair via `PlayerLook` / the camera, not the placeholder `transform.forward`.
- [ ] Decide where projectiles originate (muzzle child vs. camera) and assign it to the `aimOrigin` field.

## 3. Effects — subclass `AbilityEffect`
- [ ] Write the concrete effects (e.g. `SpawnProjectileEffect`, `MeleeHitEffect`, a dash). Each reads `ctx` and acts; stays stateless.
- [ ] For anything that deals damage, call `ctx.BuildDamage(multiplier)` and pass the result on (e.g. into `Projectile.Launch`, or `IDamageable.TakeDamage`).
- [ ] For spawning/movement, add the handle the effect needs as a field on `AbilityContext` (a spawner, a motor), and set it in `BuildContext`.
  - Movement note: KCC owns velocity inside its own update, so a dash can't set velocity from outside — record the intent (e.g. via an interface on `Player`) and apply it inside `UpdateVelocity`.
  - Projectiles can start non-pooled (`Instantiate`/`Destroy`); `Projectile.Launch` already takes a release callback, so a pool can swap in later without changing effects.

## 4. Author + wire
- [ ] Create ability assets: Create → Abilities → Ability Definition. Set slot, cooldown, `baseDamage`, icon; add effects with the `+` on the `effects` list.
- [ ] Add `AbilityController` to the player and drag the four assets into its slots.

## Later
- [ ] Ability bar UI: read `GetInstance(slot)` for `CurrentCharges` + `RechargeProgress01`.