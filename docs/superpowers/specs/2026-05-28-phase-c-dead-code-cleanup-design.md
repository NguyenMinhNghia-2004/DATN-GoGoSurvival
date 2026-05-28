# Phase C — Dead code cleanup + empty UI prefab wrappers

- **Status**: Draft, awaiting user approval
- **Parent**: `2026-05-28-luzart-migration-master-roadmap.md`
- **Created**: 2026-05-28
- **Risk**: Low
- **Prerequisite**: none — first phase

## 1. Outcome

After Phase C:

- ~13 C# files removed from the project — all verified orphaned (no scene/prefab reference, no source-level reference).
- 3 GameObjects removed from `GamePlay.unity` (`AdsManager`, `Enverement`, `_LegacyManagers/GamePlay`).
- 4 UI prefab shells (`SV_Equipement`, `SV_Process`, `SV_Evolve`, `SV_Mails`) have a `SV_LegacyUIBase` component on root so `UIManager.ShowAsync(UIId.SV_*)` does not crash.
- A new `Luzart.Migration.MigrationFlags` static class exists under `Assets/_Main/Scripts/_LuzartGame/Migration/`, currently empty. Used by later phases.
- Game still plays identically.

## 2. Inventory — items to remove

### 2.1 Script files (delete)

Verified orphaned via `find_gameobjects search_method=by_component`. Each candidate must be re-verified at slice execution time.

**Batch 1 — utility/UI dead code (lowest risk):**

| File | Why orphaned |
|---|---|
| `Assets/_Main/Scripts/Cheat/CheatManager.cs` | Marked `[Obsolete("Đã gộp vào CheatPanel.cs")]` |
| `Assets/_Main/Scripts/Gameplay/BorderCorner.cs` | Empty class, only commented code |
| `Assets/_Main/Scripts/Gameplay/DestroyItems.cs` | Stand-alone coroutine, no attach in scene/prefab |
| `Assets/_Main/Scripts/Gameplay/RotatorePoints.cs` | SelectMap-era utility, SelectMap UI now wrapped |
| `Assets/_Main/Scripts/Gameplay/transformFace.cs` | SelectMap UV-rect animator, unused |
| `Assets/_Main/Scripts/Gameplay/followPoint.cs` | Top-level helper, GunBullte has its own internal FollowPoint |
| `Assets/_Main/Scripts/UI/ScrollContent.cs` | No active prefab attach |
| `Assets/_Main/Scripts/UI/LocalisationPresent.cs` | Phase B already removed spawner integration |
| `Assets/_Main/Scripts/Player/swipe.cs` | SelectMap swipe handler |

**Batch 2 — UI managers (medium-low risk):**

| File | Why orphaned |
|---|---|
| `Assets/_Main/Scripts/UI/MainMenu.cs` | Replaced by `SV_MainMenuUI` |
| `Assets/_Main/Scripts/UI/SettingManager.cs` | Replaced by `SV_SettingsPopupUI` |
| `Assets/_Main/Scripts/UI/ManagerFloatingBtn.cs` | No active prefab attach (referenced only via Inspector by `GameManager.ManagerFloatingBtn` field — see §3.2 below) |
| `Assets/_Main/Scripts/Audio/AudioCheckerPlayer.cs` | Not attached |

**Batch 3 — weapon spawners (verify carefully):**

| File | Why orphaned (claim) | Verify |
|---|---|---|
| `Assets/_Main/Scripts/Gameplay/SpawenManager.cs` | `EnemySpawnerManager` (framework) replaces it | Search `.unity` + `.prefab` for GUID |
| `Assets/_Main/Scripts/Gameplay/RanshoneManager.cs` | Not in scene | Same |
| `Assets/_Main/Scripts/Enemy/AiguleManager.cs` | Not in scene | Same |
| `Assets/_Main/Scripts/Enemy/DroneManager.cs` | Not in scene | Same |
| `Assets/_Main/Scripts/Enemy/ProtectedGreen.cs` | Not in scene | Same |
| `Assets/_Main/Scripts/Weapons/Brick.cs` | Not in scene | Same |
| `Assets/_Main/Scripts/Weapons/RocketManager.cs` | Not in scene; verify `Rocket.prefab` does not have it on root | Check prefab |
| `Assets/_Main/Scripts/Weapons/GunManager.cs` | Not in scene; verify weapon prefab refs | Check |

**Batch 4 — projectile/weapon child scripts (NEEDS prefab audit):**

These may still be on `Bullet*.prefab`, `Wapeon.prefab`, `WeapRotate*.prefab`. Verify before deleting:

