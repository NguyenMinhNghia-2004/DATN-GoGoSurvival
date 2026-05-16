---
title: Drop System
category: systems
tags: [drops, pickups]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Drop System

Enemies drop pickups on death. 9 drop SO assets under `Assets/_Main/Data/Drops/`, four distinct effect families.

## Drop families

### XP (Biofuel) — fuels level-up loop

Type: `XPDropConfig`

| Asset | XP |
|---|---|
| `Drop_SmallBiofuel` | 10 |
| `Drop_MediumBiofuel` | 20 |
| `Drop_BigBiofuel` | 50 |

Feeds [[systems/skill-system]] — every level threshold triggers SV_LevelUpPopup.

### Coin — meta-currency for [[systems/equipment-system]] enhance

Type: `CoinDropConfig` (new for this project)

| Asset | Coins |
|---|---|
| `Drop_SmallCoin` | 10 |
| `Drop_MediumCoin` | 20 |
| `Drop_BigCoin` | 50 |

### Magnet — radial absorb

Type: `MagnetDropConfig` (new)

| Asset | Radius |
|---|---|
| `Drop_Magnet` | 30 |

On pickup, pulls all on-screen XP + coin drops to the player.

### Food — heal

Type: `FoodDropConfig` (new)

| Asset | Effect |
|---|---|
| `Drop_Food` | Heals 20% of max HP |

### Bomb — screen clear

Type: `BombDropConfig` (new)

| Asset | Effect |
|---|---|
| `Drop_Bomb` | 200 damage, radius 8 |

## GDD: Upgrade Box intentionally excluded

GDD listed a 6th drop type (Upgrade Box). It is **NOT** in scope:

> "Bỏ nhé, cái này code thêm kha khá" — GDD note.

Translation: "Skip — it would take quite a bit of extra code." Decision is final for v1.

See [[claims#c-20260516-08]].

---
## Backlinks
- [[overview]]
- [[systems/skill-system]] — XP drops feed level-up
- [[systems/equipment-system]] — Coin drops feed enhance economy
- [[technical/data-layer]]
