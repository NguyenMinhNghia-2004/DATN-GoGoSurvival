---
title: NinjaUI Migration Guide
category: sources
tags: [migration, ninja-ui, cookbook]
source_path: raw/technical/ninja-ui-migration-guide.md
source_type: technical
date: 2026-05-16
created: 2026-05-16
ingested: 2026-05-16
updated: 2026-05-16
---

# NinjaUI Migration Guide

## Abstract

Step-by-step cookbook for migrating remaining DATN screens from the legacy monolithic `DATN.Legacy.UIManager` to the NinjaUI framework. Covers: how to register new UIIds in `UIRegistry.asset`, the prefab-conversion workflow (duplicate → swap MonoBehaviour → wire references), how to call `UIManager.Instance.ShowAsync(...)` from gameplay code, an example wiring for `UpgradeSkillManager.UpgradeSkill()` (the level-up flow), a scene-hierarchy diagram, folder-structure final, gotchas (DOTween extension, CanvasGroup requirement, Time.timeScale + SetUpdate(true)), and a per-screen migration checklist.

## Key claims

- [[claims#c-20260516-01]] — Legacy UIManager and NinjaUI coexist during migration
- (UIRegistry cheatsheet: per-UIId lane + cache policy + dismiss flags — sourced from doc §1 Step 1 table, not promoted to a numbered claim since it's a cookbook table not a reusable fact)

## Pages updated from this source

- [[technical/ninja-ui-architecture]]
- [[systems/ui-flow]]
- [[art/ui-prefabs]]
- [[decisions/ninja-ui-migration]]

## Open questions raised

- [[open-questions#q-20260516-02]] — Shop + Equipment migration timing
- [[open-questions#q-20260516-05]] — Full delete vs keep legacy as fallback for demo

## Notes

Document doubles as a checklist and reference; consult it whenever adding a new UI screen. Useful sections:
- §1 Step 1: UIRegistry cheatsheet (12-row table of lane / cache / dismiss per screen)
- §3: Wiring example for `UpgradeSkillManager`
- §6: Gotchas (DOTween, CanvasGroup, Time.timeScale, DontDestroyOnLoad)
- §8: Test plan after migration
