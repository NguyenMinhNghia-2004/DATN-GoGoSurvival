---
title: Equipment System
category: systems
tags: [equipment, meta-progression]
sources: [raw/technical/overnight-summary.md]
created: 2026-05-16
updated: 2026-05-16
---

# Equipment System

Meta-layer between runs: player accumulates equipment, enhances it, equips it for stat bonuses + grade skills.

## Five sets, ~6 slots each

| Set | Slot count | Items |
|---|---|---|
| Army | 6 | `Eq_Kunai` (Weapon), `Eq_ArmyNameplate` (Necklace), `Eq_ArmyGloves`, `Eq_ArmyUniform` (Armor), `Eq_ArmyBelt`, `Eq_ArmyBoots` |
| Monster | 5 | `Eq_BonePendant`, `Eq_LeatherGloves`, `Eq_Carapace`, `Eq_LeatherBelt`, `Eq_ProstheticLegs` |
| Protective | 5 | `Eq_EmeraldPendant`, `Eq_ProtectiveGloves`, `Eq_ProtectiveSuit`, `Eq_BroadWaistguard`, `Eq_LayeredSnowshoes` |
| Metal | 5 | `Eq_MetalNeckguard`, `Eq_ShinyWristguard`, `Eq_FullMetalSuit`, `Eq_WaistSensor`, `Eq_LightRunners` |
| Stylish | 5 | `Eq_TrendyCharm`, `Eq_FingerlessGloves`, `Eq_TravelersJacket`, `Eq_StylishBelt`, `Eq_StylishBoots` |

Total = 26 `EquipmentData` SOs under `Assets/_Main/Data/Equipment/`.

## Per-item fields

- `atkByQuality[7]` **or** `hpByQuality[7]` (depending on slot type) — 7 quality tiers Normal → Relic.
- `maxEnhanceLevel = 10`, `enhanceBonusPerLevel = 5%`.
- `enhanceCostCoins[10]` — exponential ramp (early tiers cheap, late expensive).
- 3 grade skills (Excellent / Epic / Legendary) — name + description from GDD. Unlocked when item is upgraded to that grade.

## Quality vs Enhance

These are independent axes:
- **Quality** (Normal → Relic, 7 tiers) — set by the item drop / shop purchase. Determines base stat.
- **Enhance level** (0 → 10) — player spends coins to upgrade. Multiplies base stat by `1 + level * 0.05`.

So a Relic-quality item at +0 may outperform a Normal-quality item at +10, depending on the gap.

## GDD note

Original GDD was lighter; the 7-tier ladder is extrapolated. See [[claims#c-20260516-06]] for the cited values.

## Linked starting skill

`Eq_Kunai` references a starting skill via `linkedStartingSkill` (type `SkillData` from `_LegacyCompat/`). Equipping the kunai pre-arms the player with `Sk_Kunai` at run start.

## UI status

The Equipment screen is **NOT yet on NinjaUI**. MainMenu button routes to `UIId.SV_ItemEquipment`, which has no UIRegistry entry → will throw. See [[open-questions#q-20260516-02]].

---
## Backlinks
- [[overview]]
- [[systems/skill-system]] — `linkedStartingSkill` ties equipment to starting weapons
- [[technical/data-layer]]
- [[claims#c-20260516-07]]
