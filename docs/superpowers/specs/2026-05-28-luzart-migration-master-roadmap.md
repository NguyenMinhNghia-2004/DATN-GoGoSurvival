# Luzart Migration — Master Roadmap

- **Status**: Draft, awaiting user approval
- **Owner**: solo dev (DATN thesis)
- **Created**: 2026-05-28
- **Active scene**: `Assets/_Main/Scenes/GamePlay.unity` (only scene in build)
- **Sibling specs**: `2026-05-28-phase-{c,d,e,f}-*-design.md`

## 1. Context

The project runs a **dual-stack** architecture:

- **Legacy DATN code** (no namespace + `DATN.Legacy`) drives gameplay state today: HP, kills, currency, player input, weapon spawning, enemy AI/animation.
- **Luzart framework** (`Assets/_Main/Scripts/_LuzartGame/`) was added as a parallel runtime: `Domain` + `IContent` registry, `EntityBase` + `Behavior` composition, ScriptableObject configs (`ZSkillConfig`, `StatsConfig`, `EnemyDefinition`…). Per `CLAUDE.md` this was tagged "third-party" but the user confirms it is their own thesis code.
- Five bridges keep the two stacks in sync (`DATNPlayerEntityAdapter`, `DATNEnemyEntityAdapter`, `DATNGameplayBridge`, `SV_EndGameBridge`, `SV_LegacyUIBase`).

Live `GamePlay.unity` (verified via Unity MCP `find_gameobjects` on 2026-05-28) holds **12 root GameObjects** and only **~14 active legacy components**. Many older scripts (`SpawenManager`, `GunManager`, `RocketManager`, `DroneManager`, `MainMenu`, `SelectMapManager`, etc.) exist as files but are **not attached** to any scene or prefab GameObject.

## 2. Goal

Migrate **all behaviour/logic** to the Luzart framework. Legacy code stays only where it owns **visual** representation:

- ✅ Keep: sprite, Animator controller, particles, VFX child GameObjects
- ✅ Keep: prefab GameObject hierarchy (Body, DetecteurRoad, Weapon Point, child structure)
- ✅ Keep: UI prefab canvas + button + image + text layout
- ❌ Replace: all C# logic — input, damage, HP, kill counting, weapon firing, enemy AI, UI button onClick handlers, currency, equipment apply, audio gating
- ❌ Replace: data containers that are not pure visual data (`SpriteWeapons`, `LevelsManager`) → ScriptableObjects

The migration must be **zero-downtime**: every commit must leave the game in a playable state (HP correct, enemies spawning, killable, UI navigable).

## 3. Architectural decisions

### 3.1 ZSkillRuntime as MonoBehaviour (Survivor.io style)

Confirmed shape:

```
Player (GameObject)
└── Skills/                          ← new child container
    ├── ZSkillRuntime_Kunai (GO)    [ZSkillRuntime]
    ├── ZSkillRuntime_Boomerang     [ZSkillRuntime]
    └── …
```

- `ZSkillConfig` (SO) stays as authored data.
- `ZSkillRuntime : MonoBehaviour` replaces the plain-class `ZSkill`. Lives on a child GameObject of Player. Drives cooldown, target acquisition, spawn-projectile, stat application.
- `ZSkillBehavior_*` either stays as plain class composed into `ZSkillRuntime`, or becomes a sibling component. **Pick during Phase F design**.
- Visible in Inspector at runtime → improves debuggability and matches Survivor.io mental model where each weapon is "a thing in scene".

### 3.2 Strangler-fig with feature flags

Each slice follows the pattern:

```
Step 1 (commit): Add new code alongside old — game uses old, new is dormant
Step 2 (commit): Introduce feature flag bool (default off)
Step 3 (commit): Flag on for dev test, fix bugs
Step 4 (commit): Default flag on, old becomes dead path
Step 5 (commit): Delete old code + flag
```

Feature flag location: a `MigrationFlags` ScriptableObject under `Assets/_Main/Data/Migration/MigrationFlags.asset` (created in Phase C, alongside a `MigrationFlagsContent : AbstractScriptableContent` wrapper for Domain access). Inspector-editable bool fields → runtime-toggleable during play-test without recompile. Each flag has a tooltip with the spec/issue it tracks.

