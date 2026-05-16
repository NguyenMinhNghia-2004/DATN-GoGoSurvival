---
title: NinjaUI Architecture
category: technical
tags: [ui, framework, ninja-ui]
sources: [raw/technical/ninja-ui-migration-guide.md]
created: 2026-05-16
updated: 2026-05-16
---

# NinjaUI Architecture

How the `Luzart.UIFramework.NinjaUI` runtime is wired in this project. Framework code lives at `Assets/Luzart/UIFramework/NinjaUI/Runtime/` — treat as read-only.

## Lanes (sub-canvases under `_NinjaUI`)

| GameObject | UILayer enum | Purpose |
|---|---|---|
| `0_WorldOverlay` | `WorldOverlay` | World-space markers, damage numbers |
| `1_Screen` | `Screen` | Full-screen views (MainMenu, Shop, Win/Lose) |
| `2_Hud` | `Hud` | In-game HUD (always-on during play) |
| `3_Popup` | `Popup` | Modal/transient popups (Pause, Settings, LevelUp) |
| `4_System` | `System` | Loading spinners, fatal-error overlays |
| `5_Toast` | `Toast` | Brief auto-dismiss messages |

## Registry → asset provider → instance

```
Gameplay code
   ↓ UIManager.Instance.ShowAsync(UIId.X, ctx)
   ↓
UIManager
   ├─ Registry.TryGet(id) → UIConfig          (UIRegistry.asset lookup)
   ├─ AssetProvider.LoadAsync(config) → Prefab (DirectPrefabUIAssetProvider, no Addressables)
   ├─ Instantiate prefab under correct lane root
   ├─ Lifecycle: OnCreateAsync → OnBeforeShowAsync → AnimateShowAsync → OnShownAsync
   └─ Return UIHandle to caller
```

## UIConfig fields (per registry entry)

| Field | Example | Notes |
|---|---|---|
| `Id` | `UIId.SV_LevelUpPopup` | Enum in `Luzart/.../Core/UIId.cs` |
| `StringId` | `"levelup"` (optional) | Alt lookup |
| `AssetRef` | `SV_LevelUpPopup.prefab` reference | Direct, no Addressables |
| `Lane` | `Popup` | Which root to instantiate under |
| `CachePolicy` | `PoolOnClose` | `KeepLoaded`, `PoolOnClose`, `ReleaseOnClose` |
| `PreloadOnBoot` | `false` | Eager-load at boot |
| `AllowMultiInstance` | `false` | Allow stacking same UI twice |
| `DismissByEscape` | `false` (LevelUp = force-pick) | Hard rule for force-choice popups |
| `PausableWhenOverlaid` | `false` | Auto-pause underlying UI |

See [[art/ui-prefabs]] for the current 9 registered entries.

## UIBase subclass pattern

Each screen has a `SV_*UI : UIBase` (or `UIBase<TData>` for data-driven popups). Convention in this project (`Assets/_Main/Scripts/UI/NinjaUIScreens/`):

```csharp
public class SV_SettingsPopupUI : UIBase
{
    [SerializeField] Toggle musicToggle;
    [SerializeField] Toggle soundToggle;
    [SerializeField] Toggle vibrationToggle;
    [SerializeField] Button closeButton;

    protected override UniTask OnCreateAsync() { /* wire button events */ return UniTask.CompletedTask; }
    protected override UniTask AnimateShowAsync() { /* DOTween fade in */ }
}
```

Popups that need input data subclass `UIBase<TData>`:

```csharp
public class SV_LevelUpPopupUI : UIBase<SV_LevelUpData> { ... }

// Caller:
UIManager.Instance.ShowAsync(UIId.SV_LevelUpPopup,
    new UIContext(new SV_LevelUpData { Options = rolled, OnPicked = cb }));
```

## Animation hooks

`AnimateShowAsync` / `AnimateHideAsync` are virtuals. Most popups use DOTween Pro (`DG.Tweening`) with `SetUpdate(true)` to bypass `Time.timeScale = 0` (Pause/LevelUp freeze game time but the popup still animates).

CanvasGroup is required on prefab root for fade animations.

## Gotchas to remember

1. `OnCloseRequested` is `internal` to NinjaUI assembly — subclasses call `OnCloseButtonClicked()` instead.
2. UIBase prefab MUST have a `UIBase` subclass on root or `AcquireInstanceAsync` will Destroy + throw.
3. `_NinjaUI` is at scene root → `UIManager` auto-`DontDestroyOnLoad`s itself. Survives scene transitions.
4. Time.timeScale interactions: any animation that should keep running during pause needs `SetUpdate(true)`.

See [[sources/ninja-ui-migration-guide]] §6 for the full gotcha list.

---
## Backlinks
- [[overview]]
- [[decisions/ninja-ui-migration]]
- [[systems/ui-flow]]
- [[art/ui-prefabs]]
