---
title: Skill System
category: systems
tags: [skills, level-up, picker]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Skill System

## Loop

1. Player levels up (XP threshold via [[systems/drop-system]] biofuel pickups).
2. `UpgradeSkillManager.UpgradeSkill()` rolls 3 candidate skills.
3. `UIManager.Instance.ShowAsync(UIId.SV_LevelUpPopup, ...)` opens picker (force-pick, ESC blocked).
4. Player picks → `Broadcaster.Broadcast(SkillUpgradeSuccessBroadcastData)`.
5. `OnSkillUpgradeSuccessBroadcast` handler applies upgrade (stat boost or new skill).
6. Time.timeScale resumed; gameplay continues.

Fallback: if `UIManager.Instance == null`, auto-pick option 0 (logs warning). Useful for tests + headless runs.

## Catalog (`SV_SkillCatalog.asset`)

22 entries — 10 active, 12 passive. Each entry has per-star (★1..★5) scaling:
- `atkMultiplier[5]`
- `scaleMultiplier[5]`
- `speedMultiplier[5]`
- `perStarDescription[5]`

For passives: `passiveValue[5]` + `passiveStatType` + `perStarDescription[5]`.

### Active skills (10)

| Id | Behavior config type | Icon wired |
|---|---|---|
| `Sk_Kunai` | `ZSkillBehaviorConfig_CreateProjectile` | ✓ |
| `Sk_Boomerang` | `ZSkillBehaviorConfig_CreateProjectile` | ✓ |
| `Sk_Brick` | `ZSkillBehaviorConfig_Bomb` | ✓ |
| `Sk_DrillShot` | `ZSkillBehaviorConfig_CreateProjectile` | ✓ |
| `Sk_Durian` | `ZSkillBehaviorConfig_Bomb` | ✓ |
| `Sk_Forcefield` | `ZSkillBehaviorConfig_Bomb` | ✓ |
| `Sk_Guardian` | `ZSkillBehaviorConfig_Lighting` | ✓ |
| `Sk_Molotov` | `ZSkillBehaviorConfig_Bomb` | ✓ |
| `Sk_RPG` | `ZSkillBehaviorConfig_CreateProjectile` | ✓ |
| `Sk_SoccerBall` | `ZSkillBehaviorConfig_CreateProjectile` | ✗ (icon missing) |

### Passive skills (12)

All use `ZSkillBehaviorConfig_AddStat` (flat stat boost per star).

`Ps_HiPowerMagnet`, `Ps_FitnessGuide`, `Ps_AmmoThruster`, `Ps_HEFuel`, `Ps_EnergyDrink`, `Ps_ExoBracer`, `Ps_EnergyCube`, `Ps_OilBond`, `Ps_RoninOyoroi`, `Ps_SportsShoes`, `Ps_HiPowerBullet` (✗ icon missing), `Ps_KogaNinjaScroll`.

Icons live in `Assets/Image/Skill/` (active) and `Assets/Image/Passive/` (passive).

## Two-table data design

`SV_SkillCatalog` carries **UI-facing** flat data; `ZSkillConfig` shells (in `Assets/_Main/Data/Skills/Configs/`) carry the **framework-side runtime** data. Currently `ZSkillConfig.upgradeConfigs` lists are empty → framework's `UpgradeSkillManager.UpgradeSkill()` won't apply stat changes from a `ZSkillUpgradeConfig` because none exist yet. The popup still shows the right info because it reads `SV_SkillCatalog`. See [[open-questions#q-20260516-03]] and [[technical/data-layer]].

## UI binding

`SV_LevelUpSlot.Bind(skillId, star)` looks up the entry in `SV_SkillCatalog`, populates icon + name + `perStarDescription[star]`. The slot is a child prefab; the popup uses `VerticalLayoutGroup` to host 3 slots.

> [!bug] Missing icons
> `Sk_SoccerBall` and `Ps_HiPowerBullet` have no icon assigned. Slots will show blank if they roll. See [[open-questions#q-20260516-04]].

---
## Backlinks
- [[overview]]
- [[technical/data-layer]]
- [[art/ui-prefabs]]
- [[claims#c-20260516-05]]
