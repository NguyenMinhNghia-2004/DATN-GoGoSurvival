# Phase D — Remove `DATN.Legacy.UIManager` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Replace `DATN.Legacy.UIManager` (data hub on `_LegacyManagers/_LegacyUIScripts`) with ScriptableObjects (`WeaponCatalog`, `LevelCatalog`) registered in Domain. Migrate every caller. Delete the legacy class + GameObject. Absorb 2 deferred Phase C inactive-GO deletions.

**Architecture:** Strangler-fig with `MigrationFlags`. Add new SO + Domain registration → introduce flag (off) → switch callers behind flag → cutover (flag on) → delete legacy paths + GO.

**Spec:** `docs/superpowers/specs/2026-05-28-phase-d-legacy-uimanager-removal-design.md`

**Verification per slice:** compile clean, `read_console errors=0`, scene saves OK.

---

## Files

### CREATE
- `Assets/_Main/Scripts/_LuzartGame/LuzartTechnical/DataResources/Definition/WeaponCatalog.cs` — `WeaponCatalog : ScriptableObject` with 12 Entry records
- `Assets/_Main/Scripts/_LuzartGame/LuzartTechnical/DataResources/Definition/WeaponCatalogContent.cs` — `WeaponCatalogContent : AbstractScriptableContent` to register catalog into Domain
- `Assets/_Main/Data/Weapons/WeaponCatalog.asset` — 12 entries copied from `SpriteWeapons`
- `Assets/_Main/Data/Weapons/WeaponCatalogContent.asset`
- `Assets/_Main/Scripts/_LuzartGame/LuzartTechnical/DataResources/Definition/LevelCatalog.cs`
- `Assets/_Main/Scripts/_LuzartGame/LuzartTechnical/DataResources/Definition/LevelCatalogContent.cs`
- `Assets/_Main/Data/Levels/LevelCatalog.asset` — references Level1..Level6 prefabs
- `Assets/_Main/Data/Levels/LevelCatalogContent.asset`
- `Assets/Editor/Migration/DeleteInactiveLegacyMenuItem.cs` — Editor utility to delete inactive root GOs

### MODIFY
- `Assets/_Main/Scripts/_LuzartGame/Migration/MigrationFlags.cs` — add `UseWeaponCatalog`, `UseLevelCatalog` flags
- `Assets/_Main/Scripts/_LuzartGame/Gameplay/System/GameController.cs` — add `SpawnDefaultLevel()`, `MapReady` property
- `Assets/_Main/Scripts/UI/NinjaUIScreens/SV_MainMenuUI.cs` — switch from `UIManager.PlayBtn()` to `GameController.StartGameplay()` behind flag
- Scene `Assets/_Main/Scenes/GamePlay.unity` — wire new SO contents, delete _LegacyManagers + Enverement
- `Assets/_Main/Scripts/Enemy/EnemyManager.cs` — replace `UIManager` read with `Domain.Get<WeaponCatalog>` (if it reads SpriteWeapons fields)
- `Assets/_Main/Scripts/Weapons/BoltSHooter.cs` — same
- `Assets/_Main/Scripts/Data/CoinsManager.cs`, `Assets/_Main/Scripts/Data/DiamondVip.cs` — replace `UIManager.MapReady` with `GameController.MapReady`

### DELETE
- `Assets/_Main/Scripts/UI/UIManager.cs`
- `Assets/_Main/Scripts/Gameplay/LevelsManager.cs` (if no other readers)
- `Assets/_Main/Scripts/Weapons/SpriteWeapons.cs` (if `SV_GameplayHud` doesn't use it)
- Scene GOs: `_LegacyManagers/_LegacyUIScripts`, `_LegacyManagers`, `_LegacyManagers/GamePlay` (deferred), `Enverement` (deferred)

---

## Task 1: Audit caller list (D.1)

- [ ] **Step 1.1**: Grep all readers of legacy `UIManager`:
  ```
  Grep "FindFirstObjectByType<.*UIManager>|FindObjectOfType<.*UIManager>|UIManager.Instance"
       glob="*.cs"
  ```
- [ ] **Step 1.2**: For each hit, identify what fields/methods are accessed (Weapons.X, MapReady, PlayBtn, etc.). Record in commit msg.
- [ ] **Step 1.3**: Read `SV_GameplayHudUI.cs` — check if it reads `SpriteWeapons` to render weapon icons (impacts whether SpriteWeapons.cs is deletable).
- [ ] **Step 1.4**: No commit needed unless audit changes spec.

## Task 2: Create WeaponCatalog (D.1)

- [ ] **Step 2.1**: Read `SpriteWeapons.cs` to copy field names/types.
- [ ] **Step 2.2**: Create `WeaponCatalog.cs`:

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Luzart
{
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "GoGo/Weapon Catalog")]
    public class WeaponCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public GameObject prefab;
            public Sprite sprite;
            public string displayName;
            [TextArea] public string description;
        }

        [SerializeField] private Entry[] _entries;

        public IReadOnlyList<Entry> Entries => _entries;

        public bool TryGet(string id, out Entry entry)
        {
            foreach (var e in _entries)
            {
                if (e.id == id) { entry = e; return true; }
            }
            entry = default;
            return false;
        }
    }
}
```

- [ ] **Step 2.3**: Create `WeaponCatalogContent.cs`:

```csharp
using UnityEngine;

