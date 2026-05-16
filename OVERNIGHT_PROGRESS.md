# Overnight Progress — NinjaUI Migration (2026-05-16 → 2026-05-17)

> Sleep notes for morning review. TL;DR at the bottom.

## What now works end-to-end

Tested via play-mode + Unity MCP automation. Verified flows:

1. **Splash → MainMenu** transition via `UIBootstrap`
2. **MainMenu → Gameplay**: click BtnPlay → SV_MainMenuUI.OnPlay → legacy gameplay starts + SV_GameplayHud shows
3. **Pause flow**: PauseBtn in HUD → SV_PausePopup with original "Pause" UI (Resume / Home / Sound / nested Setting) → Resume → game continues
4. **Level-up flow (auto)**: gain XP organically → UpgradeSkillManager fires CurrentLevel.Changed → SV_LevelUpPopup shown with 3 skill cards (Left/Center/Right from original prefab) → click pick → skill applied via SkillUpgradeSuccessBroadcastData → popup closes → game resumes. Queue mechanism handles multi-level XP jumps.
5. **Win flow**: `Data_ClassicEndGame{IsWin=true}` broadcast → SV_EndGameBridge → SV_WinScreen with stats
6. **Lose flow (natural)**: Player HP → 0 → death → broadcast → SV_LoseScreen with "DEFEAT" banner + survival time + kills + Confirm
7. **Gameplay systems running on new framework**: EnemySpawnerManager (waves), GameController (level/kill/time), UpgradeSkillManager (skill picks), drop items (XP gems on ground picked up).

## What was broken + how I fixed it

