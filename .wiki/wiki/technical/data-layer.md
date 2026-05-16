---
title: Data Layer — ScriptableObject conventions
category: technical
tags: [data, scriptable-object, conventions]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Data Layer — ScriptableObject conventions

All game-content data lives under `Assets/_Main/Data/` as ScriptableObject assets. No JSON/CSV imports; designers edit in Inspector.

## Directory map

```
Assets/_Main/Data/
├── Equipment/         (26 EquipmentData)         see [[systems/equipment-system]]
├── Enemies/           (4 EnemyData)              see [[systems/enemy-spawn]]
├── Skills/
│   ├── SV_SkillCatalog.asset (22 entries)       see [[systems/skill-system]]
│   ├── Configs/       (22 ZSkillConfig — shells)
│   └── Behaviors/     (22 ZSkillBehaviorConfig)
├── Drops/             (9 *DropConfig)            see [[systems/drop-system]]
├── StatDefinitions/   (12 AssetStatDefinition)
└── UI/
    └── UIRegistry.asset                          see [[technical/ninja-ui-architecture]]
```

## Naming conventions

| Prefix | Type | Examples |
|---|---|---|
| `Eq_` | EquipmentData | `Eq_Kunai`, `Eq_ArmyUniform` |
| `En_` | EnemyData | `En_RegularZombie`, `En_BossBoucebloom` |
| `Drop_` | DropConfig (XP/Coin/Magnet/Food/Bomb) | `Drop_SmallBiofuel`, `Drop_Magnet` |
| `Sk_` | Active skill (catalog entry id) | `Sk_Kunai`, `Sk_Boomerang` |
| `Ps_` | Passive skill (catalog entry id) | `Ps_HiPowerMagnet`, `Ps_SportsShoes` |
| `ZSk_` | ZSkillConfig (framework shell, active) | `ZSk_Kunai`, `ZSk_RPG` |
| `ZPs_` | ZSkillConfig (framework shell, passive) | `ZPs_FitnessGuide`, `ZPs_KogaNinjaScroll` |
| `StatDef_` | AssetStatDefinition | `StatDef_HPMax`, `StatDef_TiLeChiMang` (crit rate) |

## Why two skill data types?

Historical: `SV_SkillCatalog` is the **UI-side flat data** (icon, name, per-star description, scaling multipliers). It feeds the level-up popup directly. `ZSkillConfig` + `ZSkillBehaviorConfig` is the **framework-side runtime data** that drives projectile spawning / passive stat application. Currently shells exist but `ZSkillConfig.upgradeConfigs` is empty — see [[open-questions#q-20260516-03]].

## Vietnamese stat names

Some stat definitions use Vietnamese identifiers to match the GDD:

| Stat | Vietnamese | English |
|---|---|---|
| `StatDef_TiLeChiMang` | Tỉ lệ chí mạng | Crit rate |
| `StatDef_SatThuongChiMang` | Sát thương chí mạng | Crit damage |

Other stats use English: `StatDef_HPMax`, `StatDef_ATK`, `StatDef_Speed`, `StatDef_Cooldown`, `StatDef_FireSpeed`, `StatDef_Armor`, `StatDef_Luck`, `StatDef_XPMultiplier`, `StatDef_Heal`, `StatDef_RangeFind` — 12 total.

When grepping for stat usage, search both forms.

## _LegacyCompat shims

`Assets/_Main/Scripts/_LegacyCompat/` holds 4 files that exist purely for compile compatibility during migration:
- `SkillData.cs` (Equipment.linkedStartingSkill type)
- `SkillEnums.cs` (`PassiveStatType` enum)
- `SV_SkillCatalog.cs` (the catalog SO type definition lives in this folder)
- `_FrameworkStubs.cs` (`IView`, `IBroadcastData`, `Data_ClassicEndGame`, …)

> [!info] When migration is done
> Drop the contents of `_LegacyCompat/` once Shop + Equipment are on NinjaUI and the old `DATN.Legacy.UIManager` is deleted.

---
## Backlinks
- [[overview]]
- [[systems/skill-system]]
- [[systems/equipment-system]]
- [[systems/drop-system]]
- [[systems/enemy-spawn]]
