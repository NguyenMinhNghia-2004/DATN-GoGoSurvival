---
title: Object Pooling is thesis Chapter 4 centerpiece
category: decisions
tags: [optimization, thesis, performance]
sources: [raw/gdd/thesis-planning-chat.md]
created: 2026-05-16
updated: 2026-05-16
---

# Object Pooling is thesis Chapter 4 centerpiece

## Decision
**Date**: 2026-05-15
**Decided by**: developer (planning chat)
**Status**: active (planned, not yet implemented)

### Context

The planning chat identified that the current code abuses `Instantiate` + `Destroy` for enemies and projectiles. With horde-survival genre ([[decisions/2d-survivor-genre]]) that's a guaranteed mobile-perf hit. Thesis outline (Chương 4.2 — viết code cho các module chương trình chính) needs a concrete optimization technique to demonstrate engineering depth.

### Decision

Make Object Pooling the headline technical contribution of the report. Apply it to:
- Enemy spawns (currently `SpawnManager.Instantiate(Zombie)`).
- Projectiles (kunai, boomerang, etc.).
- Drop pickups (XP, coin, magnet, food, bomb).
- VFX hit-flash / death particles.

### Consequences

- Need a generic `IPoolable` interface + pool registry (`PoolManager` singleton or DI service).
- Adapter pattern means enemies have **two** lifecycles to wire pool returns through: DATN's `EnemyManager` despawn AND the framework `DATNEnemyEntityAdapter.OnDestroy → unregister from EntityManager`. Pooling needs to call `EnemyManager.Despawn()` then `adapter.Reset()` (not `Destroy`).
- Benchmark: before/after CPU + GC graphs at 100/500/1000 enemies = report-friendly figures.
- Thesis writing hook: tie it to "Cơ sở lý thuyết về phân tích và thiết kế hệ thống thông tin" (Chương 2.1) by framing pooling as a memory-management design pattern.

> [!info] Not done yet
> As of 2026-05-16 the codebase still uses `Instantiate`/`Destroy`. This decision sets the direction, not the implementation.

---
## Backlinks
- [[overview]]
- [[decisions/2d-survivor-genre]]
- [[sources/thesis-planning-chat]]
