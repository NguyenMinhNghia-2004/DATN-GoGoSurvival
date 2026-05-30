---
title: Log
category: log
created: 2026-05-16
updated: 2026-05-30
---

# DATN-GoGoSurvival — Log

Chronological record of all wiki operations.

## [2026-05-30] audit | Slice 0 re-audit — verified live state (autonomous run)

Verified against **live code + scene YAML + Unity Play mode** (the prior Phase-F handoff notes below were a forward-looking plan, not current state).

- **Baseline = green**: compiles clean, Play mode 0 errors, `LuzartPlayerController` player exists & renders. Verification gate works.
- **`MigrationFlags.asset` (runtime values)**: `UseLuzartPlayerController=1`, `UseLuzartPlayerEntityRoot=1`, `UseLuzartEnemyEntityRoot=1`, `FrameworkOwnsPlayerHP=0`. Player/enemy entity + player controller are Luzart-owned; **HP bridge not yet reversed**.
- **Live legacy components in `GamePlay.unity`** (GUID-checked): enabled — `GameManager`, `PlayerManager`, `ManagerWeapons`, `ControllerSpawening`, `GunManager`, `BooleanManager`, `SpriteWeapons`; disabled (`m_Enabled:0`) — `SpawenManager`x2, `ManagerEnemys`, legacy `CameraController` (GUID `3b3cb9cd...`, the known disabled-ref trap). `EnemyManager` lives on enemy prefabs (`Monster`, `Zombie`), not the scene.
- **Weapons still legacy at runtime**: `GunManager` fires hardcoded bolts ~every 2 s (no flag gate). The Luzart `ZSkill` scaffold exists (`_LuzartGame/Skills/`) but its `ZSkillBehavior_*` are stub scaffolds (empty behavior list at runtime). Real weapon migration = implement behaviors + author `ZSkillConfig` SOs from GDD + wire + retire `GunManager`.
- **Currency 100% legacy**: `CurrencyManager` + `DataManager` + PlayerPrefs (~15 refs each). No Luzart `ResourcePool` yet.
- > [!warning] `_LegacyCompat/_FrameworkStubs.cs` defines `IView`/`ViewT<T>`/`Data_ClassicEndGame`/`PopupSkillUpgradeData` used by the live death->Lose path — **compile-critical, do not delete until NinjaUI owns those types.**
- **Reference blueprint built**: `IO_Training/.wiki/wiki/systems/` (architecture, init-flow, skills, projectiles, stats, modifiers, items, currency). Mirror its composition; target deviates only on the GameObject-child skill model.
- Full corrected slice plan: `docs/superpowers/AUTONOMOUS-RUN-PROGRESS.md`.

## [2026-05-28] migrate | Phase F — foundation scaffolds (handoff)
- **Spec**: `docs/superpowers/specs/2026-05-28-phase-f-gameplay-zskill-monobehaviour-design.md`
- **Plan**: `docs/superpowers/plans/2026-05-28-phase-f-gameplay-zskill-monobehaviour.md`
- **Scope shipped this session**: foundation scaffolds only — no behavior change.
- **Done**:
  - `ZSkillRuntime : MonoBehaviour` — Survivor.io-style per-skill child of Player/Skills/
  - `LuzartPlayerController : MonoBehaviour` — future PlayerManager+JoystickManager replacement (dormant)
  - `LuzartPlayerEntityRoot : AbstractMonoBehaviorContent` — future DATNPlayerEntityAdapter replacement (dormant)
  - `LuzartEnemyEntityRoot : MonoBehaviour` — future EnemyManager+adapter replacement (dormant)
  - `MigrationFlags` extended with 4 Phase F bools (default false)
  - Scene: created `Player/Skills/` container GO
- **Deferred to follow-up sessions** (each needs play-test iteration):
  - Player cutover (3 commits): attach LuzartPlayerController + LuzartPlayerEntityRoot, flip flags, delete legacy 3 MBs
  - Weapon ports (12 commits): port 12 legacy weapons to ZSkillConfig+Behavior
  - Bridge reversal (1 commit): flip FrameworkOwnsPlayerHP, delete DATNGameplayBridge
  - Enemy cutover (2 commits): Zombie prefab swap, delete legacy enemy code
  - Camera cutover (1 commit): LuzartCameraController, delete legacy CameraController + ControllerSpawening
  - Legacy delete pass (3 commits): GameManager, BooleanManager, UIManager + _LegacyManagers
- **Net migration totals** (Phase C-F foundation):
  - `GamePlay.unity`: 12 root GOs → 10 (AdsManager, Enverement deleted; added Player/Skills child)
  - 7 new SOs (MigrationFlags, MigrationFlagsContent, WeaponCatalog, WeaponCatalogContent, LevelCatalog, LevelCatalogContent, the 9 prefab wrappers from C.4)
  - 5 new framework classes (ZSkillRuntime, Luzart{PlayerController, PlayerEntityRoot, EnemyEntityRoot, _Migration tools})
  - 4 legacy scripts deleted: CheatManager, ScrollContent, LevelsManager, PlayerStats
  - 1 Editor menu: `Tools/Migration/Delete Inactive Legacy GOs`
  - Game plays identically; ready for incremental cutover

## [2026-05-28] migrate | Phase E — PlayerStats singleton removed
- **Spec**: `docs/superpowers/specs/2026-05-28-phase-e-playerstats-skilldata-removal-design.md`
- **Rescoped**: audit revealed PlayerStats was effectively orphan (only writer = EquipmentManager; no readers anywhere). SkillData NOT a stub — 33 skill assets (Active/Passive/EVO) reference it. PassiveStatType still used by Equipment.GradeSkill.
- **Done**:
  - Deleted `PlayerStats.cs`. Method `EquipmentManager.ApplyToPlayerStats()` stripped to no-op (signature kept for compile compat).
