---
title: Log
category: log
created: 2026-05-16
updated: 2026-05-28
---

# DATN-GoGoSurvival — Log

Chronological record of all wiki operations.

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