Reason: enables true zero-downtime — bugs in new code don't crash the game, dev can toggle back via Inspector at runtime without restarting Unity.

### 3.3 Bridge direction reversal (Phase F core)

Today: `GameManager.Health` is source-of-truth, `DATNGameplayBridge` mirrors it to `StatsBehavior.Runtime_HP`.

After Phase F: `StatsBehavior.Runtime_HP` is source-of-truth. Legacy `GameManager.Health` (if it still exists) reads from `StatsBehavior`. Eventually `GameManager` is deleted entirely.

Reversal step is gated by feature flag `MigrationFlags.FrameworkOwnsPlayerHP`. Cutover happens in a single commit with both directions tested.

### 3.4 Save data compatibility

PlayerPrefs schema (Coins, Gems, Equipment JSON) **stays compatible** during the migration. If a schema field needs to change in Phase E, write a one-shot migration routine in `DataManager.Awake` that reads old key → writes new key → deletes old key.

Rationale: solo dev test data is currently the only data, but keeping migration code disciplined helps the thesis defense narrative.

### 3.5 Visual prefabs are frozen

No edits to: `Body` SpriteRenderer settings, Animator state machines, particle FX child GameObjects, UI Canvas layout (RectTransform, Image, Text positions). Only **add new MonoBehaviour components** to existing GameObjects, or **add new child GameObjects** under existing parents.

If a visual change is strictly required (e.g., wiring a new button), document it in the phase spec.

## 4. Phase summary

| Phase | Title | Outcome | Risk | Slice count (est.) |
|---|---|---|---|---|
| **C** | Dead code + prefab vỏ trống | ~13 script files deleted; 4 UI prefabs wrapped (`SV_Equipement/Process/Evolve/Mails`); 3 GameObjects deleted (`AdsManager`, `Enverement`, `_LegacyManagers/GamePlay`) | Low | 4–5 |
| **D** | Remove `DATN.Legacy.UIManager` | `WeaponCatalog` + `LevelCatalog` SOs created; callers migrated; `_LegacyUIScripts` GameObject deleted; `UIManager.cs` (legacy) deleted | Medium | 6–8 |
| **E** | Remove `PlayerStats` singleton + `SkillData` stub | `EquipmentManager.ApplyTo` writes to `StatsBehavior`; `EquipmentData.linkedStartingSkill` migrated to `ZSkillConfig` via AssetPostprocessor; `PlayerStats.cs` + `SkillData.cs` deleted; `PassiveStatType` mapped to framework `StatType` | Medium-High | 5–7 |
| **F** | Gameplay loop + `ZSkillRuntime` MonoBehaviour | `ZSkillRuntime : MonoBehaviour`; 12 weapons re-implemented as ZSkillBehavior MB components; reverse bridge direction (StatsBehavior owns HP); delete `GameManager`, `PlayerManager`, `EnemyManager`, `JoystickManager`, `ManagerEnemys`, `DATNGameplayBridge` | High | 12+ (one per weapon + 4 systemic) |

See per-phase specs for slice-by-slice plans.

## 5. Cross-phase concerns

### 5.1 Conventions

- **Namespace**: all new code in `Luzart` namespace (matches existing framework). Phase-specific subnamespaces allowed: `Luzart.Migration`, `Luzart.Weapons`, etc.
- **File location**: new MonoBehaviours under `Assets/_Main/Scripts/_LuzartGame/` to match the framework root. New SOs under `Assets/_Main/Data/<Category>/`.
- **Commit prefix**: `migrate(<phase>): <slice description>` e.g. `migrate(D): introduce WeaponCatalog SO`.

### 5.2 Verification per commit

Required before claiming a commit is "playable":

1. Compile clean (`mcpforunity://editor/state` → `is_compiling == false`, no errors in console).
2. Enter Play mode, observe:
   - Splash → MainMenu shows
   - Click Play → enters gameplay
   - Player movable, enemies spawn, killable
   - XP/HP HUD updates
   - Level-up popup triggers
3. Exit Play mode, no errors logged.

