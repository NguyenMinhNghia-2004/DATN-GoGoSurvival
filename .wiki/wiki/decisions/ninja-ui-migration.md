---
title: Replace legacy UIManager with NinjaUI
category: decisions
tags: [ui, framework, migration]
sources: [raw/technical/ninja-ui-migration-guide.md, raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Replace legacy UIManager with NinjaUI

## Decision
**Date**: 2026-05-15 (start) / 2026-05-16 (M2+M5 done)
**Decided by**: developer
**Status**: active (gradual decommission, ~60% migrated)

### Context

The original `Assets/_Main/Scripts/UI/UIManager.cs` was a 1500-line monolith doing: scene-wide `SetActive(true/false)` toggling, hard-coded references to every screen, manual stack management, mixed gameplay + UI logic. Adding a new screen meant editing 3-4 places and risking breaking unrelated flows.

A reference codebase (`_LuzartGame/`) shipped with the `Luzart.UIFramework.NinjaUI` package: lane-based root (Screen / Hud / Popup / System / Toast / WorldOverlay), `UIRegistry` ScriptableObject for `UIId → prefab` mapping, async show/hide lifecycle, `UIBase` subclass-per-screen, DOTween animation hooks.

### Options considered

1. **Refactor legacy UIManager in-place** — pros: minimal disruption; cons: still a monolith afterward, no lane/lifecycle benefits.
2. **Adopt NinjaUI fully (big bang)** — pros: clean end state; cons: breaks every screen at once.
3. **Adopt NinjaUI gradually, screens coexist with legacy** — pros: continuous compile, can demo intermediate state; cons: two UI systems live in scene at the same time during transition.

### Decision

**Option 3 — gradual coexistence.** The old `UIManager` is moved to `namespace DATN.Legacy` and stays attached to the existing `UI` GameObject in the scene. The new `_NinjaUI` Canvas is added alongside. Each screen is migrated one at a time: duplicate prefab → swap MonoBehaviour to `SV_*UI : UIBase` → register in `UIRegistry.asset` → remove handler from legacy UIManager.

### Consequences

- 9 screens migrated overnight (see [[art/ui-prefabs]]): Splash, MainMenu, GameplayHud, Pause, Settings, LevelUp, LevelUpSlot, Win, Lose.
- 7 `SV_*UI` C# wrappers in `Assets/_Main/Scripts/UI/NinjaUIScreens/`.
- `UIBootstrap` GameObject drives Splash → MainMenu auto-flow on scene load.
- `UpgradeSkillManager` (M5) now calls `UIManager.Instance.ShowAsync(UIId.SV_LevelUpPopup, ...)` instead of legacy.
- **Still on legacy**: Shop, Equipment, Messages screens. MainMenu buttons currently route Shop/Equipment to NinjaUI IDs that have no registry entries — see [[open-questions#q-20260516-02]].
- Addressables dependency removed: `DirectPrefabUIAssetProvider` is default → prefabs go directly into `UIConfig.AssetRef`. Simpler than addressables for a thesis-scale project.
- Coexistence risk: two `EventSystem`s, two stacking models → see [[open-questions#q-20260516-05]] for whether to fully delete legacy before thesis defense.

> [!tip] Migration recipe
> See [[sources/ninja-ui-migration-guide]] §1 Step 2 for the per-prefab conversion pattern. Each new screen is mechanical once the pattern is internalized: ~30 minutes per screen.

---
## Backlinks
- [[overview]]
- [[systems/ui-flow]]
- [[technical/ninja-ui-architecture]]
- [[art/ui-prefabs]]
- [[claims#c-20260516-01]]
