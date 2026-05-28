# Phase E — Remove `PlayerStats` singleton + `SkillData` stub

- **Status**: Draft, awaiting user approval
- **Parent**: `2026-05-28-luzart-migration-master-roadmap.md`
- **Created**: 2026-05-28
- **Risk**: Medium-High
- **Prerequisite**: Phase D complete

## 1. Outcome

After Phase E:

- `PlayerStats.Instance` is deleted. Equipment bonuses apply directly to the framework `StatsBehavior` of `DATNPlayerCharacter`.
- `EquipmentData.linkedStartingSkill` field type changes from `SkillData` to `ZSkillConfig`. All 26 `Eq_*` SO assets re-author to point at the matching `ZSk_*`/`ZPs_*`.
- `_LegacyCompat/SkillData.cs` and `_LegacyCompat/SkillEnums.cs` are deleted. `PassiveStatType` enum is replaced by mappings to framework `StatType`.
- `EquipmentManager`, `EquipmentInventory`, `EquipmentInstance` still exist (they are not "legacy") but now bind to framework Stats.
- Save data (PlayerPrefs) remains backwards-compatible — a one-shot migration in `DataManager.Awake` rewrites any old keys.
- Game still plays identically.

## 2. Inventory — current `PlayerStats` + `SkillData` usage

### 2.1 `PlayerStats` (legacy singleton, NOT in scene root — instantiated at runtime, likely in MainMenu prefab or via lazy `Instance` getter)

- `Assets/_Main/Scripts/Player/PlayerStats.cs`: holds base ATK/HP/Speed, passive modifiers dict (`Dictionary<PassiveStatType, float>`), equipment bonuses.
- Readers (from prior grep):
  - `EquipmentManager.ApplyToPlayerStats()` — writes
  - `PlayerStats` callers in projectile/HP code — TBD (verify via project-wide grep in Slice E.1)

### 2.2 `SkillData` (stub in `_LegacyCompat`)

- `Assets/_Main/Scripts/_LegacyCompat/SkillData.cs`: minimal class, `[CreateAssetMenu("GoGo/Legacy/Skill Data (STUB)")]`. Asset files: enumerated in Slice E.1 audit (`*.asset` of type `SkillData`). Expected count: 0 or very few.
- Used by: `EquipmentData.linkedStartingSkill` (one field, one type).

### 2.3 `PassiveStatType` enum

- `Assets/_Main/Scripts/_LegacyCompat/SkillEnums.cs`: enum with 12 entries.
- Used by: `PlayerStats` modifiers dict + `GradeSkill` struct in `EquipmentData`.

## 3. New types / changes

### 3.1 Mapping table: `PassiveStatType` → framework `StatType`

| PassiveStatType (legacy) | Framework StatType |
|---|---|
| `AttackRange` | `RangeFind` (or new `AttackRange` if framework lacks it — verify) |
| `MaxHP` | `HPMax` |
| `MovementSpeed` | `Speed` |
| `CooldownReduction` | `Cooldown` (as multiplier; sign carries through) |
| `DamageReduction` | `Armor` |
| `GoldGain` | `Luck`? or a new `GoldGain` StatType added |
| `EXPGain` | `XPMultiplier` |
| `ItemLootRange` | `RangeFind` (overlap with AttackRange — confirm) |
| `HPRegen` | `Heal` |
| `BulletSpeed` | `FireSpeed` |
| `SkillDuration` | new `SkillDuration` StatType |

