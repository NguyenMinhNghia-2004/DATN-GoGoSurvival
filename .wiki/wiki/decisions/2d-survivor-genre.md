---
title: Genre — 2D Survivor.io rogue-lite
category: decisions
tags: [genre, scope]
sources: [raw/gdd/thesis-planning-chat.md]
created: 2026-05-16
updated: 2026-05-16
---

# Genre — 2D Survivor.io rogue-lite

## Decision
**Date**: 2026-05-15
**Decided by**: developer (during planning chat)
**Status**: active

### Context

Existing code-base started as a Manager-Centric 2D template. Three reference directions were viable:
1. Keep 2D, target Survivor.io / Vampire Survivors style (auto-attack, level-up picker)
2. Lift to 2.5D for visual polish
3. Full 3D rewrite

The thesis report (đồ án tốt nghiệp) needs concrete optimization work in Chapter 4, which biases toward a genre with enough on-screen entities to make pooling matter.

### Options considered

1. **2D Survivor.io** — pros: aligns with existing 2D physics setup, hordes of enemies make Object Pooling a believable optimization story, simpler art pipeline. Cons: visually less "wow".
2. **2.5D** — pros: nicer demo; cons: physics + camera rework, doubles art budget.
3. **3D** — out of scope for solo thesis timeline.

### Decision

**2D Survivor.io clone.** Locks the genre as horde-survival rogue-lite with: joystick movement, auto-fire weapons, level-up picker (3-skill choice), 5-set equipment meta, wave-based spawning.

### Consequences

- Equipment/skill data models match Survivor.io structure (5 sets × 6 slots, active+passive split). See [[systems/equipment-system]], [[systems/skill-system]].
- Need lots of enemies on screen → [[decisions/object-pooling-priority]] becomes a real requirement, not just thesis padding.
- 2D physics layer optimization stays in scope (raised in planning chat).
- No 3D pipeline complexity: keep `Plugins/Demigiant/DOTween` for animation, skip animation rigging.

---
## Backlinks
- [[overview]] — frames the project
- [[claims#c-20260516-02]] — claim refers here
- [[sources/thesis-planning-chat]] — origin
