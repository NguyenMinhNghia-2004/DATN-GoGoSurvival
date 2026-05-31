# Overnight execution plan — 2026-06-01

User authorized full overnight autonomous work to implement GoGo Survival's systems following the IO_Training (Luzart/Crystal) reference, starting with **populating every ScriptableObject from the GDD with prefabs assigned**.

## Findings (discovery phase)

### GDD content (per-sheet)
- **Note**: Resources = Energy/Coin/Gem; Settings = Music/Sound/Vibration; Level milestone bonuses (Lv20 +1000 HP, Lv40 +5% Crit, Lv60 +600 ATK, Lv80 +5% Heal on level-up, Lv100 +1000 ATK, Lv120 +5% Crit); per-level XP curve (1-20=50, 21-40=60, 41-60=70, 61-80=80, 81-100=80, 101-999=100); per-level XP total caps (300/600/1000/1500/2200/3000/4000).
- **Active Skills (10)**: Kunai, Boomerang, Brick, Drill Shot, Durian, Forcefield, Guardian, Molotov, RPG, Soccer Ball. Each has 5 star tiers (★ → ★★★★★) with ATK Multiplier + Scale (or Speed bullet Multiplier) + per-star description.
- **Passive Skills (12)**: Hi-Power Magnet, Fitness Guide, Ammo Thruster, HE Fuel, Energy Drink, Exo-Bracer, Energy Cube, Oil Bond, Ronin Oyoroi, Sports Shoes, Hi-Power Bullet, Koga Ninja Scroll. Each has 5 levels with a single % bonus.
- **Equipment (26 items, 5 sets)**: Army/Monster/Protective/Metal/Stylish, each with Necklace/Gloves/Armor/Belt/Shoes. 5 quality tiers (Normal/Good/Better/Excellent/Epic) → Stats base + bonus-per-level, with grade-skill text effects from Better tier on. Necklaces also have an extra "Merge Item" mechanic (3-of-same to upgrade).
- **Enemies (2 + 1 elite + 1 boss)**: Regular Zombie, Zombie Hound (Normal), Elite Zombie Hound, Boss Boucebloom.
- **Drops (9 + scrapped Upgrade Box)**: Small/Medium/Big Biofuel (XP 10/20/50), Small/Medium/Big Coin, Magnet, Food (Heal 20% HP), Bomb (AoE).

### Existing state in target project (post-W6 nuke head `5ec7778`)
- 237 Luzart framework `.cs` files in `Assets/_Main/Scripts/_LuzartGame/` — compile clean.
- **Already-populated SOs**: enemy `Stat_*` (HP/ATK/Speed numeric constants), `StatsCfg_*` (linked to stats), `EnDef_*` (sprite/material/scale + prefab refs assigned), `EquipmentData` custom (atkByQuality/hpByQuality/gradeSkill text — uses custom `EquipmentData` class, NOT Luzart `ItemConfig`).
- **Skeleton SOs (created but empty)**:
  - `ZSk_*.asset` (10 active skill configs) — `upgradeConfigs: []` empty, behavior config ref present.
  - `ZSk_*_Behavior.asset` (10 behavior configs) — `skillBehaviorStats: []` and `projectileConfig: {fileID: 0}` unset.
  - `ZPs_*.asset` (12 passive skill configs) + `ZPs_*_Behavior.asset` — likewise mostly empty.
  - `SkillDatabase.asset`, `SV_SkillCatalog.asset` — present, status TBD.
- **Missing SOs entirely**:
  - `Assets/_Main/Data/Projectiles/` folder + 10 `ProjectileConfig` SOs (one per skill).
  - 10 active × 5 = 50 `ZSkillUpgradeConfig` SOs.
  - 12 passive × 5 = 60 `ZSkillUpgradeConfig` SOs.
  - `AssetStatDefinition` SO for `StatType.AmountProjectile`, `Cooldown`, `FireSpeed`, `LifeTime`, `RangeFind`, `RadiusCollider`, `ATKMultiplier`, `Scale` — partial coverage in `StatDefinitions/`.
  - `AssetModifier_*` + `AssetModifierDefinition_*` SOs (needed for passive skill modifier factors).
- **Available legacy art for prefabs** (in `Assets/_Main/Perfabes/Normal/`): Bullet, Bullet1-5, Rocket, Brick, Diamond/DiamondBlue/Green/Red/VIP, Flash, Wapeon, WeapRotate, Zombie, Monster.

## Strategy — what's tonight's deliverable

