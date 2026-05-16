---
title: Project Overview
category: overview
tags: [unity, survivor-io, thesis, rogue-lite]
sources: [raw/gdd/thesis-planning-chat.md, raw/technical/overnight-summary.md, raw/technical/ninja-ui-migration-guide.md]
created: 2026-05-16
updated: 2026-05-16
---

# DATN-GoGoSurvival — Project Overview

## Game

- **Engine**: Unity (2D)
- **Genre**: Survivor.io / Vampire-Survivors clone — rogue-lite, top-down, auto-attack
- **Platform**: Mobile (Android primary; iOS secondary)
- **Team size**: Solo (final-year thesis — đồ án tốt nghiệp)
- **Reference game**: Survivor.io (Habby)

## Core pillars

1. **Auto-combat survival** — player only moves; weapons fire on cooldown
2. **Build-craft via level-up picker** — every level pops a 3-skill choice (active or passive)
3. **5-set, 7-quality equipment progression** — meta layer between runs
4. **Mobile-first performance** — Object Pooling, 2D-optimized physics

## Current state (2026-05-16)

Code-side migration of NinjaUI framework is **complete and compiles clean** (375 source files, 0 errors). What's wired so far:

- **Adapter pattern bridge** between DATN's legacy `PlayerManager`/`EnemyManager` MonoBehaviours and the Luzart framework's `PlayerCharacter`/`EnemyCharacter` IContent (see [[technical/entity-adapter-pattern]] and [[decisions/adapter-bridge-vs-rewrite]]).
- **Scene boot flow**: `_GameBoot` (SceneRootManager + EntityManager) + `UIBootstrap` (Splash → MainMenu) — see [[technical/scene-boot-flow]].
- **9 UI prefabs** (Splash, MainMenu, GameplayHud, PausePopup, SettingsPopup, LevelUpPopup, LevelUpSlot, WinScreen, LoseScreen) registered in `UIRegistry.asset` — see [[art/ui-prefabs]].
- **Data layer**: 22 skills (10 active + 12 passive), 26 equipment items (5 sets × 5–6 slots), 4 enemy types, 9 drop types, 12 stat definitions, all authored as ScriptableObjects.

Remaining work tracked in [[open-questions]].

## Key systems

- [[systems/skill-system]] — 10 active + 12 passive skills, 5-star scaling per skill, level-up picker
- [[systems/equipment-system]] — 5 sets × 6 slots, 7 quality tiers, 10 enhance levels per piece
- [[systems/drop-system]] — XP / Coin / Magnet / Food / Bomb pickups
- [[systems/enemy-spawn]] — wave-based with HP/dmg/speed scaling per wave
- [[systems/ui-flow]] — NinjaUI lane/cache-policy model

## Key entities

- [[entities/player-character]] — DATNPlayerCharacter, joystick-driven, auto-fire weapons
- [[entities/enemies/regular-zombie]], [[entities/enemies/zombie-hound]], [[entities/enemies/elite-zombie-hound]], [[entities/enemies/boss-boucebloom]]

## Key decisions

- [[decisions/2d-survivor-genre]] — chose 2D Survivor.io reference over 3D/2.5D
- [[decisions/ugs-cloud-backend]] — Unity Gaming Services for Auth + Cloud Save (not Firebase)
- [[decisions/ninja-ui-migration]] — replaced legacy `DATN.Legacy.UIManager` monolith with NinjaUI framework
- [[decisions/adapter-bridge-vs-rewrite]] — keep DATN's PlayerManager/EnemyManager, bridge via adapter components
- [[decisions/object-pooling-priority]] — pooling is THE optimization story for the thesis

## Open questions

See [[open-questions]] — notable items: Shop/Equipment UI prefabs not yet built; 2 skill icons missing; legacy `DATN.Legacy.UIManager` still attached in scene during gradual migration.
