---
title: Player Character (DATNPlayerCharacter)
category: entities
tags: [player, adapter, joystick]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Player Character

## Two-sided identity

| Side | Type | Responsibility |
|---|---|---|
| Unity / DATN | `PlayerManager` MonoBehaviour on Player GameObject | Joystick input, movement, animation, weapon firing |
| Luzart framework | `DATNPlayerCharacter : PlayerCharacter` | `TransformBehavior`, `StatsBehavior`, `OnUpdate` for framework skills |

`DATNPlayerEntityAdapter` (also on the Player GameObject) bridges them — see [[technical/entity-adapter-pattern]].

## Why a custom `DATNPlayerCharacter`

Vanilla `PlayerCharacter` ctor demands a full `StatsConfig` ScriptableObject hierarchy (Render config, Animation config, Movement config, …). DATN already drives all that legacy-side; the custom subclass **skips** the heavy ctor and just keeps:
- `TransformBehavior` — so framework can read position
- `StatsBehavior` — so framework skills can query/modify stats (currently mostly read-only; full stat write-through is open — see [[open-questions#q-20260516-01]])

## Lifecycle

```
Scene load:
  DATNPlayerEntityAdapter.Inject(domain)
    → new DATNPlayerCharacter(domain)
    → domain.Add<PlayerCharacter>(character)
  Initialize:
    → character creates TransformBehavior + StatsBehavior
  Start:
    → behaviors Start()

Every Update:
  adapter.DoUpdate:
    character.Transform.SetPosition(transform.position)   ← Unity → framework
    character.OnUpdate(dt)                                ← framework behaviors tick
```

## Joystick + input

Input handled entirely by `PlayerManager.Update()` (DATN legacy) using `Joystick Pack` asset. Framework side does not receive input — it just observes the resulting transform.

## Weapons

Starting weapons come from equipped items in [[systems/equipment-system]] (`Eq_Kunai` ships with `linkedStartingSkill = Sk_Kunai`). New weapons unlock via [[systems/skill-system]] level-up picks.

---
## Backlinks
- [[overview]]
- [[technical/entity-adapter-pattern]]
- [[technical/scene-boot-flow]]
- [[decisions/adapter-bridge-vs-rewrite]]
- [[systems/skill-system]]
