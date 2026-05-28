# Phase F — Gameplay loop + ZSkillRuntime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans.

**Goal:** Rewrite player/enemy/weapon gameplay loop on top of Luzart framework. ZSkill becomes MonoBehaviour child of Player. Reverse bridge so framework owns HP. Delete legacy GameManager + PlayerManager + EnemyManager + JoystickManager + DATNGameplayBridge + DATN.Legacy.UIManager.

**Status (2026-05-28 session):** Foundation scaffolds shipped (no behavior change, all flags off / dormant). Weapon ports + cutover deferred — they require play-test iteration that can't be batched safely.

**Spec:** `docs/superpowers/specs/2026-05-28-phase-f-gameplay-zskill-monobehaviour-design.md`

---

## What this session ships (Foundation)

These commits add new framework code alongside the live legacy stack. Nothing flips. Game plays identically.

### F.1 — Audit gameplay consumers
- Grep all readers of `GameManager`, `PlayerManager`, `EnemyManager`, `JoystickManager`, `ManagerEnemys`, `CameraController`.
- Document weapon → ZSkillBehavior mapping table in commit.
- Commit: `docs(F.1): audit gameplay legacy consumers`.

### F.2 — `ZSkillRuntime : MonoBehaviour` scaffold
- Create `Assets/_Main/Scripts/_LuzartGame/Skills/ZSkillRuntime.cs`:
  ```csharp
  using System.Collections.Generic;
  using UnityEngine;

  namespace Luzart
  {
      /// <summary>
      /// Survivor.io-style per-skill GameObject. Hosts a single ZSkillConfig and
      /// runs its IZSkillBehavior list every Update.
      ///
      /// Bind(owner, config) is called once after Instantiate. Behaviors are
      /// composed from ZSkillConfig.BehaviorConfigs (plain class instances).
      /// Phase F.7-F.18 will port the 12 legacy weapons into ZSkillBehavior_* impls.
      /// </summary>
      public class ZSkillRuntime : MonoBehaviour
      {
          [SerializeField] private ZSkillConfig _config;
          private IEntity _owner;
          private readonly List<IZSkillBehavior> _behaviors = new();
          private bool _bound;

          public ZSkillConfig Config => _config;
          public IEntity Owner => _owner;

          public void Bind(IEntity owner, ZSkillConfig config)
          {
              if (_bound) return;
              _owner = owner;
              _config = config;
              // Phase F.7+ wires real behaviors. For now an empty list is fine —
              // Update is a no-op until behaviors exist.
              _bound = true;
          }

          private void Update()
          {
              if (!_bound) return;
              float dt = Time.deltaTime;
              for (int i = 0; i < _behaviors.Count; i++) _behaviors[i]?.Update(dt);
          }

          private void OnDestroy()
          {
              for (int i = 0; i < _behaviors.Count; i++)
                  (_behaviors[i] as System.IDisposable)?.Dispose();
              _behaviors.Clear();
          }
      }
  }
  ```
- Compile clean.
- Commit: `migrate(F.2): scaffold ZSkillRuntime MonoBehaviour`.

### F.3 — `Player/Skills/` empty container in scene
- Open `GamePlay.unity`. Find Player GO.
- Create empty child GO `Skills` under Player.
- This is the parent for runtime ZSkillRuntime children spawned in Phase F.7+.
- Save scene.
- Commit: `migrate(F.3): add Player/Skills/ container GO`.

