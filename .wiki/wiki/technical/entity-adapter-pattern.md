---
title: Entity Adapter Pattern (DATN ↔ Luzart bridge)
category: technical
tags: [adapter, entities, architecture, di]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Entity Adapter Pattern

How DATN's MonoBehaviour-driven entities show up inside the Luzart framework's `Domain` registry without rewriting either side.

## The two worlds

**DATN side** (gameplay, Unity-native):
- `PlayerManager` MonoBehaviour on Player GameObject — handles joystick, movement, anim.
- `EnemyManager` MonoBehaviour orchestrates spawns; each zombie has movement + anim + AI components.
- Talks Unity Transform, AnimationController, Rigidbody2D.

**Luzart side** (framework, DI-driven):
- `Domain` — a `Dictionary<Type, IContent>` registry.
- `EntityManager : IContent` — registered in Domain, holds a list of `EnemyCharacter`.
- `PlayerCharacter : Character : IContent` — has `TransformBehavior`, `StatsBehavior`, `Inject(Domain)` lifecycle.
- Skill behaviors like `ZSkillBehavior_CreateProjectile` resolve targets via `_domain.Get<PlayerCharacter>()` and `_entityManager.GetAllEnemies()`.

## The adapter trick

Two pairs of new types, each pair = `MonoBehaviour adapter` + `framework Character subclass`:

| Pair | Adapter (MonoBehaviour) | Character subclass | Skips |
|---|---|---|---|
| Player | `DATNPlayerEntityAdapter` | `DATNPlayerCharacter : PlayerCharacter` | `StatsConfig`-required ctor logic |
| Enemy | `DATNEnemyEntityAdapter` | `DATNEnemyCharacter : EnemyCharacter` | heavy Render/Animation behaviors (DATN drives them) |

The adapter implements `AbstractMonoBehaviorContent` (Luzart base). At scene load, `SceneRootManager` finds it via `FindObjectsOfType<AbstractMonoBehaviorContent>()` and runs `Inject(domain)` → adapter creates its `Character` subclass and registers with the right registry.

## Lifecycle diagram

```
Scene load:
  _GameBoot.SceneRootManager.Awake()
    → new Domain()
    → FindObjectsOfType<AbstractMonoBehaviorContent>()
      → discovers DATNPlayerEntityAdapter, EntityManager
    → each.Inject(domain)
      → DATNPlayerEntityAdapter creates DATNPlayerCharacter
      → domain.Add<PlayerCharacter>(character)
    → each.Initialize()
      → character.Inject + Initialize
        → creates TransformBehavior + StatsBehavior
  Start() → each.Start() → behavior.Start()

Every Update:
  DATNPlayerEntityAdapter.DoUpdate
    → character.Transform.SetPosition(transform.position)   ← sync Unity → framework
    → character.OnUpdate(dt)                                ← ticks behaviors

Enemy spawn (DATN's SpawnManager Instantiate(Zombie.prefab)):
  Zombie GameObject Awake
    → DATNEnemyEntityAdapter.Awake
      → creates DATNEnemyCharacter
      → registers with domain.Get<EntityManager>()
  Each Update: sync transform + OnUpdate
  OnDestroy: unregister, terminate
```

## What this buys

Framework code that previously couldn't run (because no `PlayerCharacter` existed in the Domain) now works:

```csharp
var player = _domain.Get<PlayerCharacter>();      // returns DATNPlayerCharacter
var enemies = _entityManager.GetAllEnemies();     // returns List<DATNEnemyCharacter>
projectile.Target = nearestEnemy(player, enemies);
```

## What it doesn't buy

- DATN still owns the canonical state. If a framework behavior wants to *move* the player, it has to either fight `PlayerManager.Update` or get DATN to step aside. Currently framework is read-mostly.
- StatsConfig isn't wired. `DATNPlayerCharacter` skips it, so framework `ZSkillUpgradeConfig` stat changes won't actually move DATN's HP/ATK numbers. The thin character is a *positioning* + *identity* shim, not a stat owner. See [[open-questions#q-20260516-03]].

## Pooling implication

When Object Pooling lands ([[decisions/object-pooling-priority]]), enemy despawn must:
1. Call `EnemyManager.Despawn()` (DATN side).
2. Trigger `DATNEnemyEntityAdapter.OnDisable` (or equivalent) to unregister from framework `EntityManager` — **not** `OnDestroy`, since pooling sets inactive instead of destroying.

This is a known gotcha to handle when the pool refactor happens.

---
## Backlinks
- [[overview]]
- [[technical/scene-boot-flow]]
- [[decisions/adapter-bridge-vs-rewrite]]
- [[entities/player-character]]
- [[entities/enemies/regular-zombie]]
