---
title: Index
category: index
created: 2026-05-16
updated: 2026-05-16
---

# DATN-GoGoSurvival — Wiki Index

Master catalog. Read first to find relevant pages.

## Overview
- [[overview]] — Survivor.io-clone thesis project, NinjaUI migration done, data SOs authored

## Meta
- [[claims]] — Cross-page facts with sources
- [[open-questions]] — Unanswered design/tech questions
- [[contradictions]] — Conflicting claims (currently empty)

## Sources
- [[sources/overnight-summary]] — Overnight migration work summary (M2 + M5 milestones)
- [[sources/ninja-ui-migration-guide]] — NinjaUI cookbook + migration checklist
- [[sources/thesis-planning-chat]] — Initial planning conversation (genre, UGS, ads decisions)

## Systems
- [[systems/skill-system]] — 22 skills (10 active + 12 passive), 5-star scaling, level-up picker
- [[systems/equipment-system]] — 5 sets × 6 slots × 7 quality tiers, 10 enhance levels
- [[systems/drop-system]] — XP / Coin / Magnet / Food / Bomb pickups
- [[systems/enemy-spawn]] — Wave-based with per-wave HP/dmg/speed scaling
- [[systems/ui-flow]] — NinjaUI lane + cache-policy model

## Entities
- [[entities/player-character]] — DATNPlayerCharacter via adapter
- [[entities/enemies/regular-zombie]] — HP 100, dmg 5, speed 2.5
- [[entities/enemies/zombie-hound]] — HP 80, dmg 4, speed 3.5
- [[entities/enemies/elite-zombie-hound]] — HP 400, dmg 10, speed 3.0
- [[entities/enemies/boss-boucebloom]] — HP 5000, dmg 25, speed 1.5

## Art
- [[art/ui-prefabs]] — 9 SV_* prefabs (Splash, MainMenu, HUD, popups, end-screens)

## Technical
- [[technical/scene-boot-flow]] — `_GameBoot` + `UIBootstrap` sequence
- [[technical/entity-adapter-pattern]] — How DATN ↔ Luzart entity bridge works
- [[technical/ninja-ui-architecture]] — Lanes, registry, asset provider, lifecycle
- [[technical/data-layer]] — Where SO assets live, naming conventions

## Decisions
- [[decisions/2d-survivor-genre]] — Survivor.io 2D vs 3D/2.5D
- [[decisions/ugs-cloud-backend]] — Unity Gaming Services vs Firebase vs custom
- [[decisions/ninja-ui-migration]] — Replace legacy UIManager with NinjaUI
- [[decisions/adapter-bridge-vs-rewrite]] — Bridge DATN entities to framework
- [[decisions/object-pooling-priority]] — Pooling is thesis Chapter 4 centerpiece
- [[decisions/no-google-ads]] — Removed AdMob (no dev account)

## Bugs
<!-- (empty — none recorded yet) -->

## Analysis
<!-- (empty) -->
