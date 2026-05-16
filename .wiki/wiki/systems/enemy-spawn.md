---
title: Enemy Spawn System
category: systems
tags: [enemies, spawning, waves, scaling]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Enemy Spawn System

DATN's legacy `EnemyManager` + `SpawnManager` instantiate enemy prefabs each wave. The Luzart framework sees them via `DATNEnemyEntityAdapter` ([[technical/entity-adapter-pattern]]).

## Enemy types (4)

`Assets/_Main/Data/Enemies/`:

| Asset | HP | Damage | Speed | Role |
|---|---|---|---|---|
| `En_RegularZombie` | 100 | 5 | 2.5 | Trash mob — bulk of horde |
| `En_ZombieHound` | 80 | 4 | 3.5 | Fast flanker |
| `En_EliteZombieHound` | 400 | 10 | 3.0 | Mini-boss |
| `En_BossBoucebloom` | 5000 | 25 | 1.5 | Wave boss |

Each `EnemyData` SO is fed into `EnemyManager` per-wave spawn rules.

## Per-wave scaling

Stats scale up each wave (multipliers on base):

| Stat | Per-wave increment |
|---|---|
| HP | +15% |
| Damage | +10% |
| Speed | +2% |

So wave N stats = `baseStat × (1 + perWaveMultiplier)^(N-1)`. Speed scales gently to keep the game playable; HP scales aggressively for difficulty.

See [[claims#c-20260516-10]].

## Spawn → framework registration

```
SpawnManager.Instantiate(Zombie.prefab)
  → Zombie GameObject Awake
    → DATNEnemyEntityAdapter.Awake
      → creates DATNEnemyCharacter
      → registers with domain.Get<EntityManager>()
```

`EntityManager.GetAllEnemies()` is what framework skill behaviors (e.g. `ZSkillBehavior_CreateProjectile` selecting target) read.

## Pooling — future

Currently uses `Instantiate` + `Destroy`. The thesis-headline optimization ([[decisions/object-pooling-priority]]) will replace this with a pool. When that lands, `DATNEnemyEntityAdapter.OnDestroy` becomes `OnDisable` (or explicit reset) so the framework `EntityManager` registration is cleared without destroying the GameObject. See [[technical/entity-adapter-pattern]] §pooling implication.

> [!tip] Benchmark target
> 100 / 500 / 1000 simultaneous enemies = thesis-report-friendly stress test numbers.

---
## Backlinks
- [[overview]]
- [[entities/enemies/regular-zombie]], [[entities/enemies/zombie-hound]], [[entities/enemies/elite-zombie-hound]], [[entities/enemies/boss-boucebloom]]
- [[decisions/object-pooling-priority]]
- [[technical/entity-adapter-pattern]]
- [[claims#c-20260516-09]], [[claims#c-20260516-10]]
