---
title: UI Flow
category: systems
tags: [ui, flow, navigation]
sources: [raw/technical/ninja-ui-migration-guide.md, raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# UI Flow

How the player navigates the game's screens. Backed by NinjaUI ([[technical/ninja-ui-architecture]]).

## Boot

```
Press Play
   → UIBootstrap.Start()
   → ShowAsync(UIId.Splash)            [SV_Splash.prefab, lane Screen, ReleaseOnClose]
   → splash fill bar runs to 100%
   → ShowAsync(UIId.SV_MainMenu)        [SV_MainMenu.prefab, lane Screen, KeepLoaded]
```

## Main Menu buttons

| Button | Action | Status |
|---|---|---|
| Play | calls legacy `DATN.Legacy.UIManager.PlayBtn()` (existing gameplay-start logic) | ✓ works |
| Shop | `ShowAsync(UIId.SV_Shop)` | ✗ throws (no UIRegistry entry) — see [[open-questions#q-20260516-02]] |
| Equipment | `ShowAsync(UIId.SV_ItemEquipment)` | ✗ throws (no UIRegistry entry) |
| Settings | `ShowAsync(UIId.SV_SettingsPopup)` | ✓ works |
| Messages | falls back to legacy `SetActive` panel | ✓ via legacy |

## In-game

```
Gameplay start
  → SV_GameplayHud always visible (lane Hud, KeepLoaded)
  → Pause button → SV_PausePopup (lane Popup, PoolOnClose, force time scale 0)
  → Level threshold → SV_LevelUpPopup (lane Popup, force-pick, ESC blocked)
  → Win condition → SV_WinScreen (lane Screen, ReleaseOnClose)
  → Lose condition → SV_LoseScreen (lane Screen, ReleaseOnClose)
```

## Force-pick popup pattern

`SV_LevelUpPopup` is the only popup with `DismissByEscape = false` AND no `Close` button. Player MUST pick one of 3 options. Implementation:

1. Popup opens → `Time.timeScale = 0` in `OnBeforeShowAsync`.
2. DOTween animation uses `SetUpdate(true)` to keep animating during pause.
3. Picking option fires `Broadcaster.Send(SkillUpgradeSuccessBroadcastData)`, popup hides itself.
4. `Time.timeScale = 1` restored in `OnHiddenAsync`.

## ESC behavior

| Popup | ESC closes? |
|---|---|
| `SV_SettingsPopup` | ✓ |
| `SV_PausePopup` | ✓ |
| `SV_LevelUpPopup` | ✗ (force-pick) |

## Toast (currently unused)

Toast lane (`5_Toast`) is wired but no `Toast` UIRegistry entry yet. Available for "Item picked up!" / "Achievement unlocked" notifications later.

---
## Backlinks
- [[overview]]
- [[technical/ninja-ui-architecture]]
- [[art/ui-prefabs]]
- [[systems/skill-system]] — level-up popup hookup
- [[decisions/ninja-ui-migration]]
