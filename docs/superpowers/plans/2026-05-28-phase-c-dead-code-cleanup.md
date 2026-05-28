# Phase C — Dead Code Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Delete orphaned legacy scripts, remove inactive scene GameObjects, and add `SV_LegacyUIBase` wrappers to 5 empty UI prefabs so `UIManager.ShowAsync` does not crash. Game remains playable after every commit.

**Architecture:** Strangler-fig dead-code removal. Every deletion is preceded by a verification step that confirms the script GUID does not appear in any `.unity` or `.prefab` file. Each batch deletion is its own commit so revert is single-step.

**Tech Stack:** Unity 6000.3.14f1, MCP for Unity (manage_components, manage_prefabs, manage_gameobject, find_gameobjects, Grep), C# 9, .NET Standard 2.1.

**Spec:** `docs/superpowers/specs/2026-05-28-phase-c-dead-code-cleanup-design.md`

**Verification pattern after every commit:**
1. Read `mcpforunity://editor/state` → `is_compiling == false`
2. `read_console(types=["error"], count=10)` → zero errors
3. Smoke play-test by reading scene state and (optionally) `manage_camera(action="screenshot", include_image=true, max_resolution=256)`. **Skip play-mode automation** — manual play is enough.

---

## File Structure

### Files to CREATE

| Path | Responsibility |
|---|---|
| `Assets/_Main/Scripts/_LuzartGame/Migration/MigrationFlags.cs` | `MigrationFlags : ScriptableObject` — empty flag container, runtime-toggleable in Inspector. |
| `Assets/_Main/Scripts/_LuzartGame/Migration/MigrationFlagsContent.cs` | `MigrationFlagsContent : AbstractScriptableContent` — registers `MigrationFlags` SO into `Domain` so any code can resolve it. |
| `Assets/_Main/Data/Migration/MigrationFlags.asset` | The SO instance (empty in Phase C). |

### Files to MODIFY

| Path | Reason |
|---|---|
| Scene `Assets/_Main/Scenes/GamePlay.unity` | Delete 3 inactive GameObjects (`AdsManager`, `Enverement`, `_LegacyManagers/GamePlay`). Wire `MigrationFlagsContent` into `_GameBoot.DomainContentLoader.contents`. |
| 5 prefabs `Assets/_Main/Perfabes/UI/SV_{Equipement,Process,Evolve,Mails,SelectMap}.prefab` | Add `SV_<Name>UI` MonoBehaviour to root (class already exists in `SV_LegacyWrappers.cs`). |

### Files to DELETE (after verification)

Batch 1 — utility dead code:
- `Assets/_Main/Scripts/Cheat/CheatManager.cs`
- `Assets/_Main/Scripts/Gameplay/BorderCorner.cs`
- `Assets/_Main/Scripts/Gameplay/DestroyItems.cs`
- `Assets/_Main/Scripts/Gameplay/RotatorePoints.cs`
- `Assets/_Main/Scripts/Gameplay/transformFace.cs`
- `Assets/_Main/Scripts/Gameplay/followPoint.cs`
- `Assets/_Main/Scripts/UI/ScrollContent.cs`
- `Assets/_Main/Scripts/UI/LocalisationPresent.cs`
- `Assets/_Main/Scripts/Player/swipe.cs`

Batch 2 — UI managers:
- `Assets/_Main/Scripts/UI/MainMenu.cs`
- `Assets/_Main/Scripts/UI/SettingManager.cs`
- `Assets/_Main/Scripts/UI/ManagerFloatingBtn.cs`
- `Assets/_Main/Scripts/Audio/AudioCheckerPlayer.cs`

Batch 3 — weapon spawners (verify per file):
- `Assets/_Main/Scripts/Gameplay/SpawenManager.cs`
- `Assets/_Main/Scripts/Gameplay/RanshoneManager.cs`
- `Assets/_Main/Scripts/Enemy/AiguleManager.cs`
- `Assets/_Main/Scripts/Enemy/DroneManager.cs`
- `Assets/_Main/Scripts/Enemy/ProtectedGreen.cs`
- `Assets/_Main/Scripts/Weapons/Brick.cs`
- `Assets/_Main/Scripts/Weapons/RocketManager.cs`
- `Assets/_Main/Scripts/Weapons/GunManager.cs`

