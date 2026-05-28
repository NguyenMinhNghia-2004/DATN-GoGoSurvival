# Phase F — Gameplay loop + `ZSkillRuntime` MonoBehaviour

- **Status**: Draft, awaiting user approval
- **Parent**: `2026-05-28-luzart-migration-master-roadmap.md`
- **Created**: 2026-05-28
- **Risk**: High
- **Prerequisite**: Phase E complete

## 1. Outcome

After Phase F:

- **Player control**: input + movement + animation parameter setting + damage taking are all driven by framework components on the `Player` GameObject. `PlayerManager`, `JoystickManager`, `ManagerEnemys` deleted.
- **Player skills**: each active/passive skill is a child GameObject under `Player/Skills/` with a `ZSkillRuntime : MonoBehaviour` component. 12 weapons re-implemented as ZSkillBehavior MB components.
- **Enemy**: legacy `EnemyManager` script on Zombie prefab deleted. AI/animation/HP/damage handled by framework `EnemyAIBehavior`, `EnemyCollisionHandlerBehavior`, `AnimationBehavior`. `DATNEnemyEntityAdapter` (the bridge) is also gone — framework directly owns the entity.
- **Bridge reversal**: `DATNGameplayBridge` deleted. `StatsBehavior.Runtime_HP` is source-of-truth for HP. `GameController.CountEnemyDead` updated directly by framework enemy death callback.
- **End-game**: `SV_EndGameBridge` stays (it listens to `Data_ClassicEndGame` and shows NinjaUI screens — still relevant).
- **Camera**: `CameraController` legacy script is replaced by a framework `LuzartCameraController` that follows `Domain.Get<PlayerCharacter>().Transform.Position`.
- **Deleted code**: `GameManager.cs`, `PlayerManager.cs`, `EnemyManager.cs`, `JoystickManager.cs`, `movementJoystick.cs` (re-implemented), `ManagerEnemys.cs`, `CameraController.cs`, `ControllerSpawening.cs`, `DATNGameplayBridge.cs`, `DATNPlayerEntityAdapter.cs` (no longer needed — Player has direct framework component), `DATNEnemyEntityAdapter.cs`.
- **Visual prefabs untouched**: Body sprite + animator + DetecteurRoad GO + Weapon Point GO stay. Zombie prefab body + sprite + animator stay.

## 2. Architectural design

### 2.1 Player GameObject after Phase F

```
Player (GameObject, layer 0)
├── [Rigidbody2D, BoxCollider2D]            ← unchanged (visual collision)
├── [LuzartPlayerController]                ← NEW: replaces PlayerManager + JoystickManager
├── [LuzartPlayerEntityRoot]                ← NEW: replaces DATNPlayerEntityAdapter
├── Body  (SpriteRenderer + Animator)       ← unchanged (visual)
├── DetecteurRoad (BoxCollider2D)           ← unchanged
├── Weapon Point (Transform anchor)          ← unchanged
└── Skills/                                  ← NEW container
    ├── ZSkillRuntime_Kunai     [ZSkillRuntime + ZSkillBehavior_Projectile…]
    ├── ZSkillRuntime_Spinner   [ZSkillRuntime + ZSkillBehavior_CreateProjectile…]
    └── …
```

### 2.2 `LuzartPlayerController : MonoBehaviour`

- Inputs: reads NinjaUI Joystick broadcast (`JoystickBroadcastData`) — the joystick UI itself stays as a visual `Joystick Table` GO under `2_Hud` lane.
- Drives `Rigidbody2D.linearVelocity` from joystick vector × `StatsBehavior.Get(Speed)`.
- Sets Animator parameters (`IsMoving`, `MoveX`, `MoveY`) so existing animator state machine works.
- On `Rigidbody2D` collision with enemy → calls `_stats.TakeDamage(amount)`.

### 2.3 `LuzartPlayerEntityRoot : AbstractMonoBehaviorContent`

- Replaces `DATNPlayerEntityAdapter` + `DATNPlayerCharacter`.
- `Inject(domain)`: create framework `PlayerCharacter` from `StatsConfig` (use a real config now, not the skipped path) and register in Domain.
- `DoUpdate(dt)`: sync `transform.position → Stats.Transform.Position`. (Same as adapter today.)
- On `Stats.Runtime_HP` reaches 0 → fire `Data_ClassicEndGame { IsWin=false }` via Broadcaster.

### 2.4 `ZSkillRuntime : MonoBehaviour`