If framework `StatType` is missing entries, **add them** to the framework enum (this is the user's own framework, not third-party). Document each enum addition in the slice commit.

### 3.2 `EquipmentData` field migration

Current field:
```csharp
[SerializeField] private SkillData linkedStartingSkill;
```

Target:
```csharp
[SerializeField] private ZSkillConfig linkedStartingSkill;
```

Migration strategy: **AssetPostprocessor + manual re-assignment**.

- Most `linkedStartingSkill` fields are likely null on `Eq_*` assets (only weapon equipment defines this). Verify by reading all 26 `Eq_*.asset`.
- For non-null entries: map by name convention (e.g., a weapon `Eq_Kunai` likely links to `ZSk_Kunai`). Author this mapping table in the slice commit message.

### 3.3 `EquipmentManager.ApplyToPlayerStats` → `ApplyTo(StatsBehavior)`

Refactor signature:

```csharp
// Before
public void ApplyToPlayerStats()
{
    PlayerStats.Instance.ClearAllPassiveModifiers();
    foreach (var equip in _equipped) {
        foreach (var grade in equip.GetUnlockedGradeSkills())
            PlayerStats.Instance.SetPassiveModifier(grade.statType, grade.value);
    }
}

// After
public void ApplyTo(StatsBehavior stats)
{
    // clear & re-apply equipment modifiers tagged with source "equipment"
    stats.ClearModifiersBySource("equipment");
    foreach (var equip in _equipped) {
        foreach (var grade in equip.GetUnlockedGradeSkills()) {
            var st = LuzartStatMap.FromPassive(grade.statType);
            stats.AddModifier(source: "equipment", st, grade.value);
        }
    }
}
```

Requires extending `StatsBehavior` with:
- `AddModifier(source, statType, value)`
- `ClearModifiersBySource(source)`

These are small additions to the framework — necessary for the migration target shape.

## 4. Slice plan

### Slice E.1 — Audit usage

- Grep project-wide for `PlayerStats.Instance`, `SkillData`, `PassiveStatType`. Document every call site.
- Identify which Player prefab(s) attach `PlayerStats` MonoBehaviour. Likely on `Player` GO or its `MainMenu` instantiation path.
- Update §2 of this spec with concrete file lists.
- Commit (docs only): `docs(E.1): audit PlayerStats + SkillData usage`.

### Slice E.2 — Extend `StatsBehavior` with sourced modifiers

- Add `AddModifier(source, statType, value)`, `RemoveModifier(source, statType)`, `ClearModifiersBySource(source)`.
- Internal: store `List<(string source, StatType type, double value)> _modifiers` and recompute on apply.
- Add unit-test-style runtime check: call apply, read final stat, log.
- Commit: `migrate(E.2): extend StatsBehavior with sourced modifiers`.

### Slice E.3 — `LuzartStatMap` helper

- Create `Assets/_Main/Scripts/_LuzartGame/Migration/LuzartStatMap.cs` with `FromPassive(PassiveStatType) -> StatType`.
- Add any missing `StatType` enum entries (`GoldGain`, `SkillDuration`, etc. — TBD per audit).
- Commit: `migrate(E.3): introduce LuzartStatMap + StatType additions`.

### Slice E.4 — Dual-path `EquipmentManager.ApplyToPlayerStats`

- Add `[SerializeField] bool _useFrameworkStats;` (or use `MigrationFlags.UseFrameworkStatsForEquipment`).
- When flag off → existing legacy apply.
- When flag on → resolve `PlayerCharacter` from `Domain`, call `ApplyTo(stats)`.
- Flag default off.
- Commit: `migrate(E.4): dual-path EquipmentManager apply (flag off)`.

### Slice E.5 — Cutover: flag on

- Set flag on.
- Play-test: equip various items, check stat values reflected in HUD + actual damage/move.
- Bug-fix in this commit if needed.
- Commit: `migrate(E.5): cutover EquipmentManager to framework Stats`.

### Slice E.6 — Delete `PlayerStats.cs` + legacy path

- Remove the `else` branch in `EquipmentManager`.
- Remove any direct `PlayerStats.Instance` reads in other code (use `Domain.Get<PlayerCharacter>().Stats` instead).
- Delete `PlayerStats.cs`.
- If a `PlayerStats` MonoBehaviour exists on a prefab, remove the component.
- Commit: `migrate(E.6): delete PlayerStats singleton`.

### Slice E.7 — Migrate `EquipmentData.linkedStartingSkill` field type

- This is a **destructive SO change**. Sequence:
  1. Audit which `Eq_*.asset` have non-null `linkedStartingSkill` and map name → target `ZSkillConfig`.
  2. Write an Editor utility `Tools/Migration/ConvertLinkedStartingSkill` that:
     - For each `EquipmentData` asset, reads the YAML field, resolves the matching `ZSkillConfig` by name pattern, writes the new ref.
  3. Run the utility. Verify 26 assets resolve cleanly.
  4. Change `EquipmentData` field type from `SkillData` to `ZSkillConfig`.
  5. Refresh assets — should re-bind correctly.
- Commit: `migrate(E.7): convert EquipmentData.linkedStartingSkill to ZSkillConfig`.

### Slice E.8 — Delete `SkillData.cs` + `SkillEnums.cs`

- Verify zero references.
- Delete `_LegacyCompat/SkillData.cs`.
- Replace `PassiveStatType` usage in `EquipmentData.GradeSkill` with `StatType` directly.
- Delete `_LegacyCompat/SkillEnums.cs`.
- Commit: `migrate(E.8): delete SkillData + PassiveStatType`.

### Slice E.9 — Save data migration

- In `DataManager.Awake`, if `PlayerPrefs.HasKey("legacy_equipment_v1")` (old equipment JSON key), read it → migrate to new format → write new key → delete old key.
- (Verify whether the schema actually changed — if `EquipmentInstance.ToSaveEntry` JSON shape stayed the same, no migration needed.)
- Commit: `migrate(E.9): one-shot save data migration` — or skip with note if unchanged.

### Slice E.10 — Phase E close-out

- Wiki log update.
- Verify success criteria.
- Commit: `migrate(E.10): Phase E close-out`.

## 5. Success criteria

- [ ] `Grep "PlayerStats.Instance"` → 0 hits.
- [ ] `Grep "PassiveStatType"` → 0 hits in `.cs` files.
- [ ] `_LegacyCompat/SkillData.cs`, `_LegacyCompat/SkillEnums.cs` deleted.
- [ ] 26 `Eq_*.asset` files have valid `linkedStartingSkill: ZSkillConfig` refs (or null where appropriate).
- [ ] Equipping a weapon in MainMenu Equipment screen → entering Gameplay → confirmed stat change (HP/ATK reflected in HUD).
- [ ] Full play loop works.

## 6. Out of scope

- Implementing actual `ZSkillUpgradeConfig` content (still open question q-20260516-03 — Phase F decides on minimum viable hook).
- Adding equipment-quality unlock animations.
- Server-side save (PlayerPrefs only).

## 7. Risks

| Risk | Mitigation |
|---|---|
| `EquipmentData.linkedStartingSkill` field type change loses all SO refs (Unity invalidates them) | Run AssetPostprocessor BEFORE field change. Keep a backup branch tag before E.7 |
| `PlayerStats` reads exist beyond `EquipmentManager` (e.g. in projectile code reading `FinalATK`) | E.1 audit must enumerate all readers; each gets its own subslice if non-trivial |
| Framework `StatType` is missing entries (e.g. `GoldGain`) and adding to enum invalidates any serialized `StatType` field on existing SO assets | Always **append** to enum end (never reorder). Test by re-loading existing `ZSkillConfig` assets after change |
| Cutover in E.5 reveals that equipment bonuses don't actually drive damage today (legacy `PlayerManager` may hardcode HP) | Document the gap; if true, Phase E success criteria scales down and Phase F picks it up |

## 8. Decisions

- **Append-only StatType enum**: required to avoid SO data loss.
- **`PassiveStatType` deleted, not kept as adapter**: code reads will be rewritten directly; the type was a stub.
- **Save data migration is opt-in**: only added if E.7 schema change requires it. Default assumption: no JSON change needed.
