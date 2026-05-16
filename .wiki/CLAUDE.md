# DATN-GoGoSurvival — Game Wiki Schema

You are the wiki maintainer for this game project. Build and maintain a persistent knowledge base in `wiki/`. Read from `raw/` but never modify it.

## Project

- **Engine**: Unity (2D)
- **Genre**: Survivor.io / Vampire-Survivors-style rogue-lite
- **Context**: Đồ Án Tốt Nghiệp (final-year thesis project) — code is the primary deliverable but report (báo cáo) outline drives some scope decisions
- **Project root**: one level up from this `.wiki/` directory
- **Created**: 2026-05-16

## Directory structure

```
.wiki/
├── CLAUDE.md       # This file
├── raw/            # Immutable sources
│   ├── gdd/        # Thesis planning chats, design docs
│   ├── meetings/
│   ├── references/
│   ├── feedback/
│   ├── technical/  # Migration guides, overnight summaries
│   └── assets/
└── wiki/           # LLM-owned
    ├── index.md
    ├── log.md
    ├── overview.md
    ├── claims.md
    ├── contradictions.md
    ├── open-questions.md
    ├── sources/
    ├── systems/
    ├── entities/
    ├── world/
    ├── art/
    ├── technical/
    ├── decisions/
    ├── bugs/
    └── analysis/
```

## Provenance ledger

The four meta files (`claims.md`, `contradictions.md`, `open-questions.md`, `sources/`) form the provenance layer. **Always cite a source for cross-page facts.**

## Page conventions

Every page has YAML frontmatter:
```yaml
---
title: Page Title
category: systems | entities | world | art | technical | decisions | bugs | analysis | meta | sources | overview | index | log
tags: [relevant, tags]
sources: [raw/technical/overnight-summary.md]
created: YYYY-MM-DD
updated: YYYY-MM-DD
---
```

### Wikilinks — ALWAYS with category path

- `[[systems/skill-system]]` ✓
- `[[skill-system]]` ✗

Typed relationships: `(depends on)`, `(contradicts)`, `(supersedes)`, `(see also)`.

### Callouts

```markdown
> [!warning] Contradiction
> [!question] Open Question
> [!info] Design Intent
> [!bug] Known Issue
> [!tip] Optimization Note
```

## This project's custom rules

- **Framework code lives in `Assets/Luzart/` and `Assets/_Main/Scripts/_LuzartGame/`** — treat as third-party (don't modify). Anything DATN-specific lives elsewhere under `_Main/Scripts/`.
- **`DATN.Legacy.*`** namespace = old monolithic UI/managers being decommissioned. New code uses NinjaUI (`Luzart.UIManager`).
- **Two parallel character hierarchies**: DATN's legacy `PlayerManager`/`EnemyManager` (MonoBehaviour) AND the Luzart framework `PlayerCharacter`/`EnemyCharacter` (IContent in Domain). Adapter components (`DATNPlayerEntityAdapter`, `DATNEnemyEntityAdapter`) bridge them so framework skill behaviors can resolve DATN's in-scene entities.
- **All SO data lives under `Assets/_Main/Data/`** subdirs: `Equipment/`, `Enemies/`, `Skills/`, `Drops/`, `StatDefinitions/`, `UI/`.
- **UI registry**: `Assets/_Main/Data/UI/UIRegistry.asset` is the single source of truth for `UIId → prefab` mapping.
- **Online services**: chose **Unity Gaming Services (UGS)** for Authentication + Cloud Save. Google Mobile Ads was removed (no developer account).
- **Vietnamese context**: thesis report uses Vietnamese; codebase identifiers + commit messages mix Vietnamese (e.g. `Eq_TiLeChiMang` = crit rate, `SatThuongChiMang` = crit damage) with English. Keep both forms in mind when querying.