- **Deferred to Phase F**: SkillData → ZSkillConfig migration (33 assets); PassiveStatType → framework StatType mapping; EquipmentManager bonus computation → StatsBehavior writes.
- **Next**: Phase F — gameplay loop rewrite + ZSkillRuntime MonoBehaviour

## [2026-05-28] migrate | Phase D — UIManager data + signal extraction
- **Spec**: `docs/superpowers/specs/2026-05-28-phase-d-legacy-uimanager-removal-design.md`
- **Plan**: `docs/superpowers/plans/2026-05-28-phase-d-legacy-uimanager-removal.md`
- **Rescoped**: full UIManager.cs deletion deferred to Phase F (PlayBtn/GameStart/BackFinishSafe deeply couple to legacy state). Phase D extracted **data SOs + signal property**, kept procedural facade.
- **Done**:
  - D.2: `WeaponCatalog` SO (12 entries) + `LevelCatalog` SO (Leve1-6) + 2 Content wrappers registered in Domain
  - D.3–D.5: `GameController.MapReady` + `SpawnDefaultLevel()`; `UIManager.MapReady` now bridges writes to framework; 3 readers migrated (`ControllerSpawening`, `DiamondVip`, `LocalisationPresent`)
  - D.6: `UIManager.PlayBtn` instantiates via `GameController.SpawnDefaultLevel`
  - D.7: `LevelsManager.cs` deleted; MonoBehaviour removed from `GameManager`; `UIManager.Level` field gone
  - D.8: `Tools/Migration/Delete Inactive Legacy GOs` Editor menu; deleted inactive `Enverement` (rootCount 11 → 10)
- **Deferred to Phase F**: `UIManager.cs` full delete, `_LegacyManagers` subtree, `SpriteWeapons.cs`, `StopAllAudios`/`useSurvivorIoEndGame` flags
- **Next**: Phase E — remove `PlayerStats` singleton + `SkillData` stub

## [2026-05-28] migrate | Phase C dead-code cleanup
- **Specs**: `docs/superpowers/specs/2026-05-28-luzart-migration-master-roadmap.md` + 4 phase specs
- **Plan**: `docs/superpowers/plans/2026-05-28-phase-c-dead-code-cleanup.md`
- **Done**:
  - C.1: introduced `Luzart.Migration.MigrationFlags` SO + `MigrationFlagsContent` wrapper, asset at `Assets/_Main/Data/Migration/`, wired into `_GameBoot.DomainContentLoader.contents`
  - C.2: deleted `CheatManager.cs` (obsolete) + `ScrollContent.cs` (unused) — the only 2 pure orphans confirmed
  - C.3a: deleted empty `AdsManager` GameObject (root count 12 → 11)
  - C.4: added `SV_<Name>UI : SV_LegacyUIBase` to 5 empty prefabs (`SV_Equipement`, `SV_Process`, `SV_Evolve`, `SV_Mails`, `SV_SelectMap`) so `UIManager.ShowAsync` resolves
- **Re-scoped**: 18 of 20 candidate dead-code scripts had prefab/scene refs → deletion absorbed into Phase D-F when host prefabs get refactored
- **Deferred to Phase D.9**: removing 2 inactive root GOs (`Enverement`, `_LegacyManagers/GamePlay`) — Unity MCP `manage_gameobject.delete` integer-ID rejection + YAML activation triggered Unity hang
- **Next**: Phase D — remove `DATN.Legacy.UIManager`

## [2026-05-16] init | Wiki initialized
- Created `.wiki/` structure inside project folder
- Engine: Unity 2D; Genre: Survivor.io rogue-lite; Context: final-year thesis (đồ án tốt nghiệp)
- Ready for first source ingest

## [2026-05-16] ingest | Project history bootstrap
- Sources ingested:
  - `raw/gdd/thesis-planning-chat.md` (initial planning chat — genre, UGS, ads decisions)
  - `raw/technical/overnight-summary.md` (M2 + M5 milestone summary, 23 SO assets, 9 UI prefabs)
  - `raw/technical/ninja-ui-migration-guide.md` (NinjaUI cookbook + checklist)
- Pages created (29):
  - Meta: [[overview]], [[index]], [[claims]], [[contradictions]], [[open-questions]]
  - Sources: [[sources/thesis-planning-chat]], [[sources/overnight-summary]], [[sources/ninja-ui-migration-guide]]
  - Decisions: [[decisions/2d-survivor-genre]], [[decisions/ugs-cloud-backend]], [[decisions/no-google-ads]], [[decisions/ninja-ui-migration]], [[decisions/adapter-bridge-vs-rewrite]], [[decisions/object-pooling-priority]]
  - Technical: [[technical/scene-boot-flow]], [[technical/entity-adapter-pattern]], [[technical/ninja-ui-architecture]], [[technical/data-layer]]
  - Systems: [[systems/skill-system]], [[systems/equipment-system]], [[systems/drop-system]], [[systems/enemy-spawn]], [[systems/ui-flow]]
  - Entities: [[entities/player-character]], [[entities/enemies/regular-zombie]], [[entities/enemies/zombie-hound]], [[entities/enemies/elite-zombie-hound]], [[entities/enemies/boss-boucebloom]]
  - Art: [[art/ui-prefabs]]
- Claims appended: c-20260516-01 through c-20260516-12
- Open questions: q-20260516-01 through q-20260516-05
