---
title: Open Questions
category: meta
created: 2026-05-16
updated: 2026-05-16
---

# Open Questions

## Open

### q-20260516-05 — Should `DATN.Legacy.UIManager` be fully deleted or kept as fallback during thesis defense?
- **Why it matters**: leaving both UI stacks in scene is risky for demo (input routing, double-renders); fully deleting requires Shop + Equipment NinjaUI prefabs first.
- **Where it surfaced**: `raw/technical/ninja-ui-migration-guide.md:100-107`, [[decisions/ninja-ui-migration]]
- **Status**: open

### q-20260516-04 — Missing 2 skill icons (Soccer Ball, Hi-Power Bullet) — find/create art?
- **Why it matters**: SV_LevelUpPopup will render blank slot if those skills roll. Cosmetic but visible.
- **Where it surfaced**: `raw/technical/overnight-summary.md:91,95`, [[systems/skill-system]]
- **Candidates / partial info**: Other 20 icons already in `Assets/Image/Skill/` and `Assets/Image/Passive/`.
- **Status**: open

### q-20260516-03 — Should `ZSkillConfig.upgradeConfigs` be authored (deep skill-stat-per-level) or rely on `SV_SkillCatalog`'s flat per-star data?
- **Why it matters**: framework-side `UpgradeSkillManager.UpgradeSkill()` won't apply real stat changes until per-level `ZSkillUpgradeConfig` exist. UI works either way.
- **Where it surfaced**: `raw/technical/overnight-summary.md:148`
- **Candidates / partial info**: 12 `AssetStatDefinition` SOs already created for referencing. Authoring is hundreds of small files.
- **Status**: open

### q-20260516-02 — Shop + Equipment screens still on legacy UI — when to migrate?
- **Why it matters**: MainMenu buttons currently route Shop/Equipment to `UIId.SV_Shop` and `UIId.SV_ItemEquipment` which have no UIRegistry entries → will throw at runtime.
- **Where it surfaced**: `raw/technical/overnight-summary.md:223-225`, [[art/ui-prefabs]]
- **Candidates / partial info**: existing DATN Shop UI can be ported with the same conversion pattern used for the 9 done prefabs (see [[sources/ninja-ui-migration-guide]] §1 Step 2).
- **Status**: open

### q-20260516-01 — Adapter pattern leaves two parallel character lifecycles — is that OK long-term?
- **Why it matters**: DATN's `PlayerManager`/`EnemyManager` tick movement/animation; framework `DATNPlayerCharacter` ticks its own behaviors. Risk of double-input or de-sync once Domain-driven skills start moving the player.
- **Where it surfaced**: `raw/technical/overnight-summary.md:36-49`, [[technical/entity-adapter-pattern]]
- **Candidates / partial info**: option to give framework `Transform.SetPosition` precedence; or restrict framework behaviors to read-only sensing.
- **Status**: open

## Answered
