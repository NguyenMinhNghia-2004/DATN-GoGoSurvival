---
title: UI Prefabs (9 SV_*)
category: art
tags: [ui, prefab, ninja-ui]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# UI Prefabs

9 prefabs at `Assets/_Main/Perfabes/UI/`, all built programmatically during the overnight batch. Each prefab is registered in `Assets/_Main/Data/UI/UIRegistry.asset` and wired to a `SV_*UI` MonoBehaviour in `Assets/_Main/Scripts/UI/NinjaUIScreens/`.

## Registry table

| Prefab | UIId | Lane | Cache | Visuals | Status |
|---|---|---|---|---|---|
| `SV_Splash.prefab` | `Splash` | Screen | ReleaseOnClose | Background.png + Logo.png + fill bar | ✓ Wired |
| `SV_MainMenu.prefab` | `SV_MainMenu` | Screen | KeepLoaded | Placeholder solid colors | ✓ Functional, art polish later |
| `SV_GameplayHud.prefab` | `SV_GameplayHud` | Hud | KeepLoaded | HP bar + score + kills/coins + reload | ✓ Wired |
| `SV_PausePopup.prefab` | `SV_PausePopup` | Popup | PoolOnClose | Backdrop + 3 buttons + DOTween fade | ✓ |
| `SV_SettingsPopup.prefab` | `SV_SettingsPopup` | Popup | PoolOnClose | 3 toggles (Music / Sound / Vibration) + Close | ✓ |
| `SV_LevelUpPopup.prefab` | `SV_LevelUpPopup` | Popup | PoolOnClose | VerticalLayoutGroup of 3 `SV_LevelUpSlot`s | ✓ Force-pick |
| `SV_LevelUpSlot.prefab` | (child) | — | — | Icon + Name + Level + Desc + Button | ✓ Auto-binds via [[systems/skill-system]] catalog |
| `SV_WinScreen.prefab` | `SV_WinScreen` | Screen | ReleaseOnClose | Stats panel + 2 buttons | ✓ |
| `SV_LoseScreen.prefab` | `SV_LoseScreen` | Screen | ReleaseOnClose | Stats panel + 2 buttons | ✓ |

UI flow that uses these is in [[systems/ui-flow]].

## Polish status

Most prefabs are functional but visually minimal — placeholder solid colors stand in for art. Splash is the most "finished" (real Background + Logo wired). HUD layout is final; MainMenu/Win/Lose need art passes.

## Not yet built

| UIId expected | Status |
|---|---|
| `SV_Shop` | ✗ no prefab, no registry entry — see [[open-questions#q-20260516-02]] |
| `SV_ItemEquipment` | ✗ same |
| `Toast` (generic) | ✗ lane exists, no UI prefab |
| `Loading` (system overlay) | ✗ |

These will throw when MainMenu Shop/Equipment buttons are clicked until added to UIRegistry.

---
## Backlinks
- [[overview]]
- [[systems/ui-flow]]
- [[technical/ninja-ui-architecture]]
- [[decisions/ninja-ui-migration]]
- [[systems/skill-system]] — LevelUpSlot binds from skill catalog