```csharp
namespace Luzart
{
    public class ZSkillRuntime : MonoBehaviour
    {
        [SerializeField] private ZSkillConfig _config;
        private IEntity _owner;                          // Player
        private readonly List<IZSkillBehavior> _behaviors = new();

        public void Bind(IEntity owner, ZSkillConfig config)
        {
            _owner = owner;
            _config = config;
            foreach (var behaviorConfig in config.BehaviorConfigs)
                _behaviors.Add(behaviorConfig.CreateBehavior(/* IZSkill view of this */));
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var b in _behaviors) b.Update(dt);
        }

        public void Upgrade(int level) { /* apply ZSkillUpgradeConfig modifiers */ }
    }
}
```

Authoring: `ZSkillConfig.CreateSkill(IEntity)` is replaced/augmented with `ZSkillConfig.SpawnRuntime(IEntity, Transform parent) → ZSkillRuntime`.

### 2.5 Decision: ZSkillBehavior remains plain class

Each `IZSkillBehavior` instance is owned by its `ZSkillRuntime`. Behaviors are not separate MonoBehaviour components — the runtime is the only MonoBehaviour, behaviors live inside its `_behaviors` list.

Rationale:
- Mixing MB-on-MB invites lifecycle ordering bugs.
- Behaviors are very small (often 1 method), MB overhead per behavior is unnecessary.
- Inspector still sees `ZSkillRuntime` with its config — debuggable enough.

### 2.6 Enemy GameObject after Phase F

```
Zombie  (prefab, runtime spawn)
├── [Rigidbody2D, BoxCollider2D, BoxCollider2D]   ← unchanged
├── [SpriteRenderer, Animator, AudioSource]       ← unchanged
├── [LuzartEnemyEntityRoot]                       ← NEW: replaces EnemyManager + DATNEnemyEntityAdapter
└── child visual GOs                              ← unchanged
```

`LuzartEnemyEntityRoot`:
- On Awake: instantiate `EnemyCharacter`, apply `EnemyDefinition` SO data, register with framework `EntityManager`.
- Hosts the framework Behaviors (added in code, not in Inspector): `EnemyAIBehavior` (chase player), `EnemyCollisionHandlerBehavior` (touch damage), `AnimationBehavior` (sync animator), `DropBehavior` (drop XP gem on death).
- On `Stats.Runtime_HP` reaches 0: trigger drop spawn + `Destroy(gameObject)`.

### 2.7 Camera

`LuzartCameraController : MonoBehaviour` — same behaviour as legacy `CameraController` but resolves target via `Domain.Get<PlayerCharacter>()` and reads its `Transform.Position`. Eliminates Inspector Player ref.

## 3. Slice plan

**Critical pattern**: each slice introduces a new component, dual-runs with the old, validates, then deletes the old. ~12 slices minimum (one per weapon + 5 systemic).

### Slice F.1 — Audit + spec lock

- Project-wide grep for all consumers of `GameManager`, `PlayerManager`, `EnemyManager`, `JoystickManager`, `ManagerEnemys`, `CameraController`.
- For each ZSkillBehavior subtype, identify which **legacy weapon** it should replace (`Kunai → BoltSHooter`, `Spinner → SpinerManager`, `Drone → DroneManager`, etc.).
- Update this spec §1–§2 with concrete mapping table.
- Commit (docs): `docs(F.1): audit gameplay legacy consumers`.

### Slice F.2 — `LuzartPlayerController` parallel scaffold

