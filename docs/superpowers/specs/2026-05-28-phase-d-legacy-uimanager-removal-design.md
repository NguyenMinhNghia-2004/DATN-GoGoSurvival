# Phase D — Remove `DATN.Legacy.UIManager`

- **Status**: Draft, awaiting user approval
- **Parent**: `2026-05-28-luzart-migration-master-roadmap.md`
- **Created**: 2026-05-28
- **Risk**: Medium
- **Prerequisite**: Phase C complete

## 1. Outcome

After Phase D:

- `DATN.Legacy.UIManager` (the legacy UI/data hub at `_LegacyManagers/_LegacyUIScripts`) is deleted: GameObject removed, `UIManager.cs` file deleted, `DATN.Legacy` namespace footprint reduced.
- All data it held is replaced with ScriptableObjects:
  - 12 weapon refs (prefabs + sprites + name + description) → `WeaponCatalog` SO.
  - Level1 prefab ref (the only live use of `LevelsManager`) → `LevelCatalog` SO.
  - Scene flags (`MapReady`, `StopAllAudios`, `useSurvivorIoEndGame`) → either inlined into `GameController` or moved to a `MigrationSettings` SO.
- `PlayBtn()` logic absorbed into `GameController.StartGameplay()`.
- All callers that previously did `FindFirstObjectByType<DATN.Legacy.UIManager>()` are rewritten to consume the SO via `Domain.Get<...>()` or direct SO reference.
- Game still plays identically.

## 2. Inventory — current legacy UIManager usage

From `Grep "UIManager" Assets/_Main/Scripts --type cs`, callers that reference the legacy `DATN.Legacy.UIManager` (not the NinjaUI `Luzart.UIFramework.UIManager`):

| Caller | What it reads / calls |
|---|---|
| `SV_MainMenuUI.OnPlay()` | `UIManager.Instance.PlayBtn()` (sets MapReady, instantiates Level1) |
| `BoltSHooter` | `FindFirstObjectByType<UIManager>()` → reads `Manager.EnemyAvailable` for homing target list |
| `EnemyManager` (on Zombie prefab) | Same find pattern → reads scene flags |
| `AudioCheckerPlayer` | Reads `UIManager.StopAllAudios` (already in Phase C delete list) |
| `CheatPanel` | Inspector-injected `UIManager` field |
| `ManagerWeapons` | (in Phase C deleted, but verify) |
| `GameManager` | Inspector field `UI` → `UIManager` |
| `SpawenManager`, `ControllerSpawening` | Inspector field |
| `LocalisationPresent` (deleted in Phase C) | — |
| `DiamondVip`, `CoinsManager` | Reads `UIManager.Instance.MapReady` |

Inspector-only refs (no code call) can be cleared by setting the field to null before delete. Code calls need their own switch.

## 3. New types introduced

### 3.1 `WeaponCatalog` (ScriptableObject)

```csharp
// Assets/_Main/Scripts/_LuzartGame/LuzartTechnical/DataResources/Definition/WeaponCatalog.cs
namespace Luzart
{
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "GoGo/Weapon Catalog")]
    public class WeaponCatalog : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string id;             // "Gun", "SpinerA", "Ball", "Rocket", "DroneA"…
            public GameObject prefab;
            public Sprite sprite;
            public string displayName;
            [TextArea] public string description;
        }

        public Entry[] entries;

        public Entry? Find(string id);
    }
}
```

Asset path: `Assets/_Main/Data/Weapons/WeaponCatalog.asset`. Authoring: copy values from current `SpriteWeapons` MonoBehaviour fields.