| File | Verification step |
|---|---|
| `Assets/_Main/Scripts/Weapons/aigule.cs` (lowercase, on `RotatoreAigule.prefab`?) | grep GUID over `.prefab` |
| `Assets/_Main/Scripts/Weapons/AddBallForce.cs` | grep |
| `Assets/_Main/Scripts/Weapons/ballManager.cs` | grep — `Ball` weapon prefab? |
| `Assets/_Main/Scripts/Weapons/brickManager.cs` | grep |
| `Assets/_Main/Scripts/Weapons/SpinerManager.cs` | grep |
| `Assets/_Main/Scripts/Weapons/SpinnerGun.cs` | grep |
| `Assets/_Main/Scripts/Weapons/CheckWeapons.cs` | grep |
| `Assets/_Main/Scripts/Weapons/GunBullte.cs` | grep |

**Decision rule**: any script whose `.cs.meta` GUID is found in ≥1 `.prefab` or `.unity` file stays for Phase F (when those weapons get re-implemented as ZSkillBehavior). Document the finding in the slice commit message.

### 2.2 Scene GameObjects to delete

| GameObject path | State | Reason |
|---|---|---|
| `AdsManager` | empty (only Transform) | Project removed Google Mobile Ads per `decisions/no-google-ads` |
| `Enverement` | inactive | Stale, no longer referenced |
| `_LegacyManagers/GamePlay` | inactive, holds `ManagerWeapons` | XP-bar driver legacy; HUD now driven by `SV_GameplayHudUI` |

### 2.3 UI prefabs needing `SV_LegacyUIBase` wrapper

4 prefabs currently have **no root component** beyond RectTransform/Image. `UIManager.ShowAsync(UIId.X)` requires a `UIBase` subclass on the root, otherwise the framework destroys the instance and throws.

| Prefab | Action |
|---|---|
| `Assets/_Main/Perfabes/UI/SV_Equipement.prefab` | Add `SV_EquipementUI` component (defined in `SV_LegacyWrappers.cs`, currently exists as a class but no prefab attach) |
| `Assets/_Main/Perfabes/UI/SV_Process.prefab` | Add `SV_ProcessUI` |
| `Assets/_Main/Perfabes/UI/SV_Evolve.prefab` | Add `SV_EvolveUI` |
| `Assets/_Main/Perfabes/UI/SV_Mails.prefab` | Add `SV_MailsUI` |

`SV_SelectMap.prefab` already has `SelectMapManager` + `ManagerSoundSwipe` (no SV_LegacyUIBase) — add `SV_SelectMapUI` too in the same slice.

## 3. Slice plan

Each slice = one commit. After each commit: enter Play, verify boot → main menu → play one round → exit. Capture screenshot if smoke-test reveals visual drift.

### Slice C.1 — Migration scaffold

- Create `Assets/_Main/Scripts/_LuzartGame/Migration/MigrationFlags.cs`:
  ```csharp
  namespace Luzart.Migration
  {
      /// <summary>
      /// Runtime feature flags for the strangler-fig migration.
      /// Authored as a ScriptableObject so Inspector can toggle live during Play.
      /// Each flag tracks one slice in docs/superpowers/specs/.
      /// Remove a flag once its slice is complete.
      /// </summary>
      [CreateAssetMenu(fileName = "MigrationFlags", menuName = "GoGo/Migration Flags")]
      public class MigrationFlags : ScriptableObject
      {
          // Phase C: no flags needed (no behaviour change).
          // Phase D-F: flags appear here as their slices begin.
      }
  }
  ```
- Create asset `Assets/_Main/Data/Migration/MigrationFlags.asset`.
- Create `MigrationFlagsContent : AbstractScriptableContent` wrapper that registers the SO into Domain (so any code can do `Domain.Get<MigrationFlags>()`).
- Wire the wrapper into `_GameBoot.DomainContentLoader.contents`.
- Commit: `migrate(C.1): introduce MigrationFlags ScriptableObject scaffold`.

### Slice C.2 — Verify orphan claims

- No code change. Run a verification batch using Unity MCP `find_gameobjects` + `Grep` over all `.unity` + `.prefab` files for each candidate script GUID.
- Output: an updated list in this spec, replacing §2.1 if anything resolves to "still in use".
- Commit (optional): `docs(C.2): verify orphan inventory`. Or skip commit if no doc edits needed.

### Slice C.3 — Delete Batch 1 (utility/UI dead code)

- Delete 9 files in `§2.1 Batch 1`.
- Refresh Unity, confirm 0 compile errors.
- Play-test smoke.
- Commit: `migrate(C.3): delete dead utility scripts (batch 1)`.

### Slice C.4 — Delete Batch 2 (UI managers)

