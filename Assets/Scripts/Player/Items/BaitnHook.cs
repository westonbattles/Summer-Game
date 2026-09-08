using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class BaitnHook : ItemBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer hookLinePrefab;
    [SerializeField] private SoundEvent hookLatchSound;
    [SerializeField] private SoundEvent hookPullingSound;

    [Header("Settings")]
    //[SerializeField] private float baseGripChance = 0.05f;      // 5% per item stack
    [SerializeField] private float pullSpeed = 12f;
    [SerializeField] private float pullDuration = 1.5f;
    [SerializeField] private float stopDistance = 1.5f;
    //[SerializeField] private float cooldownPerEnemy = 2.0f;     // Minimum seconds between hooks on the same enemy
    [SerializeField] private float pullSoundInterval = 0.2f;
    [SerializeField] private float primedDamageMultiplier = 5f;
    [SerializeField] private float primedWindow = 1f;
    [SerializeField] private float hookStartDelay = 0.40f;

    [Header("Item Definition")]
    [SerializeField] private ItemDefinition itemDefinition;


    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        // none.
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new BaitnHookRuntime(player, this, 1);
    }

    private class BaitnHookRuntime : ItemRuntime
    {
        private readonly Player player;
        private readonly BaitnHook data;
        private readonly Combat playerCombat;

        private readonly Dictionary<Enemy, float> primedUntil = new();

        private readonly ItemInventory playerInventory;
        private int currentItemCount = 1;

        private readonly Dictionary<Enemy, float> enemyCooldowns = new Dictionary<Enemy, float>();

        public BaitnHookRuntime(Player player, BaitnHook data, int initialCount = 1)
        {
            this.player = player;
            this.data = data;
            this.currentItemCount = initialCount;

            if (player != null)
            {
                playerCombat = player.GetComponent<Combat>();
                if (playerCombat != null)
                {
                    playerCombat.AfterDamageDealt += OnAfterDamageDealt;
                    playerCombat.BeforeDamageDealt += OnBeforeDamageDealt;
                }
                else
                {
                    Debug.LogWarning("[BaitnHook] - Player combat component missing!");
                }

                playerInventory = player.GetComponent<ItemInventory>();
                if (playerInventory != null)
                {
                    playerInventory.OnItemCountChanged += OnItemCountChanged;
                }
                else
                {
                    Debug.LogWarning("[BaitnHook] - Player inventory component missing!");
                }
            }

        }

        private void OnItemCountChanged(ItemDefinition def, int newCount)
        {
            if (data.itemDefinition != null && def == data.itemDefinition)
            {
                currentItemCount = newCount;
            }
        }
        
        // saving this here incase we lose the ability to revert cause this is the old method.
        //    private void OnAfterDamageDealt(Hit hit)
        //    {
        //        if (hit.Target == null) return;

        //        Enemy targetEnemy = hit.Target as Enemy;
        //        if (targetEnemy == null && hit.Target is Component targetComponent)
        //        {
        //            targetEnemy = targetComponent.GetComponentInParent<Enemy>();
        //        }

        //        if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy) return;
        //        if (data.hookLinePrefab == null) return;

        //        if (enemyCooldowns.TryGetValue(targetEnemy, out float lastHookTime))
        //        {
        //            if (Time.time - lastHookTime < data.cooldownPerEnemy)
        //            {
        //                return; // still on cooldown for this enemy
        //            }
        //        }

        //        // roll
        //        float chance = Mathf.Clamp01(data.baseGripChance * currentItemCount);
        //        if (UnityEngine.Random.value > chance) return;

        //        // record cooldown timestamp
        //        enemyCooldowns[targetEnemy] = Time.time;

        //        // If enemy is already hooked, cancel previous hook to prevent stack overload
        //        bool isAlreadyHooked = HookPullEffect.ActiveHooks.TryGetValue(targetEnemy, out var existingEffect);
        //        if (isAlreadyHooked)
        //        {
        //            existingEffect.Cancel();
        //        }

        //        LineRenderer lineInstance = UnityEngine.Object.Instantiate(data.hookLinePrefab);

        //        HookPullEffect.Create(
        //            player,
        //            targetEnemy,
        //            lineInstance,
        //            data.pullSpeed,
        //            data.pullDuration,
        //            data.stopDistance,
        //            data.hookLatchSound,
        //            data.hookPullingSound,
        //            data.pullSoundInterval,
        //            !isAlreadyHooked
        //        );
        //    }

        public override void Dispose()
        {
            if (playerCombat != null)
            {
                playerCombat.AfterDamageDealt -= OnAfterDamageDealt;
                playerCombat.BeforeDamageDealt -= OnBeforeDamageDealt;
            }

            if (playerInventory != null)
            {
                playerInventory.OnItemCountChanged -= OnItemCountChanged;
            }

            enemyCooldowns.Clear();
            base.Dispose();
        }

        private void OnBeforeDamageDealt(ref Hit hit)
        {
            Enemy enemy = ResolveEnemy(hit.Target);
            if (enemy == null) return;
            if (!primedUntil.TryGetValue(enemy, out float expires)) return;

            primedUntil.Remove(enemy);
            if (Time.time > expires) return;

            hit.Info.Amount *= data.primedDamageMultiplier;
        }

        private void OnAfterDamageDealt(Hit hit)
        {
            if (hit.Info.Ability is not PsychicPulse) return;

            Enemy enemy = ResolveEnemy(hit.Target);
            if (enemy == null || !enemy.gameObject.activeInHierarchy) return;
            if (data.hookLinePrefab == null) return;

            if (HookPullEffect.ActiveHooks.TryGetValue(enemy, out var existing))
                existing.Cancel();

            LineRenderer line = UnityEngine.Object.Instantiate(data.hookLinePrefab);
            HookPullEffect hook = HookPullEffect.Create(
                player, enemy, line,
                data.pullSpeed, data.pullDuration, data.stopDistance,
                data.hookLatchSound, data.hookPullingSound,
                soundInterval: data.pullSoundInterval,
                playLatchSound: true,
                startDelay: data.hookStartDelay
                );

            hook.OnCompleted = e => primedUntil[e] = Time.time + data.primedWindow;
        }
    }

    private static Enemy ResolveEnemy(IDamageable t)
    {
        if (t is Enemy e) return e;
        if (t is Component c) return c.GetComponentInParent<Enemy>();
        return null;
    }

    public class HookPullEffect : MonoBehaviour
    {
        public static readonly Dictionary<Enemy, HookPullEffect> ActiveHooks = new Dictionary<Enemy, HookPullEffect>();

        private Player player;
        private Enemy targetEnemy;
        private NavMeshAgent navAgent;
        private LineRenderer line;

        private float pullSpeed;
        private float maxDuration;
        private float stopDistance;
        private SoundEvent pullingSound;
        private float pullSoundInterval;
        private float pullSoundTimer;

        private readonly List<SoundHandle> activeSoundHandles = new List<SoundHandle>();

        private float timer;
        private bool wasAgentEnabled;
        private bool isCancelled;

        public Action<Enemy> OnCompleted;
        private float startDelay;

        // creates a gameobject and hooks the parameters at runtime
        public static HookPullEffect Create(
            Player player,
            Enemy enemy,
            LineRenderer lineInstance,
            float speed,
            float duration,
            float stopDist,
            SoundEvent latchSound,
            SoundEvent pullSound,
            float soundInterval = 0.2f,
            bool playLatchSound = true,
            float startDelay = 0f)
        {
            var runner = new GameObject($"HookEffect_{enemy.name}").AddComponent<HookPullEffect>();
            runner.Initialize(player, enemy, lineInstance,
                speed, duration, stopDist, latchSound, pullSound, soundInterval, playLatchSound, startDelay);
            return runner;
        }

        private void Initialize(
            Player player,
            Enemy enemy,
            LineRenderer lineInstance,
            float speed,
            float duration,
            float stopDist,
            SoundEvent latchSound,
            SoundEvent pullSound,
            float soundInterval,
            bool playLatchSound,
            float startDelay)
        {
            this.player = player;
            this.targetEnemy = enemy;
            this.line = lineInstance;
            this.pullSpeed = speed;
            this.maxDuration = duration;
            this.stopDistance = stopDist;
            this.pullingSound = pullSound;
            this.pullSoundInterval = soundInterval;
            this.startDelay = startDelay;

            if (targetEnemy != null)
            {
                ActiveHooks[targetEnemy] = this;
                navAgent = targetEnemy.GetComponent<NavMeshAgent>();

                targetEnemy.OnDied += HandleEnemyDied;

                // Pause NavMeshAgent pathfinding so it doesn't fight the physical pull translation
                if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                {
                    wasAgentEnabled = true;
                    navAgent.isStopped = true;
                    navAgent.updatePosition = false;
                }

                // only if its an active hook
                if (playLatchSound && latchSound != null)
                {
                    var handle = latchSound.PlayAttached(targetEnemy.transform);
                    if (handle.IsPlaying) activeSoundHandles.Add(handle);
                }
            }

            if (line != null)
            {
                line.positionCount = 2;
                line.enabled = true;
            }
        }

        private void HandleEnemyDied(Enemy enemy)
        {
            Cancel();
        }

        public void Cancel()
        {
            isCancelled = true;
            DestroyEffect();
        }

        private void Update()
        {
            timer += Time.deltaTime;

            // safety check & timeout condition
            if (isCancelled || targetEnemy == null || player == null || !targetEnemy.gameObject.activeInHierarchy || timer >= maxDuration)
            {
                DestroyEffect();
                return;
            }

            if (timer < startDelay) return;

            Vector3 startPos = player.transform.position;
            Vector3 targetPos = targetEnemy.GetCenterPoint();

            // update line renderer endpoints
            if (line != null)
            {
                line.SetPosition(0, startPos);
                line.SetPosition(1, targetPos);
            }

            // maybe normalize this incase we have vector bugs
            Vector3 direction = (startPos - targetEnemy.transform.position);
            float distance = direction.magnitude;

            if (distance > stopDistance)
            {
                Vector3 moveStep = direction.normalized * pullSpeed * Time.deltaTime;
                targetEnemy.transform.position += moveStep;

                // keeps the nav agent in sync with the manual movement
                if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                {
                    navAgent.nextPosition = targetEnemy.transform.position;
                }

                pullSoundTimer += Time.deltaTime;
                if (pullSoundTimer >= pullSoundInterval)
                {
                    pullSoundTimer = 0f;
                    if (pullingSound != null)
                    {
                        // track audio handle attached to the enemy so we can stop it when the effect ends
                        var handle = pullingSound.PlayAttached(targetEnemy.transform);
                        if (handle.IsPlaying)
                        {
                            activeSoundHandles.Add(handle);
                        }
                    }
                }
            }
            else
            {
                // stop pulling once we're close enough
                DestroyEffect();
            }
        }

        // Debating tossing this and refactoring the SoundEvent system to handle this automatically, but this works for now.
        private void StopAllSounds(float fadeTime = 0.05f)
        {
            for (int i = 0; i < activeSoundHandles.Count; i++)
            {
                if (activeSoundHandles[i].IsPlaying)
                {
                    activeSoundHandles[i].Stop(fadeTime);
                }
            }
            activeSoundHandles.Clear();
        }

        private void DestroyEffect()
        {
            StopAllSounds(0.05f);

            if (targetEnemy != null)
            {
                targetEnemy.OnDied -= HandleEnemyDied;

                if (ActiveHooks.TryGetValue(targetEnemy, out var active) && active == this)
                {
                    ActiveHooks.Remove(targetEnemy);
                }
            }

            // Restore NavMeshAgent state
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh && wasAgentEnabled)
            {
                navAgent.nextPosition = targetEnemy.transform.position;
                navAgent.updatePosition = true;
                navAgent.isStopped = false;
            }

            if (!isCancelled && targetEnemy != null) OnCompleted?.Invoke(targetEnemy);

            if (line != null)
            {
                Destroy(line.gameObject);
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            StopAllSounds(0f);

            if (targetEnemy != null)
            {
                targetEnemy.OnDied -= HandleEnemyDied;

                if (ActiveHooks.TryGetValue(targetEnemy, out var active) && active == this)
                {
                    ActiveHooks.Remove(targetEnemy);
                }
            }
        }
    }
}