Optional but recommended: capture a Game-view screenshot via `manage_camera(action="screenshot")` and diff against a baseline taken at start of phase.

### 5.3 Tests

Project has **no automated tests** today. We do not add unit tests as part of migration — that is out of scope for the thesis and would explode the work. Manual play-test acceptance per slice is the verification gate.

(If the user wants tests in the future, they get added as a separate spec — not bundled here.)

### 5.4 Rollback

Each slice is one commit. If a slice breaks the game, `git revert <sha>` reverts cleanly. Because feature flags default off in early commits of a slice, a partial slice can be left in-tree without affecting gameplay.

### 5.5 Backups before destructive steps

Before any commit that deletes a GameObject from the scene or a `.cs` file that is not provably orphaned by `find_gameobjects=0` + grep, take:

1. Git commit of current state (so revert is possible).
2. A Scene-View screenshot of the deletion target for the record.

## 6. Out of scope

- Adding automated tests.
- Adding Addressables (current `DirectPrefabUIAssetProvider` stays).
- Porting Shop / Equipment / Evolve UI from legacy prefab to NinjaUI-native — wrappers added in Phase C are enough.
- Implementing actual `ZSkillUpgradeConfig` per-level stat data (open question q-20260516-03). Phase F will leave hooks but not author content.
- UGS Cloud Save migration (still Phase 1 PlayerPrefs).
- Object Pooling (`decisions/object-pooling-priority`). Pooling lands AFTER migration completes.

## 7. Success criteria (end of Phase F)

- [ ] `find_gameobjects search_method=by_component` returns **0** for: `GameManager`, `PlayerManager`, `EnemyManager` (the legacy script — adapter on Zombie stays), `JoystickManager`, `ManagerEnemys`, `BooleanManager` (replaced by framework settings), `SpriteWeapons`, `LevelsManager`, `DATN.Legacy.UIManager`, `PlayerStats`, `DATNGameplayBridge`.
- [ ] `Assets/_Main/Scripts/_LegacyCompat/` is empty or deleted.
- [ ] `_LuzartGame/Gameplay/System/` contains the new owners: `LuzartPlayerController`, `LuzartEnemyController` (or similar names — TBD in Phase F).
- [ ] Each ZSkill in `SV_SkillCatalog` has a runnable `ZSkillRuntime` component path; level-up actually upgrades stats (closes q-20260516-03 partially).
- [ ] Final scene has ≤ 8 root GameObjects, no `_LegacyManagers` root.
- [ ] Single play-test session: boot → main menu → play → kill 10 enemies → level up → pick skill → die → see lose screen → main menu → re-play. All paths work.

## 8. Risks & mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Inspector references break silently when a legacy MonoBehaviour is removed | High | Before delete, search prefab + scene SerializeField refs via `find_in_file` over `.prefab` + `.unity` for the script GUID |
| Reverse bridge cutover (Phase F) introduces frame-lag on HP | Medium | Cutover commit must include both old + new HP sync until verified, then remove old |
| `EquipmentData.linkedStartingSkill` field type change loses references (Phase E) | Medium | Use AssetPostprocessor to migrate; verify all 26 `Eq_*` SO assets reference resolves before deleting `SkillData.cs` |
| ZSkillRuntime MB design (Phase F) clashes with existing plain-class `ZSkill` API consumers | Medium | Audit all `IZSkill` / `ZSkill` references in Phase F slice 1 before refactor |
| Visual freeze rule violated accidentally | Low | Commit message lints; review own diff for `.prefab` + `.unity` edits outside expected scope |

## 9. Open questions deferred to phase specs

- (Phase D) `LevelsManager` keeps Inspector ref to Level1 prefab — does `LevelCatalog` SO use direct prefab refs or Addressables? **Default: direct refs to keep with current pattern**.
- (Phase E) Does `PassiveStatType` enum get deleted (mapped 1:1 to framework `StatType`) or kept as a thin adapter enum? **Default: delete, map at boundary**.
- (Phase F) `ZSkillBehavior_*` plain class vs separate MonoBehaviour component children — picked in Phase F design.

These are minor; phase specs settle them.
