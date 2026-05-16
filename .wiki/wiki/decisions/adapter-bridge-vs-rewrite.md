---
title: Bridge DATN entities to Luzart framework via adapters
category: decisions
tags: [architecture, entities, adapter-pattern]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Bridge DATN entities to Luzart framework via adapters

## Decision
**Date**: 2026-05-16
**Decided by**: developer (overnight autonomous decision, marked for review)
**Status**: active (subject to long-term concern, see [[open-questions#q-20260516-01]])

### Context

DATN already had working `PlayerManager` and `EnemyManager` MonoBehaviours that handle joystick input, movement, animation, and spawning. The Luzart framework (`_LuzartGame/`) expects everything to live as `IContent` objects in a `Domain` registry — its skill behaviors do `_domain.Get<PlayerCharacter>()` and `_entityManager.GetAllEnemies()`.

Two ways to reconcile:

1. **Rewrite DATN's managers as `PlayerCharacter` / `EnemyCharacter` subclasses** — natural fit with framework, but the existing `PlayerCharacter` ctor requires a full `StatsConfig` ScriptableObject hierarchy and heavy Render/Animation behaviors that would duplicate DATN's working code.
2. **Adapter components that wrap DATN MonoBehaviours and register a stripped-down framework character** — keeps DATN's working code, framework just gets a "thin" character that exposes transform + stats.

### Options considered

1. **Full rewrite** — pros: single source of truth, framework skills work natively; cons: ~weeks of work, breaks all DATN gameplay until done.
2. **Adapter bridge** — pros: fast (one MonoBehaviour per side), zero rewrite, both systems alive immediately; cons: two character lifecycles, position sync overhead per Update, "thin" framework character lacks full behavior tree.
3. **Don't integrate framework entities at all** — pros: simplest; cons: skill behaviors that rely on `_domain.Get<...>()` won't work, framework becomes useless.

### Decision

**Option 2 — adapter bridge.** Two new MonoBehaviours + two `Character` subclasses:

- `DATNPlayerEntityAdapter` (on DATN Player GameObject) → creates `DATNPlayerCharacter : PlayerCharacter`, registers with `Domain`.
- `DATNEnemyEntityAdapter` (on `Zombie.prefab`) → creates `DATNEnemyCharacter : EnemyCharacter` per spawned zombie, registers with `EntityManager`.

`DATNPlayerCharacter` / `DATNEnemyCharacter` skip the `StatsConfig`-required ctor logic and skip heavy Render/Animation behaviors (DATN's legacy already drives those). They keep `TransformBehavior` + `StatsBehavior` so framework skills can read position and stats.

### Consequences

- Framework skill behaviors (kunai, boomerang, etc.) can resolve `_domain.Get<PlayerCharacter>()` and `_entityManager.GetAllEnemies()` without DATN code changes.
- Per-frame cost: adapter copies Unity `transform.position` → `character.Transform.SetPosition(...)` every Update. Cheap, but multiplied by enemy count.
- Two parallel update paths: DATN's `PlayerManager.Update` drives movement; `DATNPlayerCharacter.OnUpdate` ticks framework behaviors. If a framework behavior ever wants to *move* the player, ordering becomes important.
- Spawn registration is automatic: Zombie prefab's `Awake` runs adapter → adapter registers character → on `OnDestroy`, adapter unregisters. No manual list management.
- See [[technical/entity-adapter-pattern]] for the diagram.

> [!warning] Long-term concern
> If skill behaviors start *writing* to entities (forced movement, debuffs, etc.), the bridge needs a clear precedence rule. Currently DATN side is canonical; framework side is read-mostly. Tracked in [[open-questions#q-20260516-01]].

---
## Backlinks
- [[overview]]
- [[technical/entity-adapter-pattern]]
- [[entities/player-character]]
- [[entities/enemies/regular-zombie]]