### 3.2 `LevelCatalog` (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "GoGo/Level Catalog")]
public class LevelCatalog : ScriptableObject
{
    public GameObject defaultLevelPrefab;        // currently Level1
    public GameObject[] additionalLevels;        // Level2..Level6 (reserved for SelectMap port)
}
```

Asset path: `Assets/_Main/Data/Levels/LevelCatalog.asset`.

### 3.3 `MigrationSettings` (ScriptableObject) — optional

If we keep the toggle `useSurvivorIoEndGame`, it goes here. Default: drop the toggle entirely (set behaviour to "always use SV_LoseScreen") since the alternative legacy FinishScreen no longer exists.

### 3.4 New helper: `GameController.StartGameplay()` extension

Absorb the body of `UIManager.PlayBtn()`:

1. Resolve `LevelCatalog` (from Domain or Inspector field on `GameController`).
2. `Instantiate(catalog.defaultLevelPrefab)`.
3. Set framework flag `MapReady = true` (move to `GameController` state).
4. Existing `StartGameplay()` work continues (start time, subscribe events, etc.).

## 4. Slice plan

### Slice D.1 — Create `WeaponCatalog` SO + asset

- Create `WeaponCatalog.cs`.
- Right-click → create `WeaponCatalog.asset` in `Data/Weapons/`.
- Manually copy 12 entries from `SpriteWeapons` MonoBehaviour (or write an Editor utility menu item to one-shot dump).
- Compile, smoke-test (no behaviour change — asset only).
- Commit: `migrate(D.1): introduce WeaponCatalog SO with 12 entries`.

### Slice D.2 — Inject WeaponCatalog into Domain

- Add a `[SerializeField] WeaponCatalog _weaponCatalog;` field on `_GameBoot.DomainContentLoader` (or a new `WeaponCatalogContent : AbstractScriptableContent` if cleaner).
- In `DoInject`/`DoInitialize`, register the catalog into Domain.
- Add a `MigrationFlags.UseWeaponCatalog` flag (default `false`).
- Commit: `migrate(D.2): wire WeaponCatalog into Domain (flag off)`.

### Slice D.3 — Migrate callers to read from catalog

For each caller in §2 that reads weapon refs (`SpriteWeapons` fields via `UIManager.Weapons.*`):

- Wrap in `if (MigrationFlags.UseWeaponCatalog) { read from Domain.Get<WeaponCatalog>() } else { legacy read }`.
- Verify both paths return same prefab/sprite.

Commit: `migrate(D.3): dual-path weapon resolution via flag`.

### Slice D.4 — Cutover: flag on by default

- Set `MigrationFlags.UseWeaponCatalog = true`.
- Play-test thoroughly: spawn each weapon type at least once.
- Commit: `migrate(D.4): cutover to WeaponCatalog as default`.

### Slice D.5 — Delete legacy weapon paths + `SpriteWeapons` component

- Remove the `else` branches in callers.
- Remove `SpriteWeapons` MonoBehaviour from `Controller` GameObject (still keep the script file if any prefab refs it — verify).
- If `SpriteWeapons.cs` becomes orphaned, delete it.
- Remove `MigrationFlags.UseWeaponCatalog` (slice closed).
- Commit: `migrate(D.5): delete SpriteWeapons component + legacy paths`.

### Slice D.6 — Create LevelCatalog + absorb PlayBtn

- Create `LevelCatalog.cs` + `LevelCatalog.asset`. Drop Level1 prefab into `defaultLevelPrefab`.
- Add method `GameController.SpawnDefaultLevel()` that does what `PlayBtn` did.
- Add `MigrationFlags.UseLevelCatalog` flag.
- Switch `SV_MainMenuUI.OnPlay()` to call `GameController.StartGameplay()` (already does — verify) and remove the `UIManager.PlayBtn()` call behind the flag.
- Commit: `migrate(D.6): introduce LevelCatalog + GameController.SpawnDefaultLevel`.

### Slice D.7 — Cutover + clean up `LevelsManager` field

- Default flag on. Play-test the Play button flow.
- `LevelsManager` MonoBehaviour on `GameManager` GameObject: remove if no other reader.
- Delete `LevelsManager.cs` if orphaned.
- Commit: `migrate(D.7): cutover to LevelCatalog, drop LevelsManager`.

### Slice D.8 — Migrate scene flags

`MapReady`, `StopAllAudios`, `useSurvivorIoEndGame`:

- `MapReady` → `GameController.MapReady` property. Update readers (`DiamondVip`, `CoinsManager`).
- `StopAllAudios` → either fold into `GameController.IsGameStopping` event or drop if AudioCheckerPlayer is dead (Phase C).
- `useSurvivorIoEndGame` → drop the toggle (default behaviour wins).

Commit: `migrate(D.8): migrate UIManager scene flags into GameController`.

### Slice D.9 — Delete legacy `UIManager.cs` + `_LegacyUIScripts` GO

- Verify zero references via project-wide grep.
- Clear Inspector fields on any GameObject still pointing at the soon-deleted `UIManager`.
- Delete the `_LegacyUIScripts` child GameObject from scene.
- Delete `Assets/_Main/Scripts/UI/UIManager.cs`.
- The parent `_LegacyManagers` GameObject now contains zero children → delete it too.
- Commit: `migrate(D.9): delete DATN.Legacy.UIManager + _LegacyManagers root`.

### Slice D.10 — Phase D close-out

- Update wiki log entry.
- Verify success criteria (§5).
- Commit: `migrate(D.10): Phase D close-out`.

## 5. Success criteria

- [ ] `Grep "DATN.Legacy.UIManager"` returns 0 hits across `.cs` files.
- [ ] `find_gameobjects search_term=UIManager search_method=by_component` returns only the NinjaUI `Luzart.UIFramework.UIManager` (on `_NinjaUI`).
- [ ] `Assets/_Main/Data/Weapons/WeaponCatalog.asset` and `Assets/_Main/Data/Levels/LevelCatalog.asset` exist with full data.
- [ ] `_LegacyManagers` root GameObject is gone.
- [ ] `SpriteWeapons.cs` and `LevelsManager.cs` deleted (assuming no other prefab refs).
- [ ] Full play loop works.

## 6. Out of scope

- Touching `BooleanManager` (deferred to end-of-migration cleanup or Phase F).
- Touching `DataManager` or `CurrencyManager` — they own currency, not UI.
- Touching `AudioManager` — Phase F or post-migration.

## 7. Risks

| Risk | Mitigation |
|---|---|
| Hidden Inspector ref to `UIManager` in a prefab not yet audited | Slice D.2 includes a project-wide GUID grep for the `UIManager.cs.meta` GUID before any code-side cutover |
| `EnemyAvailable` (read by BoltSHooter/EnemyManager) is owned by `GameManager`, not `UIManager` — verify before assuming D handles it | Confirm in Slice D.3 audit. If owned by `GameManager`, this concern moves to Phase F |
| `SpriteWeapons.cs` is referenced by `SV_GameplayHudUI` to render weapon icons | Search before delete — likely Phase F concern; keep `SpriteWeapons.cs` if so |
| Cutover commit (D.4 or D.7) breaks Inspector serialization of an unreplaced field | Each cutover commit ends with manual scene-save + scene-reopen to flush any phantom missing refs |

## 8. Decisions

- **`LevelCatalog` uses direct prefab refs**, not Addressables — keeps current `DirectPrefabUIAssetProvider` pattern (matches NinjaUI choice in the project).
- **`useSurvivorIoEndGame` toggle is dropped**, not preserved — the legacy FinishScreen alternative is already inactive in the scene.