namespace Luzart
{
    [CreateAssetMenu(fileName = "WeaponCatalogContent", menuName = "GoGo/Weapon Catalog Content")]
    public class WeaponCatalogContent : AbstractScriptableContent
    {
        [SerializeField] private WeaponCatalog _catalog;
        public WeaponCatalog Catalog => _catalog;

        protected override void DoInject(IDomain domain)
        {
            base.DoInject(domain);
            if (_catalog != null)
                domain.Add(_catalog);
        }
    }
}
```

- [ ] **Step 2.4**: Refresh Unity, verify compile clean.
- [ ] **Step 2.5**: Create `WeaponCatalog.asset` + `WeaponCatalogContent.asset` via `manage_scriptable_object`.
- [ ] **Step 2.6**: Wire `WeaponCatalogContent.asset._catalog` → `WeaponCatalog.asset`.
- [ ] **Step 2.7**: Read `SpriteWeapons` component on `Controller` GO (instanceID 142154) — get the 12 weapon prefab/sprite/name/desc refs.
- [ ] **Step 2.8**: Populate `WeaponCatalog._entries` array with 12 entries via `manage_scriptable_object.modify`.
- [ ] **Step 2.9**: Add `WeaponCatalogContent.asset` to `_GameBoot.DomainContentLoader.contents` array.
- [ ] **Step 2.10**: Save scene, verify console clean.
- [ ] **Step 2.11**: Commit: `migrate(D.1): introduce WeaponCatalog SO with 12 entries`.

## Task 3: Switch callers to WeaponCatalog (D.2-D.4)

- [ ] **Step 3.1**: Add `UseWeaponCatalog` bool to `MigrationFlags.cs` (default false).
- [ ] **Step 3.2**: For each caller identified in D.1 reading `UIManager.Weapons.X`:
  - Replace with `if (MigrationFlags.UseWeaponCatalog) { domain.Get<WeaponCatalog>().TryGet("X", out e) → e.prefab } else { legacy }`.
  - Cache `Domain.Get<WeaponCatalog>()` once per component if hot path.
- [ ] **Step 3.3**: Compile clean, smoke test (flag off → game behaves identically).
- [ ] **Step 3.4**: Commit: `migrate(D.2): dual-path weapon resolution behind flag`.
- [ ] **Step 3.5**: Flip flag in asset to true (`manage_scriptable_object.modify`).
- [ ] **Step 3.6**: Play-test mentally (smoke check: console clean, scene state OK).
- [ ] **Step 3.7**: Commit: `migrate(D.3): cutover to WeaponCatalog as default`.

## Task 4: Remove legacy weapon paths (D.5)

- [ ] **Step 4.1**: Delete the `else` branches in callers.
- [ ] **Step 4.2**: If `SV_GameplayHudUI` doesn't read `SpriteWeapons`, remove `SpriteWeapons` MonoBehaviour from `Controller` GameObject; delete `SpriteWeapons.cs`.
- [ ] **Step 4.3**: Remove `UseWeaponCatalog` flag from `MigrationFlags.cs`.
- [ ] **Step 4.4**: Compile + verify.
- [ ] **Step 4.5**: Commit: `migrate(D.4): delete SpriteWeapons + legacy weapon paths`.

## Task 5: LevelCatalog + GameController.SpawnDefaultLevel (D.6-D.7)

- [ ] **Step 5.1**: Create `LevelCatalog.cs`:

```csharp
using UnityEngine;

namespace Luzart
{
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "GoGo/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [SerializeField] private GameObject _defaultLevelPrefab;
        [SerializeField] private GameObject[] _additionalLevels;