Batch 4 — projectile child scripts (verify per file): 8 candidates, only delete those whose GUID returns 0 grep hits in `.prefab` files.

---

## Task 1: Migration scaffold (Slice C.1)

**Files:**
- Create: `Assets/_Main/Scripts/_LuzartGame/Migration/MigrationFlags.cs`
- Create: `Assets/_Main/Scripts/_LuzartGame/Migration/MigrationFlagsContent.cs`
- Create: `Assets/_Main/Data/Migration/MigrationFlags.asset`
- Modify: `_GameBoot.DomainContentLoader.contents` (add `MigrationFlagsContent` ref)

- [ ] **Step 1.1: Create the `MigrationFlags` ScriptableObject script**

Use `create_script` (MCP tool, not Bash):

```csharp
using UnityEngine;

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

Path: `Assets/_Main/Scripts/_LuzartGame/Migration/MigrationFlags.cs`.

- [ ] **Step 1.2: Wait for compile, check console**

Read `mcpforunity://editor/state` until `is_compiling == false`. Then `read_console(types=["error"], count=10)`. Expected: zero errors.

- [ ] **Step 1.3: Create the content wrapper**

Inspect `AbstractScriptableContent` first to confirm the base API:

```
Read Assets/_Main/Scripts/_LuzartGame/DependencyInjection/AbstractScriptableContent.cs
```

Then create `Assets/_Main/Scripts/_LuzartGame/Migration/MigrationFlagsContent.cs`:

```csharp
using UnityEngine;

namespace Luzart.Migration
{
    /// <summary>
    /// Domain content wrapper for MigrationFlags. Registers the flag SO
    /// so any framework code can resolve it via Domain.Get<MigrationFlags>().
    /// </summary>
    [CreateAssetMenu(fileName = "MigrationFlagsContent", menuName = "GoGo/Migration Flags Content")]
    public class MigrationFlagsContent : AbstractScriptableContent
    {
        [SerializeField] private MigrationFlags _flags;

        public MigrationFlags Flags => _flags;

        protected override void DoInject(IDomain domain)
        {
            base.DoInject(domain);
            if (_flags != null)
                domain.Add(_flags);
        }
    }
}
```

**IMPORTANT — adjust API if needed:** If `AbstractScriptableContent.DoInject` signature differs, use the actual signature from the read above. Same for `IDomain.Add<T>()` — confirm the generic vs non-generic form.

- [ ] **Step 1.4: Wait for compile + verify no errors**

Read `mcpforunity://editor/state` and `read_console`. Fix any compile errors before continuing.

- [ ] **Step 1.5: Create the `MigrationFlags.asset` file via menu**

Use `execute_menu_item`:
```
manage_asset(action="create_folder", path="Assets/_Main/Data/Migration")
manage_asset(action="create", path="Assets/_Main/Data/Migration/MigrationFlags.asset",
             asset_type="ScriptableObject", properties={"type": "Luzart.Migration.MigrationFlags"})
```

If `manage_asset` doesn't support creating a ScriptableObject directly, fall back to `execute_menu_item(menu_path="Assets/Create/GoGo/Migration Flags")` and rename if needed.

Verify the asset exists: `manage_asset(action="get_info", path="Assets/_Main/Data/Migration/MigrationFlags.asset")`.

- [ ] **Step 1.6: Create the `MigrationFlagsContent.asset` file**

Same pattern as 1.5. Path: `Assets/_Main/Data/Migration/MigrationFlagsContent.asset`.

Use `manage_scriptable_object(action="modify", target={"path": "Assets/_Main/Data/Migration/MigrationFlagsContent.asset"}, patches=[{"path": "_flags", "value": {"path": "Assets/_Main/Data/Migration/MigrationFlags.asset"}}])` to wire the SO reference.

- [ ] **Step 1.7: Wire `MigrationFlagsContent` into `_GameBoot`**

Read the live `DomainContentLoader` component first to learn its `contents` field shape:

```
find_gameobjects(search_term="_GameBoot", search_method="by_name")
# returns instanceID 142014
manage_components(action="set_property", target=142014, component_type="DomainContentLoader",
                  property="contents", value=<append-the-MigrationFlagsContent-asset-path>)
```

Because the existing `contents` field is an array of asset refs, **read it first** via the components resource and append rather than overwrite:

```
Read resource mcpforunity://scene/gameobject/142014/component/DomainContentLoader
```

Then set the property with the appended list.

- [ ] **Step 1.8: Save the scene + verify**

Use `manage_scene(action="save")`. Then enter Play mode mentally (we don't auto-enter Play — just verify compile clean + scene loads):

```
read_console(types=["error"], count=10)
```

Expected: zero errors.

- [ ] **Step 1.9: Commit**

```bash
cd "/d/Unity Training/survivorIOSource/DATN-GoGoSurvival"
git add Assets/_Main/Scripts/_LuzartGame/Migration/ Assets/_Main/Data/Migration/ Assets/_Main/Scenes/GamePlay.unity
git status
git commit -m "migrate(C.1): introduce MigrationFlags ScriptableObject scaffold

Empty flag container + Domain content wrapper. Phase D-F slices will
add flags here. Asset under Assets/_Main/Data/Migration/.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 2: Verify Batch 1 orphans (Slice C.2 — read-only audit)

**Files:** None modified.

- [ ] **Step 2.1: Get each script's GUID**

For each Batch 1 file, read its `.meta` to get the GUID:

```
Read Assets/_Main/Scripts/Cheat/CheatManager.cs.meta
Read Assets/_Main/Scripts/Gameplay/BorderCorner.cs.meta
Read Assets/_Main/Scripts/Gameplay/DestroyItems.cs.meta
Read Assets/_Main/Scripts/Gameplay/RotatorePoints.cs.meta
Read Assets/_Main/Scripts/Gameplay/transformFace.cs.meta
Read Assets/_Main/Scripts/Gameplay/followPoint.cs.meta
Read Assets/_Main/Scripts/UI/ScrollContent.cs.meta
Read Assets/_Main/Scripts/UI/LocalisationPresent.cs.meta
Read Assets/_Main/Scripts/Player/swipe.cs.meta
```

Record the 9 GUIDs.

- [ ] **Step 2.2: Grep each GUID across scenes + prefabs**

For each GUID, run:

```
Grep pattern="guid: <GUID>" glob="**/*.unity"
Grep pattern="guid: <GUID>" glob="**/*.prefab"
```

Expected: zero matches for each (orphan confirmed). Record any hits → those files do NOT get deleted in Task 3.

- [ ] **Step 2.3: Cross-check via find_gameobjects**

```
find_gameobjects(search_term="CheatManager", search_method="by_component")
find_gameobjects(search_term="BorderCorner", search_method="by_component")
…(repeat for all 9)
```

Expected: `totalCount: 0` for each. (CheatPanel exists in scene but that's a different class.)

- [ ] **Step 2.4: No commit needed — this slice is verification only**

If all 9 confirm orphan: proceed to Task 3.
If any has hits: edit `docs/superpowers/specs/2026-05-28-phase-c-dead-code-cleanup-design.md` §2.1 to remove that file from Batch 1, then commit:
```bash
git add docs/superpowers/specs/2026-05-28-phase-c-dead-code-cleanup-design.md
git commit -m "docs(C.2): scope down Batch 1 after orphan audit"
```

---

## Task 3: Delete Batch 1 (Slice C.3)

**Files DELETE:** 9 files listed in File Structure above (Batch 1).

- [ ] **Step 3.1: Delete each script via `delete_script`**

For each of the 9 files confirmed orphan in Task 2:

```
delete_script(uri="Assets/_Main/Scripts/Cheat/CheatManager.cs")
delete_script(uri="Assets/_Main/Scripts/Gameplay/BorderCorner.cs")
delete_script(uri="Assets/_Main/Scripts/Gameplay/DestroyItems.cs")
delete_script(uri="Assets/_Main/Scripts/Gameplay/RotatorePoints.cs")
delete_script(uri="Assets/_Main/Scripts/Gameplay/transformFace.cs")
delete_script(uri="Assets/_Main/Scripts/Gameplay/followPoint.cs")
delete_script(uri="Assets/_Main/Scripts/UI/ScrollContent.cs")
delete_script(uri="Assets/_Main/Scripts/UI/LocalisationPresent.cs")
delete_script(uri="Assets/_Main/Scripts/Player/swipe.cs")
```

`delete_script` removes both the `.cs` and the `.cs.meta`.

- [ ] **Step 3.2: Wait for compile + check console**

```
read mcpforunity://editor/state → is_compiling == false
read_console(types=["error"], count=10)
```

Expected: zero errors. If any compile error appears (e.g. another file imports a deleted type), STOP and investigate.

- [ ] **Step 3.3: Commit**

```bash
cd "/d/Unity Training/survivorIOSource/DATN-GoGoSurvival"
git add -A Assets/_Main/Scripts/Cheat Assets/_Main/Scripts/Gameplay Assets/_Main/Scripts/UI Assets/_Main/Scripts/Player
git status
git commit -m "migrate(C.3): delete 9 orphan utility scripts (batch 1)

Deleted: CheatManager (obsolete merge), BorderCorner (empty),
DestroyItems, RotatorePoints, transformFace, followPoint,
ScrollContent, LocalisationPresent, swipe. All verified orphan
via GUID grep over .unity + .prefab.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 4: Audit + delete Batch 2 (Slice C.4)

**Files:** Possibly modify a scene Inspector field on `GameManager`, then DELETE 4 files.

- [ ] **Step 4.1: Get Batch 2 GUIDs + grep**

```
Read Assets/_Main/Scripts/UI/MainMenu.cs.meta
Read Assets/_Main/Scripts/UI/SettingManager.cs.meta
Read Assets/_Main/Scripts/UI/ManagerFloatingBtn.cs.meta
Read Assets/_Main/Scripts/Audio/AudioCheckerPlayer.cs.meta
```

Grep each GUID across `**/*.unity` + `**/*.prefab`.

- [ ] **Step 4.2: Special — check `GameManager.ManagerFloatingBtn` Inspector field**

`GameManager.cs` has a serialized field `ManagerFloatingBtn ManagerFloatingBtn`. Check the scene:

```
find_gameobjects(search_term="GameManager", search_method="by_component")
# instanceID 141304 from prior session
Read mcpforunity://scene/gameobject/141304/component/GameManager
```

Look for the `ManagerFloatingBtn` field. If it's set to a non-null GameObject:
- The referenced GameObject hosts a `ManagerFloatingBtn` component. Use `find_gameobjects search_term=ManagerFloatingBtn search_method=by_component` to confirm. If `totalCount > 0`, this file is NOT orphan — remove from Batch 2.

If the field is null: proceed to clear the Inspector reference and delete the script.

To clear: `manage_components(action="set_property", target=141304, component_type="GameManager", property="ManagerFloatingBtn", value=null)`.

- [ ] **Step 4.3: Delete the confirmed-orphan files**

```
delete_script(uri="Assets/_Main/Scripts/UI/MainMenu.cs")
delete_script(uri="Assets/_Main/Scripts/UI/SettingManager.cs")
delete_script(uri="Assets/_Main/Scripts/UI/ManagerFloatingBtn.cs")  # only if §4.2 confirmed orphan
delete_script(uri="Assets/_Main/Scripts/Audio/AudioCheckerPlayer.cs")
```

- [ ] **Step 4.4: Wait + console**

```
read mcpforunity://editor/state → is_compiling == false
read_console(types=["error"], count=10)
```

Zero errors expected.

- [ ] **Step 4.5: Commit**

```bash
git add -A Assets/_Main/Scripts/UI Assets/_Main/Scripts/Audio Assets/_Main/Scenes/GamePlay.unity
git status
git commit -m "migrate(C.4): delete legacy UI/audio manager scripts (batch 2)

Deleted: MainMenu (replaced by SV_MainMenuUI), SettingManager
(replaced by SV_SettingsPopupUI), ManagerFloatingBtn, AudioCheckerPlayer.
GameManager Inspector field cleared if applicable.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 5: Audit + delete Batch 3 (Slice C.5 — weapon spawners)

**Files:** DELETE up to 8 spawner scripts.

- [ ] **Step 5.1: Get Batch 3 GUIDs**

```
Read Assets/_Main/Scripts/Gameplay/SpawenManager.cs.meta
Read Assets/_Main/Scripts/Gameplay/RanshoneManager.cs.meta
Read Assets/_Main/Scripts/Enemy/AiguleManager.cs.meta
Read Assets/_Main/Scripts/Enemy/DroneManager.cs.meta
Read Assets/_Main/Scripts/Enemy/ProtectedGreen.cs.meta
Read Assets/_Main/Scripts/Weapons/Brick.cs.meta
Read Assets/_Main/Scripts/Weapons/RocketManager.cs.meta
Read Assets/_Main/Scripts/Weapons/GunManager.cs.meta
```

- [ ] **Step 5.2: Grep each across `.unity` + `.prefab`**

```
Grep pattern="guid: <GUID>" glob="**/*.unity"
Grep pattern="guid: <GUID>" glob="**/*.prefab"
```

For each GUID with `0` hits → orphan, delete.
For each GUID with `≥1` hit → record path + line; this file stays for Phase F to handle.

- [ ] **Step 5.3: Delete confirmed-orphans**

```
delete_script(uri="<path>")
```
…for each orphan.

- [ ] **Step 5.4: Wait + console**

```
read mcpforunity://editor/state
read_console(types=["error"], count=10)
```

- [ ] **Step 5.5: Commit**

```bash
git add -A Assets/_Main/Scripts/Gameplay Assets/_Main/Scripts/Enemy Assets/_Main/Scripts/Weapons
git status
git commit -m "migrate(C.5): delete orphan weapon-spawner scripts (batch 3)

Deleted: <list actual deletes here>. Files with prefab refs
deferred to Phase F: <list deferred here>.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 6: Audit + delete Batch 4 (Slice C.6 — projectile child scripts)

**Files:** DELETE up to 8 projectile/weapon child scripts.

- [ ] **Step 6.1: Enumerate Batch 4 candidates**

```
Glob pattern="Assets/_Main/Scripts/Weapons/*.cs"
```

Subtract files already in Batch 3 + verify the list matches spec §2.1 Batch 4:
- `aigule.cs`, `AddBallForce.cs`, `ballManager.cs`, `brickManager.cs`,
- `SpinerManager.cs`, `SpinnerGun.cs`, `CheckWeapons.cs`, `GunBullte.cs`

- [ ] **Step 6.2: Get each GUID + grep**

For each:
```
Read Assets/_Main/Scripts/Weapons/<file>.cs.meta
Grep pattern="guid: <GUID>" glob="**/*.unity"
Grep pattern="guid: <GUID>" glob="**/*.prefab"
```

Record orphan vs in-use.

- [ ] **Step 6.3: For in-use scripts, identify the prefab path**

Document in the next commit message which prefab references the script. Those scripts are **deferred to Phase F** — DO NOT delete them now.

- [ ] **Step 6.4: Delete orphans**

```
delete_script(uri="<path>")
```

- [ ] **Step 6.5: Wait + console**

```
read mcpforunity://editor/state
read_console(types=["error"], count=10)
```

- [ ] **Step 6.6: Commit**

```bash
git add -A Assets/_Main/Scripts/Weapons
git status
git commit -m "migrate(C.6): delete orphan projectile child scripts (batch 4)

Deleted: <list>. Deferred to Phase F (still on prefabs): <list>.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 7: Delete inactive scene GameObjects (Slice C.7)

**Files:** Modify `Assets/_Main/Scenes/GamePlay.unity`.

- [ ] **Step 7.1: Locate the three targets**

```
find_gameobjects(search_term="AdsManager", search_method="by_name")        # expect 1 hit
find_gameobjects(search_term="Enverement", search_method="by_name")        # expect 1 hit
find_gameobjects(search_term="GamePlay", search_method="by_name")           # multiple — pick the one whose parent is _LegacyManagers
```

For each result, **verify path** via the components resource so we delete the right one (especially `GamePlay`, which is also the scene name):

```
Read mcpforunity://scene/gameobject/<id>
```

Confirm path:
- `AdsManager` — `AdsManager`
- `Enverement` — `Enverement`
- `_LegacyManagers/GamePlay` — has `ManagerWeapons` component

- [ ] **Step 7.2: Verify no incoming references**

For each candidate's instanceID, run a project-wide grep over `.unity` for any other GameObject that references it via its file ID. Specifically:
```
Grep pattern="m_GameObject:" glob="**/*.unity"   # too broad; instead:
```

Use `find_gameobjects` to look for scripts that might reference these by name in `SerializeField string name`. If `_LegacyManagers/GamePlay` has `ManagerWeapons` and that component has Inspector refs to it, breaking those refs is fine because the GO is being deleted.

Run `find_in_file pattern="AdsManager" uri="Assets/_Main/Scenes/GamePlay.unity"` — expect only the GO definition itself, not other refs.

- [ ] **Step 7.3: Delete each**

```
manage_gameobject(action="delete", target=<AdsManager instanceID>)
manage_gameobject(action="delete", target=<Enverement instanceID>)
manage_gameobject(action="delete", target=<_LegacyManagers/GamePlay instanceID>)
```

- [ ] **Step 7.4: Verify scene root count**

```
manage_scene(action="get_active")
# rootCount should be 9 (was 12)
```

- [ ] **Step 7.5: Save scene + verify**

```
manage_scene(action="save")
read_console(types=["error"], count=10)
```

- [ ] **Step 7.6: Commit**

```bash
git add Assets/_Main/Scenes/GamePlay.unity
git status
git commit -m "migrate(C.7): remove 3 inactive/empty scene GameObjects

Removed AdsManager (empty, no ads in build), Enverement (inactive,
stale), and _LegacyManagers/GamePlay (held inactive ManagerWeapons
XP-bar driver — replaced by SV_GameplayHudUI). Root count 12 → 9.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 8: Wrap 5 empty UI prefabs (Slice C.8)

**Files:** Modify 5 prefabs at `Assets/_Main/Perfabes/UI/SV_{Equipement,Process,Evolve,Mails,SelectMap}.prefab`.

- [ ] **Step 8.1: Confirm wrapper classes exist in `SV_LegacyWrappers.cs`**

```
Read Assets/_Main/Scripts/UI/NinjaUIScreens/SV_LegacyWrappers.cs
```

Verify the file declares: `SV_EquipementUI`, `SV_ProcessUI`, `SV_EvolveUI`, `SV_MailsUI`, `SV_SelectMapUI`. If any missing, **add them** (each is a 3-line subclass of `SV_LegacyUIBase`):

```csharp
public class SV_EquipementUI : SV_LegacyUIBase { }
public class SV_ProcessUI : SV_LegacyUIBase { }
public class SV_EvolveUI : SV_LegacyUIBase { }
public class SV_MailsUI : SV_LegacyUIBase { }
public class SV_SelectMapUI : SV_LegacyUIBase { }
```

Use `script_apply_edits` with an `anchor_insert` op at end of file if classes need to be added.

Wait for compile + console clean.

- [ ] **Step 8.2: Open SV_Equipement prefab + add wrapper**

```
manage_prefabs(action="open_prefab_stage", prefab_path="Assets/_Main/Perfabes/UI/SV_Equipement.prefab")
# get the root GO instance id from prefab stage
find_gameobjects(search_term="SV_Equipement", search_method="by_name")
manage_components(action="add", target=<root id>, component_type="SV_EquipementUI")
manage_prefabs(action="save_prefab_stage")
manage_prefabs(action="close_prefab_stage")
```

Alternative if `open_prefab_stage` flow is awkward: use `manage_prefabs(action="modify_contents", prefab_path=..., create_child=null, component_properties=null)` if MCP supports adding a component on the root directly. Otherwise use the open/save/close pattern.

- [ ] **Step 8.3: Repeat 8.2 for the other 4 prefabs**

`SV_Process.prefab` → `SV_ProcessUI`
`SV_Evolve.prefab` → `SV_EvolveUI`
`SV_Mails.prefab` → `SV_MailsUI`
`SV_SelectMap.prefab` → `SV_SelectMapUI`

(For `SV_SelectMap`, the existing `SelectMapManager + ManagerSoundSwipe` components stay; we just ADD `SV_SelectMapUI` alongside.)

- [ ] **Step 8.4: Verify each prefab root now has the wrapper**

```
manage_prefabs(action="get_info", prefab_path="Assets/_Main/Perfabes/UI/SV_Equipement.prefab")
# rootComponentTypes should include "SV_EquipementUI"
```

Repeat for all 5.

- [ ] **Step 8.5: Console clean**

```
read_console(types=["error"], count=10)
```

- [ ] **Step 8.6: Commit**

```bash
git add Assets/_Main/Perfabes/UI/SV_*.prefab Assets/_Main/Scripts/UI/NinjaUIScreens/SV_LegacyWrappers.cs
git status
git commit -m "migrate(C.8): add SV_LegacyUIBase wrappers to 5 empty UI prefabs

UIManager.ShowAsync(UIId.SV_{Equipement,Process,Evolve,Mails,SelectMap})
now resolves to a UIBase subclass on root and does not crash. Legacy
internals (ShopManager, SelectMapManager) still drive content.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 9: Phase C close-out (Slice C.9)

**Files:** Optionally update `.wiki/wiki/log.md` per project wiki conventions.

- [ ] **Step 9.1: Verify success criteria**

Run these checks:

```
manage_scene(action="get_active")
# rootCount == 9
```

```
find_gameobjects(search_term="MigrationFlags", search_method="by_component")  # 0 in scene (it's an SO, not MB)
manage_asset(action="get_info", path="Assets/_Main/Data/Migration/MigrationFlags.asset")
manage_asset(action="get_info", path="Assets/_Main/Data/Migration/MigrationFlagsContent.asset")
# both exist
```

For each of the 5 prefabs:
```
manage_prefabs(action="get_info", prefab_path="Assets/_Main/Perfabes/UI/<name>.prefab")
# rootComponentTypes includes "SV_<Name>UI"
```

Final console check:
```
read_console(types=["error"], count=10)
```

- [ ] **Step 9.2: Update `.wiki/wiki/log.md`**

```
Read .wiki/wiki/log.md
```

Append a new section for 2026-05-28 / Phase C with:
- Files deleted (full list from Tasks 3, 4, 5, 6)
- GameObjects deleted from scene (3 from Task 7)
- Prefabs wrapped (5 from Task 8)
- Migration scaffold (Task 1)

Use existing log format (read the latest entry to mirror style).

- [ ] **Step 9.3: Commit close-out**

```bash
git add .wiki/wiki/log.md
git status
git commit -m "migrate(C.9): Phase C close-out — wiki log + verify

Phase C complete. Dead code removed, scene root 12 → 9, 5 UI
prefab wrappers added, MigrationFlags scaffold ready for Phase D.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

- [ ] **Step 9.4: Tag the commit (optional but recommended)**

```bash
git tag phase-c-complete
```

---

## Phase C Success Verification

After Task 9 completes:

- [ ] Scene has 9 root GameObjects (down from 12). Confirmed via `manage_scene(action="get_active")`.
- [ ] `Assets/_Main/Data/Migration/MigrationFlags.asset` exists. Confirmed via `manage_asset(action="get_info", ...)`.
- [ ] All 5 UI prefabs have a `SV_LegacyUIBase`-derived root component. Confirmed via `manage_prefabs(action="get_info", ...)` for each.
- [ ] At least 9 (Batch 1) + 3-4 (Batch 2) + N (Batch 3) + M (Batch 4) script files deleted. Confirmed via `git log --diff-filter=D --name-only`.
- [ ] `read_console(types=["error"], count=10)` returns no errors after every commit.
- [ ] Manual sanity check (user-driven, not automated): enter Play, click through MainMenu → Play → Pause → Settings → quit → Re-play. All paths work.

If verification fails: `git revert <breaking sha>` reverts cleanly; spec stays unchanged for re-execution.