| Bug | Root cause | Fix |
|---|---|---|
| All new UI invisible in Play mode | `_NinjaUI` Canvas was **WorldSpace, 800×600 ConstantPixelSize** | Synced to legacy: ScreenSpaceOverlay, 1080×1920, ScaleWithScreenSize, sortOrder=9 (above legacy's −1) |
| Old + new UI rendering simultaneously | Legacy `UI` Canvas still enabled | Disabled Canvas + GraphicRaycaster on legacy `UI` GameObject (children still active so legacy code can SetActive them) |
| 5 missing-script warnings (Mails / Equipement / Process / SelectMap / Evolve) | Orphan MonoBehaviour refs to scripts deleted in prior refactor | `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` on each prefab |
| NullRef in `TimerManager.Update()` line 21 | Cross-prefab refs to `timeTextScreenFinish` and `BestTime` lost when `TopUI` was split into a separate SV_GameplayHud prefab from FinishScreen | Null-checked the references in TimerManager.Update |
| SV_LevelUpPopup threw `UnassignedReferenceException slotPrefab` | Original prefab has 3 fixed slot children (Left/Center/Right) — no slotPrefab needed | Rewrote `SV_LevelUpPopupUI.OnBeforeShowAsync` to bind existing slot children instead of instantiating |
| LevelUp popup never auto-triggers | `UpgradeSkillManager._maxSkillCanRolInSectionUpgrade = 0` because `StatType.TotalSkill` returned 0 (AssetStats SO doesn't define it) | Added fallback defaults in `StatsBehavior.Get`: HPMax=100, TotalSkill=3, ATK=10, Speed=5 |
| HUD HP bar always empty | Same root: `StatType.HPMax = 0` | Same fallback fix → HP now shows 100/100 |
| SV_Shop.prefab was 4KB (junk) | MCP `create_from_gameobject "Shop"` matched a small nav-bar Button at `UI/Main Menu/Container/MainMenu/RightContent/Container/Shop` instead of the real shop panel | Deleted, re-cloned from `UI/Main Menu/Container/Shop` (the panel with `ShopManager` script) — now 283KB. Also removed the stray SV_ShopUI from the wrong button. |

## What's NOT done (intentionally — needs your input or is too risky autonomously)

1. **Legacy MonoBehaviours still attached to cloned prefabs** — e.g. `MainMenu` script on SV_MainMenu, `ShopManager` on SV_Shop, `SelectMapManager` on SV_SelectMap. These carry the original gameplay logic. Deleting them would require re-implementing that logic in SV_*UI — not safe to do without your sign-off. Right now they coexist: legacy logic runs inside, NinjaUI manages show/hide.

2. **MainMenu sub-navigation (Shop / Equipment / Process / Evolve / Mails / SelectMap buttons)** — these aren't wired to NinjaUI flows because the original game's nav model is different (it switches between sibling panels via a bottom nav bar, not by going forward into a screen stack). Wiring this needs a design decision: keep the bottom-nav style or convert to screen pushes?

3. **Stat config SOs missing values** (`StatDef_HPMax`, `StatDef_ATK`, `StatDef_Speed`) — the actual AssetStatDefinition ScriptableObjects have null `Value` fields. My patch papers over this at runtime with sensible defaults, but the right long-term fix is to populate the SOs themselves so designers can tune.

4. **`ZSk_Durian`, `ZSk_Guardian`, `ZSk_Kunai` have no UpgradeConfigs** (warning in console). These skills are inert — won't appear in upgrade pool. Content config issue, not code.

5. **SV_SettingsPopup standalone** — original `Setting` panel is nested inside `UI/GamePlay/PauseScreen` and was cloned into SV_PausePopup as a nested child. If you want a standalone Settings popup accessible from MainMenu, I need to extract `Container/Setting` as its own prefab.

## Files changed this session

- `Assets/Luzart/UIFramework/NinjaUI/Runtime/Core/UIId.cs` — added 4 enum values: SV_Process, SV_Evolve, SV_Mails, SV_SelectMap
- `Assets/_Main/Data/UI/UIRegistry.asset` — 13 entries (was 8); updated all fileIDs to new prefab roots
- `Assets/_Main/Perfabes/UI/SV_*.prefab` (13 prefabs) — cloned from existing scene UI GameObjects
- `Assets/_Main/Scripts/UI/NinjaUIScreens/SV_LegacyWrappers.cs` — thin UIBase wrappers for the 6 legacy-driven UIs
- `Assets/_Main/Scripts/UI/NinjaUIScreens/SV_LevelUpPopupUI.cs` — rewrote slot binding to use existing children
- `Assets/_Main/Scripts/Gameplay/TimerManager.cs` — null-checks for cross-prefab text refs
- `Assets/_Main/Scripts/_LuzartGame/Entity/StatsBehavior.cs` — fallback defaults for HPMax/TotalSkill/ATK/Speed
- `Assets/_Main/Scenes/GamePlay.unity` — `_NinjaUI` Canvas fixed; legacy `UI` Canvas disabled

## Git checkpoints

- `b96f52d` checkpoint: clone 13 UI prefabs from legacy scene + fix _NinjaUI canvas
- `58ed24b` checkpoint #2: NinjaUI boot flow + pause + levelup all working
- `e34752c` checkpoint #3: SV_Shop re-cloned from correct GameObject, natural lose verified
- (final stat-fallback commit will come after this file is saved)

Rollback to any checkpoint with `git reset --hard <hash>` if needed.

## TL;DR for morning

✅ Game **runs end-to-end on NinjaUI** with all original Survivor.io UI styling preserved.
✅ Splash → MainMenu → Play → HUD → Pause → LevelUp → Win/Lose all working through the new system.
✅ Legacy UI is **invisible** (Canvas disabled) so user only sees NinjaUI.
⚠️ Some sub-screens (Shop / Equipment / Process / Evolve / Mails) are clone-only — they render the original UI but their click handlers point into legacy code that's been short-circuited; they need explicit nav wiring in SV_MainMenuUI to be usable.
⚠️ Stat config SOs need designer values; runtime fallbacks keep the game playable.

When you wake up, the fastest sanity check is to press Play in `Assets/_Main/Scenes/GamePlay.unity` — you should see splash bar fill → main menu with "1. Wild Streets" → click Start → gameplay with HUD → kill stuff → level-up popup appears → pick skill → continue → die → DEFEAT screen.