Given the scope, prioritise **complete authoring data** so editing in Unity Inspector is unnecessary, AND **one playable Kunai vertical slice**:

### Phase A — Plan + foundations (this commit)
1. Plan doc (this file).
2. Create `Assets/_Main/Data/Projectiles/` folder.
3. Create missing `AssetStatDefinition` SOs.

### Phase B — Projectile configs (1 SO per skill, 10 total)
Map each skill to an existing legacy art prefab and a config archetype:

| Skill        | Archetype                          | Sprite/art prefab    | Notes |
|--------------|------------------------------------|----------------------|-------|
| Kunai        | `NormalProjectileConfig`           | Bullet1 / Bullet     | straight-line, dies on hit |
| Boomerang    | `BoomerangProjectileConfig`        | Bullet2              | acceration/return |
| Brick        | `BombProjectileConfig`             | Brick                | gravity arc + ground AoE |
| Drill Shot   | `LaserProjectileConfig`            | Bullet3              | pierces |
| Durian       | `NormalProjectileConfig`           | Bullet4              | knockback (no impl yet) |
| Forcefield   | `NormalProjectileConfig` (radial?) | Flash                | placeholder — radial done in behavior |
| Guardian     | `NormalProjectileConfig`           | WeapRotate           | orbiting (placeholder) |
| Molotov      | `BombProjectileConfig`             | Fire                 | bomb + ground patch |
| RPG          | `BombProjectileConfig`             | Rocket               | impact explosion |
| Soccer Ball  | `BoomerangProjectileConfig`        | Bullet5              | bouncing reuse |

### Phase C — Active skill upgrade configs (50 SOs)
From GDD `Active_Skills.json`: ATK Multiplier + Scale/Speed per-star → `ZSkillUpgradeConfig` `stats` list. Cooldown set per skill via constant (Kunai=1.0s, Boomerang=1.5s, etc.). Each level adds a `SerializedModifierPair` factor (e.g. "+1 Kunai" → `AmountProjectile +1`).

### Phase D — Wire active skill configs
For each `ZSk_<Skill>.asset`:
- `upgradeConfigs` = [Lv1..Lv5] (5 refs)
- `behaviorConfigs[0]` = `ZSk_<Skill>_Behavior` (already set)
- Wire `ZSk_<Skill>_Behavior.projectileConfig` = `ProjCfg_<Skill>` from Phase B
- Wire `ZSk_<Skill>_Behavior.skillBehaviorStats` = AmountProjectile + TimeBreak

### Phase E — Passive skill configs (60 upgrade SOs + modifiers)
From GDD `Pasive_Skills.json`: each passive = 5 levels of +N% to a single stat.
- Create `AssetModifier_<Stat>_AddRatio` + `ModDef_<Stat>_AddRatio` SOs.
- Each passive level produces a `SerializedModifierPair` (factor: value=N%, modifier: ref to the stat's add-ratio asset modifier).

### Phase F — Skill database wiring
- `SkillDatabase.asset` lists all 10 active + 12 passive configs.
- `SV_SkillCatalog.asset` wires up UI display.
- Wire to `LuzartPlayerEntityRoot._startingSkills` (Kunai only on start).

### Phase G — Equipment audit
- Equipment uses custom `EquipmentData` (not Luzart `ItemConfig`). Keep that — populate per GDD: stats base + bonus-per-level per quality, grade-skill text effects, link starting weapon item to skill via `linkedStartingSkill`.
- Recompute existing `Eq_*.asset` data against `Equipment.json` to ensure tiers Better/Excellent/Epic match the GDD numbers.

### Phase H — Verify + commit + report
- Compile via Unity MCP refresh.
- Console check: no new errors.
- Commit per phase. Halt at 3 reds.
- Morning report at `docs/morning-report-2026-06-01.md`.

## Workflow contract (preserved)
- 1 phase = 1+ commits, message format `feat(<area>): <phase> — <slice> [autonomous]`
- Each commit: compile clean, no new exceptions, SOs load without warnings
- Visual freeze: no sprite/Animator/UI layout edits — only SO data + prefab refs
- Rollback to last green on red; halt at 3 reds and write a blocker note

## Out of scope tonight
- EVO skills (already exist as 11 SO shells; not in GDD primary scope)
- UI level-up panels & skill-choice screens (data ready; UI = visual layout work for morning)
- Save / Cloud Save integration
- Currency Luzart-side restoration (post-W6 nuke; coin display = 0)
- Joystick / input system overhaul