### F.4 — `LuzartPlayerController` scaffold (disabled)
- Create `Assets/_Main/Scripts/_LuzartGame/Gameplay/Player/LuzartPlayerController.cs`:
  ```csharp
  using UnityEngine;

  namespace Luzart
  {
      /// <summary>
      /// Replaces DATN's PlayerManager + JoystickManager for input + movement.
      /// Reads joystick (legacy movementJoystick.joystickVec) until F port replaces input.
      /// Drives Rigidbody2D velocity scaled by StatsBehavior.Get(Speed).
      ///
      /// Disabled until MigrationFlags.UseLuzartPlayerController is true (Phase F.x).
      /// </summary>
      [DisallowMultipleComponent]
      public class LuzartPlayerController : MonoBehaviour
      {
          [SerializeField] private Rigidbody2D _rb;
          [SerializeField] private Animator _animator;
          [SerializeField, Tooltip("Reference to scene movementJoystick component (legacy)")]
          private MonoBehaviour _legacyJoystick; // typed as MonoBehaviour to avoid hard ref

          private void Reset()
          {
              _rb = GetComponent<Rigidbody2D>();
          }

          private void OnEnable()
          {
              // Phase F.x — read joystick + framework Stats here.
          }
      }
  }
  ```
- Commit: `migrate(F.4): scaffold LuzartPlayerController (disabled)`.

### F.5 — `LuzartPlayerEntityRoot` scaffold (dormant)
- Create `Assets/_Main/Scripts/_LuzartGame/Gameplay/Player/LuzartPlayerEntityRoot.cs`:
  ```csharp
  using UnityEngine;

  namespace Luzart
  {
      /// <summary>
      /// Phase F replacement for DATNPlayerEntityAdapter. Will create a real
      /// PlayerCharacter from StatsConfig (no longer the "stats skipped" shim)
      /// and register it into Domain.
      ///
      /// Inert until MigrationFlags.UseLuzartPlayerEntityRoot is true.
      /// </summary>
      public class LuzartPlayerEntityRoot : AbstractMonoBehaviorContent
      {
          [SerializeField] private StatsConfig _statsConfig;

          // Phase F.x will override DoInject / DoUpdate to create + sync PlayerCharacter.
      }
  }
  ```
- Commit: `migrate(F.5): scaffold LuzartPlayerEntityRoot (dormant)`.

### F.6 — `LuzartEnemyEntityRoot` scaffold (dormant)
- Create `Assets/_Main/Scripts/_LuzartGame/Gameplay/EnemyCharacter/LuzartEnemyEntityRoot.cs`:
  ```csharp
  using UnityEngine;

  namespace Luzart
  {
      /// <summary>
      /// Phase F replacement for DATNEnemyEntityAdapter + legacy EnemyManager.
      /// On Awake: create EnemyCharacter from EnemyDefinition, register with
      /// framework EntityManager. AI/animation/HP/damage handled by framework
      /// Behaviors (EnemyAIBehavior, AnimationBehavior, CollisionHandlerBehavior).
      ///
      /// Inert until MigrationFlags.UseLuzartEnemyEntityRoot is true.
      /// </summary>
      public class LuzartEnemyEntityRoot : MonoBehaviour
      {
          [SerializeField] private EnemyDefinition _enemyDefinition;

          // Phase F.x adds Awake + OnDestroy + Update wiring.
      }
  }
  ```
- Commit: `migrate(F.6): scaffold LuzartEnemyEntityRoot (dormant)`.

### F.7 — MigrationFlags Phase F entries
- Edit `MigrationFlags.cs` to add 4 fields:
  ```csharp
  [Header("Phase F — Gameplay")]
  [Tooltip("Phase F.x — Switch player movement + input to LuzartPlayerController.")]
  public bool UseLuzartPlayerController = false;

  [Tooltip("Phase F.x — Activate LuzartPlayerEntityRoot (replaces DATNPlayerEntityAdapter).")]
  public bool UseLuzartPlayerEntityRoot = false;

  [Tooltip("Phase F.x — Switch Zombie prefab to LuzartEnemyEntityRoot.")]
  public bool UseLuzartEnemyEntityRoot = false;

  [Tooltip("Phase F.x — Framework StatsBehavior owns HP (reverse DATNGameplayBridge).")]
  public bool FrameworkOwnsPlayerHP = false;
  ```
