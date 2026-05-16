# NinjaUI Migration Guide — DATN GoGo Survival

> **Status**: Framework integrated & compile-xanh. Scene hierarchy + UIManager configured.
> Code-side migration COMPLETE for 7 example screens. **Prefab-side wiring + UIRegistry entries
> are next** — those require Unity Editor manual work.

---

## 0. TL;DR — what changed overnight

1. **Imported new `Luzart/` package**: `UIFramework/NinjaUI` + `TweenAnimationPackage` + `NewBaseSelect` + `Attributes` + `Editor` + DOTween Pro.
2. **Removed Addressables dependency** — `DirectPrefabUIAssetProvider` is the default provider. UI prefabs go directly into `UIConfig.AssetRef`.
3. **Cleaned up redundant UI code**:
   - Old `_LuzartGame/_GameLuzart/Utility/Script/UIBase`, `UIBaseNew`, `BaseSwitchSelect`, `UIToast`, `UIItem` → deleted
   - Old `_LuzartGame/UI/` (66 reference UI files) → deleted
   - Old `_LuzartGame/LuzartTechnical/{Popup,Section,Layer,View,UIItem,UIManagerData}` → deleted
   - Old `_LuzartGame/_GameLuzart/{HeartManager,PackManager}` → deleted
   - Reference's `GameManager` orchestration folder → deleted
   - DOTween stub I had to write earlier → deleted (real DOTween Pro now in `Assets/Plugins/Demigiant/DOTween`)
4. **Scene `Assets/_Main/Scenes/GamePlay.unity`** now has a `_NinjaUI` Canvas with:
   - 6 layer roots: `0_WorldOverlay` / `1_Screen` / `2_Hud` / `3_Popup` / `4_System` / `5_Toast`
   - `UIManager` component (wired to `UIRegistry.asset`)
   - `UIInputRouter` + `UIBlockService` components
5. **`UIRegistry.asset` created** at `Assets/_Main/Data/UI/UIRegistry.asset` — currently empty, needs entries.
6. **7 example UIBase subclasses** in `Assets/_Main/Scripts/UI/NinjaUIScreens/`:
   - `SV_SplashUI` — splash with filling bar
   - `SV_MainMenuUI` — main menu
   - `SV_GameplayHudUI` — in-game HUD (HP / score / kills / coins)
   - `SV_PausePopupUI` — pause popup (with fade animation)
   - `SV_LevelUpPopupUI<SV_LevelUpData>` — 3-skill picker (Survivor.io core)
   - `SV_WinScreenUI<SV_WinData>` — win screen with stats
   - `SV_LoseScreenUI<SV_LoseData>` — game-over screen
   - `SV_SettingsPopupUI` — music/sound/vibration toggles
   - `UIBootstrap` — boot flow controller (splash → main menu)
7. **`DATN.Legacy.UIManager`** (the old monolith) still in scene on `UI` GameObject — kept for coexistence. Migrate gradually.

---

## 1. What you still need to do (1-2 days of prefab work)

### Step 1: Create UIRegistry entries

Open `Assets/_Main/Data/UI/UIRegistry.asset` in Inspector.

For each existing UI screen, click `+` to add an entry and fill:

| Field | Value (Splash example) |
|---|---|
| Id | `Splash` |
| StringId | `splash` (optional) |
| AssetRef | drag the Splash prefab from Project |
| Lane | `Screen` |
| CachePolicy | `ReleaseOnClose` |
| PreloadOnBoot | ✗ |
| AllowMultiInstance | ✗ |
| DismissByEscape | ✗ (splash auto-closes) |
| PausableWhenOverlaid | ✗ |

Cheatsheet for every existing DATN screen:

