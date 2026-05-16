---
title: Regular Zombie
category: entities
tags: [enemy, trash-mob, zombie]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Regular Zombie (`En_RegularZombie`)

## Base stats

| Stat | Value |
|---|---|
| HP | 100 |
| Damage | 5 |
| Speed | 2.5 |

Asset: `Assets/_Main/Data/Enemies/En_RegularZombie.asset`.

## Role

Trash mob. Forms the bulk of the horde in every wave. Slow enough to be outrun on foot at base speed (player ≈ 4–5 effective).

## Wave scaling

See [[systems/enemy-spawn]] — HP +15%, dmg +10%, speed +2% per wave.

## Prefab

The framework-side `DATNEnemyEntityAdapter` is attached to **`Zombie.prefab`** which all four enemy types share. The variant's `EnemyData` ScriptableObject reference determines which stats to apply at spawn.

---
## Backlinks
- [[systems/enemy-spawn]]
- [[technical/entity-adapter-pattern]]