        public GameObject DefaultLevelPrefab => _defaultLevelPrefab;
        public IReadOnlyList<GameObject> AdditionalLevels => _additionalLevels;
    }
}
```

- [ ] **Step 5.2**: Create `LevelCatalogContent.cs` (parallel to WeaponCatalogContent).
- [ ] **Step 5.3**: Create both assets, wire refs (Level1.prefab into DefaultLevelPrefab).
- [ ] **Step 5.4**: Read `GameController.cs` to find `StartGameplay()`; add `SpawnDefaultLevel(LevelCatalog)` method that does what `UIManager.PlayBtn()` did (Instantiate Level1 + set MapReady=true).
- [ ] **Step 5.5**: Add `bool MapReady` property to GameController (with public getter, set internally).
- [ ] **Step 5.6**: Read `SV_MainMenuUI.cs` — find the OnPlay handler that calls `UIManager.PlayBtn()`.
- [ ] **Step 5.7**: Add `UseLevelCatalog` flag.
- [ ] **Step 5.8**: In OnPlay, dual-path: `if (UseLevelCatalog) { GameController.SpawnDefaultLevel; } else { UIManager.PlayBtn(); }`.
- [ ] **Step 5.9**: Wire LevelCatalogContent into _GameBoot.DomainContentLoader.contents.
- [ ] **Step 5.10**: Compile, smoke check. Commit `migrate(D.6): introduce LevelCatalog + GameController.SpawnDefaultLevel`.
- [ ] **Step 5.11**: Flip flag to true. Commit `migrate(D.7): cutover to LevelCatalog`.

## Task 6: Migrate scene flags (D.8)

- [ ] **Step 6.1**: Find all readers of `UIManager.MapReady`, `UIManager.StopAllAudios`. Replace with `GameController.MapReady` / drop respectively.
- [ ] **Step 6.2**: Drop `UIManager.useSurvivorIoEndGame` — make `SV_EndGameBridge` always handle end-game.
- [ ] **Step 6.3**: Compile. Commit: `migrate(D.8): migrate scene flags to GameController`.

## Task 7: Delete legacy UIManager + _LegacyManagers + Enverement (D.9)

- [ ] **Step 7.1**: Project-wide grep for `DATN.Legacy.UIManager` references → all should be 0 except the file itself.
- [ ] **Step 7.2**: Create Editor menu item `Assets/Editor/Migration/DeleteInactiveLegacyMenuItem.cs`:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeleteInactiveLegacyMenuItem
{
    [MenuItem("Tools/Migration/Delete Inactive Legacy GOs (Phase D)")]
    public static void DeleteInactiveLegacyGOs()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        string[] targets = { "Enverement", "_LegacyManagers" };
        int deleted = 0;
        foreach (var go in roots)
        {
            foreach (var t in targets)
            {
                if (go.name == t)
                {
                    Object.DestroyImmediate(go);
                    Debug.Log($"[Migration] Deleted root GO: {t}");
                    deleted++;
                    break;
                }
            }
        }
        if (deleted > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"[Migration] Total deleted: {deleted}");
    }
}
#endif
```

- [ ] **Step 7.3**: Compile, run via `execute_menu_item(menu_path="Tools/Migration/Delete Inactive Legacy GOs (Phase D)")`.
- [ ] **Step 7.4**: Verify `manage_scene.get_active` rootCount → 8 (was 11; -3: Enverement, _LegacyManagers, plus AdsManager already gone in Phase C, +0 because GamePlay is child).
  - Actually rootCount should be 11 - 2 = 9: removing 2 ROOT GOs (Enverement + _LegacyManagers). _LegacyManagers/GamePlay is a child so not counted.
- [ ] **Step 7.5**: Delete `Assets/_Main/Scripts/UI/UIManager.cs` via `delete_script`.
- [ ] **Step 7.6**: Delete `Assets/_Main/Scripts/Gameplay/LevelsManager.cs` if no longer referenced.
- [ ] **Step 7.7**: Compile clean.
- [ ] **Step 7.8**: Commit: `migrate(D.9): delete UIManager + _LegacyManagers + Enverement + LevelsManager`.

## Task 8: Phase D close-out (D.10)

- [ ] **Step 8.1**: Update `.wiki/wiki/log.md` with Phase D entry.
- [ ] **Step 8.2**: Verify success criteria.
- [ ] **Step 8.3**: Commit: `migrate(D.10): Phase D close-out`.

---

## Success criteria

- [ ] `Grep "DATN.Legacy.UIManager"` returns 0 hits in `.cs`.
- [ ] `find_gameobjects search_term=UIManager search_method=by_component` returns only NinjaUI UIManager.
- [ ] `Assets/_Main/Data/Weapons/WeaponCatalog.asset` exists, has 12 entries.
- [ ] `Assets/_Main/Data/Levels/LevelCatalog.asset` exists.
- [ ] Scene rootCount: 11 → 9 (Enverement + _LegacyManagers deleted).
- [ ] `Assets/_Main/Scripts/UI/UIManager.cs`, `LevelsManager.cs` deleted.
- [ ] Compile clean.
- [ ] Full play loop works (user smoke test).