| UIId | Lane | CachePolicy | DismissByEscape | Notes |
|---|---|---|---|---|
| `Splash` | Screen | ReleaseOnClose | ✗ | Boot only |
| `SV_MainMenu` | Screen | KeepLoaded | ✗ | Hub |
| `SV_Shop` | Screen | KeepLoaded | ✓ | Heavy data |
| `SV_ItemEquipment` | Screen | KeepLoaded | ✓ | Inventory-like |
| `SV_GameplayHud` | Hud | KeepLoaded | ✗ | Persistent |
| `SV_PausePopup` | Popup | PoolOnClose | ✓ | Light |
| `SV_LevelUpPopup` | Popup | PoolOnClose | **✗ FORCE PICK** | Survivor.io core |
| `SV_SettingsPopup` | Popup | PoolOnClose | ✓ | — |
| `SV_WinScreen` | Screen | ReleaseOnClose | ✗ | End-game |
| `SV_LoseScreen` | Screen | ReleaseOnClose | ✗ | End-game |
| `Toast` | Toast | PoolOnClose | ✗ | Quick message |
| `Loading` | System | KeepLoaded | ✗ | Persistent system |

### Step 2: Convert existing prefabs to UIBase prefabs

For each existing UI prefab (e.g., the current Splash screen GameObject under DATN's `UI` Canvas):

1. **Duplicate** the existing prefab (don't break the legacy scene reference yet).
2. On the duplicate's ROOT GameObject:
   - REMOVE the old MonoBehaviour (`SplashManager`, `MainMenu`, `MainMenuManager`, `ShopManager`, ...).
   - ADD the corresponding `SV_*UI` MonoBehaviour from `_Main/Scripts/UI/NinjaUIScreens/`.
3. Kéo-thả các UI element references (Image, Text, Button) vào fields trên SV_*UI component.
4. Drag the prefab into `UIRegistry.AssetRef` for that UIId.
5. **Move the prefab Project asset reference** — keep it in `Assets/_Main/Perfabes/UI/`.
6. Make sure the prefab root has:
   - `RectTransform` (stretch anchors recommended).
   - `CanvasGroup` (if you want fade animation — required by Pause/LevelUp/Win/Lose).

### Step 3: Add a UIBootstrap to scene

In scene `GamePlay.unity`:
1. Create empty GameObject `UIBootstrap`.
2. Add component `UIBootstrap` (from `_Main/Scripts/UI/NinjaUIScreens/UIBootstrap.cs`).
3. Inspector: `showSplash=true`, `showMainMenu=true`, `splashId=Splash`, `mainMenuId=SV_MainMenu`.

When you press Play, it should: instantiate Splash prefab → wait for fill → instantiate MainMenu prefab.

### Step 4: Decommission DATN.Legacy.UIManager (gradual)

The old `Assets/_Main/Scripts/UI/UIManager.cs` (now in `namespace DATN.Legacy`) is still attached to the existing `UI` GameObject. Disable it gradually as you migrate:

1. Convert MainMenu → uses `SV_MainMenuUI` → remove MainMenu button handlers from `DATN.Legacy.UIManager`.
2. Convert HUD → `SV_GameplayHudUI` → remove HUD field-update code from `DATN.Legacy.UIManager.Update()`.
3. Convert Pause → `SV_PausePopupUI` → remove `Pause()/Resume()` from legacy.
4. When all screens migrated → delete `DATN.Legacy.UIManager` + `ControllerUI.cs` + `MainMenuManager.cs`.

---

## 2. Calling NinjaUI from gameplay code

### Show a screen (no data)
```csharp
using Cysharp.Threading.Tasks;
using Luzart;

async UniTask OpenMainMenu() {
    await UIManager.Instance.ShowAsync(UIId.SV_MainMenu);
}
```

### Show a popup with data
```csharp
async UniTask ShowLevelUpPicker(List<Data_UpgradeSkill> rolled, System.Action<Data_UpgradeSkill> onPicked) {
    await UIManager.Instance.ShowAsync(
        UIId.SV_LevelUpPopup,
        new UIContext(new SV_LevelUpData { Options = rolled, OnPicked = onPicked }));
}
```

### Hide top popup
```csharp
await UIManager.Instance.CloseTopPopupAsync();
```

### Hide everything (e.g., before level transition)
```csharp
await UIManager.Instance.HideAllExceptSystemAsync();
```

### Toast a quick message
```csharp
UIManager.Instance.ShowToastAsync("Item picked up!", ToastStyle.Info, duration: 2f).Forget();
```

### Push input blocker (during loading)
```csharp
using (UIManager.Instance.PushBlock("loading_level")) {
    await DataManager.Instance.LoadLevelAsync();
}
// Block auto-released when using block exits.
```

---

## 3. Wiring example — UpgradeSkillManager (the Level Up flow)

Currently `_LuzartGame/Gameplay/System/UpgradeSkillManager.cs` has:

```csharp
// TODO(NinjaUI migration): replace with UIManager.Instance.ShowAsync<UISkillUpgradePopup>(UIId.SkillUpgrade, ...)
// var popupService = SceneRootManager.Instance._domain.GetService<PopupService>();
// if (popupService != null) { popupService.ShowPopup<...>(...); }
_isUpgrading = true;
Time.timeScale = 0;
```

Replace the commented block with:

```csharp
var data = new SV_LevelUpData {
    Options = data_UpgradeSkills,
    OnPicked = picked => {
        Broadcaster.Send(new SkillUpgradeSuccessBroadcastData { SkillConfig = picked.SkillConfig });
    }
};
UIManager.Instance.ShowAsync<SV_LevelUpPopupUI>(
    UIId.SV_LevelUpPopup,
    new UIContext(data)).Forget();
_isUpgrading = true;
Time.timeScale = 0;
```

---

## 4. Architecture summary

```
Gameplay code
   ↓ UIManager.Instance.ShowAsync(UIId.X, ctx)
   ↓
UIManager (NinjaUI core)
   ├─ Registry.TryGet(id) → UIConfig
   ├─ AssetProvider.LoadAsync(config) → Prefab  (DirectPrefab = direct reference, no Addressables)
   ├─ Instantiate prefab under correct lane root (Screen/Popup/...)
   ├─ Call view.OnCreateAsync → OnBeforeShowAsync → AnimateShowAsync → OnShownAsync
   └─ Return UIHandle for caller to track / hide
```

```
Scene hierarchy after setup:
GamePlay
├── _NinjaUI (Canvas, sortOrder=100)
│   ├── 0_WorldOverlay  ← UILayer.WorldOverlay
│   ├── 1_Screen        ← UILayer.Screen
│   ├── 2_Hud           ← UILayer.Hud
│   ├── 3_Popup         ← UILayer.Popup
│   ├── 4_System        ← UILayer.System
│   └── 5_Toast         ← UILayer.Toast
├── UI (existing — DATN.Legacy.UIManager, will be deprecated)
├── GameManager, AudioManager, ...
└── EventSystem
```

---

## 5. Folder structure final

```
Assets/
├── Luzart/                           ← NEW package imported by user
│   ├── UIFramework/NinjaUI/Runtime/  ← UI framework code (don't modify)
│   ├── TweenAnimationPackage/        ← Replaces old TweenAnimation
│   ├── NewBaseSelect/                ← Clean BaseSelect (no Odin)
│   ├── Attributes/                   ← Custom attrs (LabelText, Conditional, ...)
│   ├── Editor/                       ← Property drawers for attrs
│   └── AssetModifier/                ← Bulk asset modification tool
├── Plugins/Demigiant/DOTween/        ← Real DOTween Pro
├── _Main/
│   ├── Scripts/
│   │   ├── _LegacyCompat/            ← Stubs for compile compat (delete when subsystems migrate)
│   │   │   ├── SkillData.cs          (Equipment.linkedStartingSkill type)
│   │   │   ├── SkillEnums.cs         (PassiveStatType enum)
│   │   │   └── _FrameworkStubs.cs    (IView, IBroadcastData, Data_ClassicEndGame, ...)
│   │   ├── _LuzartGame/              ← Framework code copied from reference
│   │   │   ├── Animation, Cost, DependencyInjection, Entity, Items,
│   │   │   │   LuzartTechnical, Skills, System, EditorItem
│   │   │   └── _GameLuzart/Utility   ← utility classes (kept ones, deleted UI-only)
│   │   ├── UI/                       ← DATN's UI scripts
│   │   │   ├── NinjaUIScreens/       ← NEW NinjaUI subclasses (7 wrappers + bootstrap)
│   │   │   └── (legacy: SplashManager.cs, MainMenu.cs, ... — keep until migrated)
│   │   └── (rest of DATN: Audio, Cheat, Core, Data, Editor, Enemy, Equipment, Gameplay, Player, Weapons)
│   ├── Data/UI/UIRegistry.asset      ← NEW UI config (empty, dev fills entries)
│   ├── Scenes/GamePlay.unity         ← Modified: added _NinjaUI hierarchy
│   └── (rest unchanged)
└── Joystick Pack/                    ← copied from reference for joystick input
```

---

## 6. Known gotchas

1. **DOTween extension methods**: requires `using DG.Tweening;` at top of file. Already added in all `SV_*` examples.
2. **`OnCloseRequested` is internal** to NinjaUI assembly — use `OnCloseButtonClicked()` from your subclass instead.
3. **UIBase prefab MUST have a UIBase component on root**. If you forget, `UIManager.AcquireInstanceAsync` will Destroy the instance and throw.
4. **CanvasGroup not required** for framework — but most animation overrides assume it. Add CanvasGroup to root when you override `AnimateShowAsync/AnimateHideAsync`.
5. **`DontDestroyOnLoad`**: UIManager auto-DDOL if it's at scene root. `_NinjaUI` is currently at root → ✅ OK.
6. **Time.timeScale**: Pause / LevelUp screens set `Time.timeScale = 0` in `OnBeforeShowAsync`. Their DOTween animations use `SetUpdate(true)` to bypass timescale.
7. **Legacy DATN.Legacy.UIManager still functions** during migration. The `using DATN.Legacy;` was bulk-added to 12 files that reference it. You can search `using DATN.Legacy` to find all consumers.

---

## 7. Migration checklist for each remaining screen

For SHOP:
- [ ] Find existing Shop prefab (referenced by `ShopManager.cs` or `MainMenuManager`).
- [ ] Create `SV_ShopUI : UIBase` (or `UIBase<SV_ShopData>` if needs server-pushed data).
- [ ] Copy button-click handlers from old `ShopManager` → new `SV_ShopUI.OnCreateAsync`.
- [ ] Add entry to UIRegistry.asset with `Id=SV_Shop`.
- [ ] Replace `Btn.SetActive(true)` callers with `await UIManager.Instance.ShowAsync(UIId.SV_Shop)`.

For each: SELECT MAP, EQUIPMENT, MESSAGES, FLOATING BTNs, DEATH SCREEN — same pattern.

---

## 8. Test plan after migration

1. **Boot test**: Press Play → should see Splash → MainMenu (no exceptions in Console).
2. **Open popup**: Click "Settings" on MainMenu → SettingsPopup shows with fade.
3. **ESC test**: Press ESC on SettingsPopup → closes. Press ESC on LevelUp → ignored (force pick).
4. **Stack test**: Open Shop → open Settings on top → close Settings → Shop still visible.
5. **HUD test**: Start game → HUD shows. HP bar updates on damage. Pause button opens PausePopup. Pause freezes Time.timeScale.
6. **End-game test**: Trigger Win → WinScreen shows with stats. Click Continue → back to MainMenu.

---

## 9. Where things live (cheat-sheet)

- **Add UI ID**: edit `Assets/Luzart/UIFramework/NinjaUI/Runtime/Core/UIId.cs`.
- **Register UI**: edit `Assets/_Main/Data/UI/UIRegistry.asset`.
- **Write new UIBase subclass**: drop file in `Assets/_Main/Scripts/UI/NinjaUIScreens/`.
- **Hook gameplay → UI**: call `UIManager.Instance.ShowAsync(...)` from anywhere.
- **Boot flow**: edit `UIBootstrap.cs` or override `StartFlow`.
- **Animation**: use DG.Tweening (DOTween Pro) — see `SV_PausePopupUI.AnimateShowAsync` for example.