- Create `LuzartPlayerController.cs`.
- Attach to Player GameObject. Component is **inactive** (component disabled via `enabled = false`).
- Implements joystick read + Rigidbody2D velocity (read-only test mode: log expected velocity, don't write).
- Commit: `migrate(F.2): scaffold LuzartPlayerController (disabled)`.

### Slice F.3 — `LuzartPlayerEntityRoot` parallel scaffold

- Create the class. Add to Player GameObject alongside `DATNPlayerEntityAdapter`.
- `Inject`: do not register into Domain yet (`MigrationFlags.UseLuzartPlayerRoot` flag, default false).
- Commit: `migrate(F.3): scaffold LuzartPlayerEntityRoot (flag off)`.

### Slice F.4 — Cutover player input + entity root

- Flip flag on. Disable legacy `PlayerManager.enabled` and `JoystickManager.enabled` and `DATNPlayerEntityAdapter.enabled` at runtime (do not delete yet).
- `LuzartPlayerController.enabled = true`, `LuzartPlayerEntityRoot` registers PlayerCharacter.
- Play-test: movement, collision damage, HP HUD all work via new path.
- Commit: `migrate(F.4): cutover player to Luzart components`.

### Slice F.5 — Delete legacy player components

- Remove `PlayerManager`, `JoystickManager`, `ManagerEnemys` (on Player), `DATNPlayerEntityAdapter` MonoBehaviours from Player GO.
- Delete the `.cs` files.
- Commit: `migrate(F.5): delete legacy player MonoBehaviours`.

### Slice F.6 — `Player/Skills/` container + ZSkillRuntime template

- Create child GO `Player/Skills/` (empty container).
- Implement `ZSkillRuntime.cs`.
- Hook: when a `ZSkillConfig` is awarded (level-up pick), `UpgradeSkillManager.UpgradeSkill` instantiates `ZSkillRuntime` GO under `Player/Skills/`.
- For now, no behaviors actually fire — confirm GO is created and `Update` is called.
- Commit: `migrate(F.6): ZSkillRuntime + Skills/ container`.

### Slices F.7 — F.18 — Port 12 weapons (one slice each)

For each of the 12 legacy weapons (`Gun`, `SpinerA`, `SpinerB`, `Ball`, `Rocket`, `DroneA`, `DroneB`, `DroneC`, `BrikWall`, `FireGase`, `Aguel`, `ProtecteurGreen`, `SalsaRanshom`):

Slice template:
1. Identify the matching `ZSk_*.asset` + its `ZSkillBehaviorConfig_*` (e.g., Projectile / CreateProjectile / Stat).
2. Verify the behavior config has correct prefab refs (or author them — point at the **same visual prefab** the legacy weapon uses).
3. Optionally toggle a `MigrationFlags.UseFramework_<WeaponName>` flag.
4. In-game: equip a starting kit that triggers this skill at level 1. Confirm framework spawns the visual prefab and damages enemies.
5. Verify legacy weapon path (still active via `BoltSHooter` etc. on Player prefab) does NOT also fire — disable the legacy weapon spawner once framework verified.
6. Delete the legacy script if no other weapon shares it.

After 12 slices, all 12 weapons run on framework. Slices: `migrate(F.7): port weapon Gun` ... `migrate(F.18): port weapon ProtecteurGreen`.

### Slice F.19 — Bridge reversal: framework owns HP

- Remove `DATNGameplayBridge` MonoBehaviour from `_GameBoot`.
- Delete `DATNGameplayBridge.cs`.
- `LuzartPlayerEntityRoot.OnDestroy/OnDeath` fires `Data_ClassicEndGame` directly (the bridge previously did this).
- Verify HUD HP bar still ticks (it should — it's already subscribed to `Stats.Runtime_HP`).
- Commit: `migrate(F.19): delete DATNGameplayBridge, framework owns HP`.

### Slice F.20 — Enemy port: `LuzartEnemyEntityRoot`

- On Zombie prefab: add `LuzartEnemyEntityRoot` alongside existing `EnemyManager` + `DATNEnemyEntityAdapter`.
- Dual-run with flag.
- Cutover: framework owns enemy HP + AI + collision damage.
- Verify enemies still spawn, chase, die, drop XP.
- Commit: `migrate(F.20): cutover Zombie to Luzart entity root`.

### Slice F.21 — Delete legacy enemy code

- Remove `EnemyManager` + `DATNEnemyEntityAdapter` from Zombie prefab.
- Delete `EnemyManager.cs` (the file at `Enemy/EnemyManager.cs`).
- Delete `DATNEnemyEntityAdapter.cs`.
- Commit: `migrate(F.21): delete legacy enemy MonoBehaviours`.

### Slice F.22 — Camera port

- Add `LuzartCameraController` to Camera GO alongside `CameraController`.
- Cutover with flag.
- Delete `CameraController.cs` + `ControllerSpawening.cs`.
- Commit: `migrate(F.22): cutover camera follow to framework`.

### Slice F.23 — Delete `GameManager`

- After F.5 (player) + F.21 (enemy) + F.19 (HP), `GameManager` should only hold currency/UI-trigger refs.
- Migrate any remaining state into `GameController` or the appropriate framework owner.
- Remove `GameManager` MonoBehaviour from scene.
- Delete `GameManager.cs`.
- Commit: `migrate(F.23): delete GameManager`.

### Slice F.24 — Delete `BooleanManager` (settings)

- `BooleanManager` holds Music/Sound/Vibration + GameStart flag.
- Settings → framework `SettingsContent : AbstractScriptableContent` SO, persisted via `DataManager`.
- GameStart → `GameController.IsRunning`.
- Update `SV_SettingsPopupUI`, `AudioManager` to read new settings source.
- Remove `BooleanManager` from `Controller` GO. Delete script.
- Commit: `migrate(F.24): delete BooleanManager, framework owns settings`.

### Slice F.25 — Phase F close-out

- Verify all success criteria (§4).
- Update `.wiki/wiki/log.md`.
- Tag the commit `migration-complete`.
- Commit: `migrate(F.25): Phase F + migration complete`.

## 4. Success criteria

- [ ] `find_gameobjects by_component` returns 0 for: `GameManager`, `PlayerManager`, `JoystickManager`, `ManagerEnemys`, `DATNPlayerEntityAdapter`, `DATNEnemyEntityAdapter`, `CameraController`, `ControllerSpawening`, `BooleanManager`.
- [ ] `Grep "GameManager\|PlayerManager\|EnemyManager"` in `.cs` files returns only framework references (`GameController` is OK, framework `EnemyManager` IContent is OK).
- [ ] Zombie prefab root has `LuzartEnemyEntityRoot`, no `EnemyManager` + no adapter.
- [ ] Player GameObject has `LuzartPlayerController` + `LuzartPlayerEntityRoot`, no legacy adapter, no PlayerManager/JoystickManager.
- [ ] `Player/Skills/` container exists; at least 1 ZSkillRuntime instantiates at runtime when a skill is picked.
- [ ] Full play loop works: boot → menu → play → kill 10 enemies via framework weapons → level up → pick skill → see new ZSkillRuntime spawn → die → lose screen → menu loop.
- [ ] `.wiki/wiki/overview.md` updated to drop the "two parallel character hierarchies" note (it's no longer true).

## 5. Out of scope

- Authoring real `ZSkillUpgradeConfig` per-level stat data for all 22 skills. Hooks exist; content is post-migration.
- Object Pooling.
- UGS Cloud Save.

## 6. Risks

| Risk | Mitigation |
|---|---|
| Joystick input migration breaks input routing (NinjaUI lane vs world Canvas) | Slice F.2 includes verifying `JoystickBroadcastData` actually fires from the joystick prefab; if not, add `JoystickEmitter` script on the visual Joystick Table |
| Animation parameter names differ between legacy `JoystickManager` and `LuzartPlayerController` | Mirror parameter names verbatim — Slice F.2 audits the Animator controller asset and matches strings |
| Bridge reversal (F.19) leaves the HUD reading stale HP for one frame | Cutover in a single commit, frame-tested. Acceptable transient if next frame syncs |
| Enemy spawn currently uses framework `EnemySpawnerManager` which still expects `DATNEnemyEntityAdapter` registration | Slice F.20 updates the spawner to register via `LuzartEnemyEntityRoot` instead |
| 12 weapon ports take longer than estimated, each with prefab edits | Slices F.7–F.18 can be deferred/batched; each weapon is independent, so partial progress is acceptable |
| `ZSkillConfig.CreateSkill` signature change breaks compile if other consumers exist | Audit in F.6; introduce `SpawnRuntime` as a new method, keep old `CreateSkill` until all callers ported |
| Deleting `GameManager` breaks an Inspector field on a prefab not yet audited | F.23 mandates a project-wide GUID grep before delete |

## 7. Decisions

- **ZSkillBehavior stays plain class** — only `ZSkillRuntime` is MB. Decision documented in §2.5.
- **Visual prefabs frozen** — re-use existing weapon visual prefabs as projectile prefabs for ZSkillBehavior_Projectile authoring.
- **Joystick broadcast pattern** — use `JoystickBroadcastData` (already a stub in `_FrameworkStubs.cs`). Realized in this phase.
- **`GameController` absorbs all remaining `GameManager` responsibility** — does not split into multiple new controllers.

## 8. Open items resolved before execution

- Concrete weapon → ZSkillBehavior mapping (12 entries). Authored in Slice F.1.
- Whether `EnemySpawnerManager` registers via `LuzartEnemyEntityRoot` or via `DATNEnemyEntityAdapter` until F.20. Decided in F.20 audit.
- `Domain.Get<PlayerCharacter>()` race in F.4 vs Camera follow: Camera waits 1 frame after F.4 cutover.
