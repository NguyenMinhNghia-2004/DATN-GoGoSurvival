---
title: Zombie Hound
category: entities
tags: [enemy, fast, zombie]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Zombie Hound (`En_ZombieHound`)

## Base stats

| Stat | Value |
|---|---|
| HP | 80 |
| Damage | 4 |
| Speed | 3.5 |

Asset: `Assets/_Main/Data/Enemies/En_ZombieHound.asset`.

## Role

Fast flanker. Lower HP than [[entities/enemies/regular-zombie]] but speed 3.5 means it catches the player at base movement speed. Forces the player to keep moving rather than camp.

## Wave scaling

Per [[systems/enemy-spawn]] — speed reaches ~4.0 by wave ~25 (gentle 2%/wave growth).

---
## Backlinks
- [[systems/enemy-spawn]]
- [[entities/enemies/elite-zombie-hound]] — bigger sibling