- Commit: `migrate(F.7): add Phase F migration flags (all off)`.

### F.8 — Phase F foundation close-out
- Update wiki log.
- Commit: `migrate(F.8): Phase F foundation complete (game unchanged)`.

---

## Deferred to follow-up sessions (the actual work)

Each item requires play-test iteration; cannot be batched in autonomous execution:

### Player cutover (~3 commits)
- Attach LuzartPlayerController + LuzartPlayerEntityRoot to Player GO.
- Flip flags, verify movement + collision damage + HP HUD via play test.
- Disable legacy PlayerManager + JoystickManager + DATNPlayerEntityAdapter.
- Delete the 3 legacy MonoBehaviour types + Inspector refs.

### Weapon ports (1 commit per weapon × 12)
For each of 12 legacy weapons (Gun, SpinerA, SpinerB, Ball, Rocket, DroneA/B/C, BrikWall, FireGase, Aguel, ProtecteurGreen, SalsaRanshom):
1. Author its ZSkillConfig + 1+ ZSkillBehaviorConfig_* (Projectile/CreateProjectile/Stat/Bomb/Lighting/AddStat).
2. Wire visual prefab into BehaviorConfig (re-use existing weapon visual prefab).
3. Add to test starting-kit.
4. Verify damage + visual identical to legacy.
5. Disable legacy weapon spawner.
6. Delete legacy weapon script if no other weapon shares it.

### Bridge reversal (1 commit)
- Flip `FrameworkOwnsPlayerHP`.
- Remove `DATNGameplayBridge` MonoBehaviour. Delete `DATNGameplayBridge.cs`.
- `LuzartPlayerEntityRoot` fires `Data_ClassicEndGame` on HP=0 directly.

### Enemy cutover (~2 commits)
- Open Zombie prefab. Add LuzartEnemyEntityRoot. Flip flag.
- Verify spawn/chase/die/drop loop.
- Remove `EnemyManager` + `DATNEnemyEntityAdapter` from Zombie prefab.
- Delete `EnemyManager.cs` + `DATNEnemyEntityAdapter.cs`.

### Camera cutover (1 commit)
- Add `LuzartCameraController` to Camera GO. Resolves PlayerCharacter via Domain.
- Delete `CameraController.cs` + `ControllerSpawening.cs`.

### Legacy delete pass (~3 commits)
After all cutovers:
- Delete `GameManager.cs` + GO. Migrate remaining responsibilities to GameController.
- Delete `BooleanManager.cs` + Controller GO. Settings → SO.
- Delete `UIManager.cs` + `_LegacyManagers` subtree via Editor menu (add to PhaseDeleteTargets).
- Delete `SpriteWeapons.cs` (after all 12 weapon GOs are replaced).
- Delete the remaining 7 deferred-from-C scripts.

---

## Success criteria (end of Phase F)

- [ ] All Phase F flags default ON in `MigrationFlags.asset`.
- [ ] `find_gameobjects by_component` returns 0 for: `GameManager`, `PlayerManager`, `JoystickManager`, `ManagerEnemys`, `DATNPlayerEntityAdapter`, `DATNEnemyEntityAdapter`, `CameraController`, `ControllerSpawening`, `BooleanManager`, `DATNGameplayBridge`.
- [ ] `_LegacyManagers` GO + `UIManager.cs` deleted.
- [ ] `_LuzartGame/_LegacyCompat/` empty or deleted.
- [ ] Zombie prefab has `LuzartEnemyEntityRoot`, no `EnemyManager`.
- [ ] Player has `LuzartPlayerController` + `LuzartPlayerEntityRoot`, no legacy adapters.
- [ ] `Player/Skills/` instantiates at least 1 ZSkillRuntime when a skill is picked.
- [ ] Full play loop works: boot → menu → play → kill → level-up → die → lose screen → menu → re-play.
- [ ] `.wiki/wiki/overview.md` updated to drop "two parallel character hierarchies" note.