- Before delete: verify `GameManager.ManagerFloatingBtn` Inspector field is null or replaced. If the field is still serialized, blank it via `manage_components.set_property` before script delete.
- Delete 4 files.
- Refresh, compile clean, play-test.
- Commit: `migrate(C.4): delete legacy UI manager scripts (batch 2)`.

### Slice C.5 — Delete Batch 3 (weapon spawner scripts)

- Re-verify each Batch 3 file via prefab grep.
- Delete confirmed-orphans only. Postponed-to-Phase-F files stay in tree.
- Compile clean, play-test (spawn enemies, watch HUD).
- Commit: `migrate(C.5): delete orphan weapon-spawner scripts (batch 3)`.

### Slice C.6 — Audit + delete Batch 4 (projectile child scripts)

- Open each candidate prefab (`Bullet*`, `Wapeon`, `WeapRotate*`, `RotatoreAigule`, `Ball`).
- Document which scripts are still attached → keep those, defer to Phase F.
- Delete only the confirmed-orphans.
- Compile clean, play-test.
- Commit: `migrate(C.6): delete orphan projectile scripts (batch 4)`.

### Slice C.7 — Delete inactive scene GameObjects

- Open `GamePlay.unity`.
- Use `manage_gameobject(action="delete")` for `AdsManager`, `Enverement`, `_LegacyManagers/GamePlay`.
- Save scene.
- Play-test full flow.
- Commit: `migrate(C.7): remove 3 inactive/empty scene GameObjects`.

### Slice C.8 — Wrap 5 empty UI prefabs

- For each of `SV_Equipement`, `SV_Process`, `SV_Evolve`, `SV_Mails`, `SV_SelectMap`:
  - Open prefab in Prefab Mode.
  - `manage_components(action="add", target=root, component_type="SV_<Name>UI")` (the wrapper classes already exist in `SV_LegacyWrappers.cs`).
  - Save prefab.
- Verify: from MainMenu try clicking each tab — UI now opens via NinjaUI without exception. (Buttons inside may still be broken; that is a Phase D+ concern. `UIButtonSanitizer` in `SV_LegacyUIBase.OnCreateAsync` will neutralize null-target onClick.)
- Commit: `migrate(C.8): add SV_LegacyUIBase wrappers to 5 empty UI prefabs`.

### Slice C.9 — Phase C close-out

- Update `.wiki/wiki/log.md` with a Phase C entry (per project wiki conventions).
- Confirm Phase C success criteria (§4).
- Commit: `migrate(C.9): Phase C close-out`.

## 4. Success criteria

- [ ] `Assets/_Main/Scripts/` no longer contains any of the script files in §2.1 (except those documented as "deferred to Phase F").
- [ ] `GamePlay.unity` has **9 root GameObjects** (was 12).
- [ ] All 5 UI prefabs (`SV_Equipement`, `SV_Process`, `SV_Evolve`, `SV_Mails`, `SV_SelectMap`) have a `SV_LegacyUIBase`-derived script on root.
- [ ] `MigrationFlags` class exists, is empty.
- [ ] Compile clean (0 errors, 0 new warnings beyond baseline).
- [ ] Manual play-test: full boot → main menu → play → kill → level-up → die → menu loop works.

## 5. Out of scope

- Porting Shop/Equipment UI to NinjaUI-native (still legacy logic inside the prefab, just wrapped).
- Deleting `SV_LegacyWrappers.cs` itself — still needed.
- Modifying `_FrameworkStubs.cs` — Phase E concern.
- Any logic change to `GameManager`, `PlayerManager`, `EnemyManager`, etc. — those are Phase F.

## 6. Risks

| Risk | Mitigation |
|---|---|
| A "Batch 4" script claimed orphan is actually on a runtime-spawned prefab not yet inspected | Mandatory prefab grep in Slice C.6 before delete |
| Inspector field on `GameManager` (e.g. `ManagerFloatingBtn`) still references a now-deleted MonoBehaviour → serialized as `m_Script: {fileID: 0}` (missing script) | Set field to null via `manage_components.set_property` in Slice C.4 before script delete |
| `SV_LegacyUIBase` wrapper added to `SV_Equipement` etc. exposes new bugs (e.g. animation freeze on open) | Smoke-test each one in Slice C.8 individually |
| Deleting `_LegacyManagers/GamePlay` GO loses an Inspector ref a future phase needs | Inspect for incoming references via `Find References In Scene` before delete (Slice C.7) |

## 7. Out-of-band notes

- `Assets/_Main/Scripts/_LegacyCompat/SV_SkillCatalog.cs` is misfiled — it is a **new** SO type used by `SV_LevelUpPopupUI`, not legacy. Phase C does **not** move or touch it. Tracked for cleanup at end of migration.
