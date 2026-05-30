# MIGRATION PROMPT — Rebuild GoGo Survival on the clean Luzart architecture

> **Hand this whole file to Claude Code on the executor machine.** It is self-contained: it tells you the two project paths, the data source, the target architecture, the hard guardrails, and a slice-by-slice plan. Read it top to bottom before touching anything.
>
> **You are running in AUTONOMOUS MODE — the developer is away for several hours.** Read **§7 (Autonomous Mode)** before you start; it governs how you commit, verify, self-review, stop on failure, and report. §7 overlays the whole plan.

---

## 0. Who you are and what you're doing

You are picking up a Unity 2D **Survivor.io-style** rogue-lite called **GoGo Survival**. It is a final-year thesis project (Đồ Án Tốt Nghiệp) for a **solo developer**. The codebase is mid-migration from an old "DATN/legacy" stack to a clean "Luzart" framework. Your job is to **finish the migration: delete ALL remaining legacy LOGIC and rebuild it on the Luzart architecture, while keeping every existing visual untouched.**

There are **three** locations you must know:

| Role | Path |
|---|---|
| **Target** (the project you edit) | `/Users/luzart/Documents/All_SurvivorIO/DATN-GoGoSurvival` |
| **Reference** (the blueprint — read only, never edit) | `/Users/luzart/Documents/All_SurvivorIO/survivorio` |
| **GDD** (data source — read only) | `/Users/luzart/Documents/All_SurvivorIO/GDD - GoGo Survival.xlsx` |

The reference project `survivorio` is a more mature build of the **same framework** by the original framework author. It already does, cleanly, everything the target is trying to become. **When in doubt about how a system should look, open the reference and mirror it.** The reference has an excellent wiki at `survivorio/.wiki/wiki/systems/` — read those pages (`game-init-flow`, `architecture-bigpicture`, `skill-cooldown-attack-loop`, `stat-system`, `item-equipment`, `modifier-pipeline`, `projectile-types`) as your primary blueprint.

---

## 1. HARD GUARDRAILS — read these first, they override everything

These are not suggestions. Violating any one of them is a failed task.

1. **NO destructive rampage. Code-only, scoped edits.** A previous overnight refactor broke the game by mass-deleting. You delete **one logical slice per commit**, and the game must be **playable after every single commit**. If you cannot verify a slice plays, you do NOT proceed to the next.

2. **Visual freeze — keep every visual exactly as-is.** Do **not** edit: sprites/`SpriteRenderer` settings, `Animator` controllers/state machines, particle/VFX child GameObjects, prefab GameObject hierarchies (Body, Weapon Point, child structure), UI `Canvas` layout (RectTransform/Image/Text positions). You may **add** new MonoBehaviour components to existing GameObjects, and **add** new child GameObjects under existing parents. You may **delete** legacy logic components. You may NOT restyle anything. If a visual change is strictly required (e.g. wiring a button), document it in the commit message.

3. **Delete LOGIC only, never visuals.** "Logic" = input handling, damage/HP, kill counting, weapon firing, enemy AI, currency math, equipment-apply, UI button onClick handlers, audio gating, data containers that aren't pure visual data. Replace these with Luzart equivalents. Sprites/animation/layout stay.

4. **Never modify `.wiki/raw/`.** It is an immutable source archive. You may read it. You may write to `.wiki/wiki/` (the LLM-owned knowledge base) and should update `.wiki/wiki/log.md` as you go.

5. **Zero-downtime via strangler-fig + feature flags.** New code goes in alongside old, gated by a bool in `MigrationFlags` (SO at `Assets/_Main/Data/Migration/MigrationFlags.asset`), default off. Flip on, play-test, fix, default on, then delete old. A half-finished slice left in-tree with its flag off must not affect gameplay.

6. **Verify in Unity, every slice.** "It compiles" is not "it works." After each slice: compile clean (no console errors), enter Play mode, confirm Splash→MainMenu→Play→player moves→enemies spawn & die→XP/HP HUD updates→level-up popup→death→Lose screen→back to MainMenu→replay. Exit Play with no errors. Use the Unity MCP tools (`manage_editor` for play mode, `read_console` for errors, `manage_camera action=screenshot` for visual diff).

7. **Solo-dev, thesis context.** No automated tests are in scope (the project has none and adding them explodes the work). Manual play-test is the gate. Keep the migration narrative clean — it has to survive a thesis defense.

8. **Ask before anything irreversible** beyond a normal commit (force-push, history rewrite, deleting whole scenes). Normal local commits are fine and expected.

---

## 2. Current state of the target (verified by reading code, NOT the stale wiki)

> ⚠️ The target's `.wiki/wiki/log.md` and `technical/*.md` pages are **STALE** — they describe adapter classes (`DATNPlayerEntityAdapter`, `DATNEnemyEntityAdapter`) that have already been deleted, and stop at "Phase F foundation scaffolds." **Do not trust them. Verify against live code.** Part of your job is to bring the wiki back in sync.

The migration is **further along than the wiki suggests**. As of the last verified audit:

- **Boot works on Luzart already.** `SceneRootManager.Awake()` builds the `Domain` (a `Dictionary<Type,IContent>` DI registry), discovers `AbstractMonoBehaviorContent` in scene, `DomainContentLoader` (exec order −900) registers SO-backed contents; `SceneRootManager.Start()` runs `InitializeAll`/`StartAll`; `UIBootstrap.Start()` drives Splash→MainMenu via NinjaUI `UIManager.ShowAsync(UIId)`.
- **`MigrationFlags` current values:** `UseLuzartPlayerController: 1`, `UseLuzartPlayerEntityRoot: 1`, `UseLuzartEnemyEntityRoot: 1`, `FrameworkOwnsPlayerHP: 0`. So **player movement, player entity, and enemy entity are already Luzart-owned**; only the **HP source-of-truth bridge has not been reversed yet**.
- **`GameManager.cs` is the legacy "spider" hub** (~17 inbound references). Its `Health` field is effectively dead — `LuzartOwnsDeath()` returns true while `UseLuzartPlayerController` is on, so legacy `Health` never drops and the framework owns death via `GameController.OnHPChange → Data_ClassicEndGame → SV_LoseScreen`. GameManager is the **last thing to delete**, not the first.
- **Live legacy components still attached in `GamePlay.unity`:** `GameManager`, `PlayerManager`, `ManagerEnemys`, `ControllerSpawening`, `BooleanManager`, `SpriteWeapons`, `GunManager`, `ManagerWeapons`, two `SpawenManager` instances.
- **The trap:** `CameraController.cs` (GUID `3b3cb9cd94424674c9ab27b16ef001c3`) looks like a clean orphan (0 `.cs` refs, 0 active scene component) but is referenced in `GamePlay.unity` as a **disabled** MonoBehaviour block (`m_Enabled: 0`). Grep the scene/prefab YAML for a script's GUID **before** deleting it — `find_gameobjects` alone misses disabled refs.

**Lesson baked in:** before deleting any `.cs`, search `.unity` + `.prefab` for its GUID, not just C# references.

---

## 3. Target architecture — what you are building toward

This is the architecture the developer wants, derived from their own analysis of the reference. Mirror the reference's *composition* but follow the **GameObject-child skill model** below where they differ (see §3.6, the one deliberate deviation).

### 3.1 Domain is the entry point; UI opens itself off it
`Domain` boots first and starts everything. When Domain finishes loading, control passes to the NinjaUI `UIManager`, which opens **MainMenu**. No legacy manager drives the boot. This already works — preserve it, remove anything legacy still hanging off it.

### 3.2 UI prefabs already have visuals; they must be decoupled
The UI prefabs — **MainMenu, Gameplay, Win, Lose, Inventory, Shop, UpgradeSkill** — already exist with full visual layout. **Each UI owns its own components; UIs are NOT coupled to each other or to a god-manager.**
- `UIGameplay` holds its own joystick, EXP bar, stat readouts.
- `UIUpgradeSkill` holds its own 3 skill-choice slots.
- Each reads/writes framework state through Domain content + events, not through `GameManager`.
- `UIRegistrySO` (`Assets/_Main/Data/UI/UIRegistry.asset`) is the single source of truth for `UIId → prefab`.

### 3.3 Components configured by ScriptableObject
Behaviour is data-driven. A component is a thin MonoBehaviour/Behavior; its tuning lives in a ScriptableObject. Items that affect stats are SOs (mirror the reference's `ItemConfig`/equipment SOs). **Authoring values come from the GDD (§4), not hardcoded.**

### 3.4 Skills = one GameObject child per skill on the character
Each skill is a **separate GameObject**, child of the character under a `Skills/` container:
```
Player (GameObject)
└── Skills/
    ├── ZSkillRuntime_Kunai      [ZSkillRuntime : MonoBehaviour]
    ├── ZSkillRuntime_Boomerang  [ZSkillRuntime : MonoBehaviour]
    └── …
```
- `ZSkillConfig` (SO) = authored data (cooldown, damage, target rule, **prefab ref**, editable visuals).
- `ZSkillRuntime : MonoBehaviour` drives cooldown → target acquisition → spawn projectile → apply stats.
- Compose `ZSkillBehavior_*` (CreateProjectile / Stat / AddStat / Lighting / Bomb) into the runtime, mirroring the reference's behavior decomposition.
- Visible in Inspector at runtime → matches the Survivor.io mental model ("each weapon is a thing in the scene") and aids debugging.

### 3.5 Projectiles = GameObject + SO, visual-only data
Each bullet/projectile is a **separate GameObject** spawned from a prefab, configured by a `ProjectileConfig` SO (base + Normal/Laser/Bomb/Boomerang/Lighting subclasses, mirror reference). The SO/prefab carry **visual** data; the **logic** (movement, hit, damage application) lives in `ProjectileEntity`. Spawned by `ZSkillBehavior_CreateProjectile`.

### 3.6 ⚠️ The one deliberate deviation from the reference
The **reference** implements skills as **plain classes** driven by a `SkillControllerBehavior`. The **target** wants skills as **GameObject children** (`ZSkillRuntime : MonoBehaviour`, per §3.4 — this matches the scaffold already in the target and roadmap §3.1). **Follow the target's GameObject-child model**, but borrow the reference's Config/Behavior/Projectile **composition shape**. Flag this divergence in your commit message and in the wiki when you implement it, so the thesis narrative is explicit about the choice.

### 3.7 Character = MonoBehaviour + GameObject with animations
The player and enemies are GameObjects with MonoBehaviour controllers (`LuzartPlayerController`, `LuzartEnemyController`/`LuzartEnemyEntityRoot`) and their existing Animators. Logic (input, damage, HP, AI) is framework-owned; the Animator and sprites are untouched.

### 3.8 Stats = each stat is a type, configured in SO
Every stat is a typed entity (framework `StatType` + `StatsBehavior` with `Number` runtime values exposing `.Changed` events). Stat definitions live in SOs under `Assets/_Main/Data/StatDefinitions/`. The legacy `PassiveStatType` enum maps 1:1 onto framework `StatType` at the boundary and is deleted.

### 3.9 Money = SO stat updated via observer pattern
Currency is a framework resource (mirror reference's `ResourcePool_Gold`/`ResourcePool_Gem` + `ResourceDefinition_*` SOs) that **notifies observers on change**, not a `DataManager`/`CurrencyManager` singleton poking PlayerPrefs every frame. UI subscribes to the change event. Keep PlayerPrefs **save-compat** during migration (one-shot key migration in `DataManager.Awake` if a field must change).

---

## 4. Data source — the GDD (read, then author SOs)

All numeric content (stat values, skill numbers, enemy stats, drop tables, equipment) lives in the GDD **outside** `Assets/`:
`/Users/luzart/Documents/All_SurvivorIO/GDD - GoGo Survival.xlsx`

Six sheets (note the exact names, some have trailing spaces):
`Note`, `Equipment`, `Active Skills ` (trailing space), `Pasive Skills` (sic), `Enemy`, `Drop Item`.

Read it with Python `openpyxl`. **`openpyxl` may not be installed — install it first** (`pip install openpyxl`). Use the GDD as the authoritative source when authoring/filling SO assets. Do **not** invent numbers; pull them from the sheet.

---

## 5. Execution plan — leaf-to-root, one slice per commit

Delete from the **leaves inward**; `GameManager` is the trunk and goes **last**. Every slice = one commit, game playable after each. Use `MigrationFlags` to gate risky cutovers.

**Slice 0 — Re-audit (do not skip).** The state in §2 was true at handoff; verify it now. List live legacy components in `GamePlay.unity` (`find_gameobjects` by component), grep each legacy `.cs` GUID across `.unity`+`.prefab` for hidden/disabled refs, read current `MigrationFlags` values. Update `.wiki/wiki/log.md` with the real current state. Output a corrected slice list before editing.

**Slice 1 — Reverse the HP bridge.** Flip `FrameworkOwnsPlayerHP` on. Make `StatsBehavior.Runtime_HP` the source of truth; legacy `GameManager.Health` (if read anywhere) reads from it. Cutover commit keeps both sync directions until verified, then removes the old one. Play-test death still triggers Lose screen.

**Slice 2 — Port the 12 weapons** to `ZSkillRuntime` GameObject-children (§3.4–3.6). **One weapon per commit**, play-test each (fires, hits, damages, respects cooldown). Author each `ZSkillConfig` from the GDD `Active Skills ` / `Pasive Skills` sheets. This is the bulk of the work.

**Slice 3 — Decouple the remaining UIs (§3.2).** Move any onClick/state logic still routed through `GameManager`/`UIManager` legacy facade into each UI's own components reading Domain content. UpgradeSkill's 3 slots, Gameplay's joystick/EXP/stats, Shop/Inventory currency display via observer (§3.9).

**Slice 4 — Currency to SO+observer (§3.9).** Replace per-frame `DataManager` polling with resource-pool change events. Keep PlayerPrefs save-compat.

**Slice 5 — Delete dead legacy managers**, GUID-checked, one per commit: `ControllerSpawening`, `BooleanManager`, `SpriteWeapons`, `GunManager`, `ManagerWeapons`, `SpawenManager`, `ManagerEnemys`, `PlayerManager`, the disabled `CameraController` block, etc. **Grep each GUID across `.unity`+`.prefab` first.** Remove the component from scene, then delete the file, then commit, then play-test.

**Slice 6 — Delete `GameManager` last.** By now nothing should reference it. Remove from scene, delete `GameManager.cs`, delete the `MigrationFlags` plumbing if fully dead, collapse `_LegacyManagers` root. Final play-test of the full loop.

**Slice 7 — Sync the wiki.** Rewrite the stale `.wiki/wiki/technical/scene-boot-flow.md` + `entity-adapter-pattern.md` to reflect the clean architecture, append a final `log.md` entry, update success criteria.

### Success criteria (end state)
- `find_gameobjects by_component` returns **0** for: `GameManager`, `PlayerManager`, `EnemyManager`, `JoystickManager`, `ManagerEnemys`, `BooleanManager`, `SpriteWeapons`, `LevelsManager`, `DATN.Legacy.UIManager`, `PlayerStats`, `DATNGameplayBridge`, `ControllerSpawening`.
- No `DATN`/no-namespace legacy logic scripts remain; `_LegacyManagers`/`_LegacyCompat` gone.
- Each skill has a runnable `ZSkillRuntime` GameObject; level-up actually upgrades stats.
- Currency updates via observer, not per-frame singleton polling.
- All UI decoupled — each holds its own components, none routes through a god-manager.
- One unbroken play-test: boot → MainMenu → Play → kill ≥10 enemies → level up → pick skill → die → Lose screen → MainMenu → replay. Every path works, no console errors.
- Wiki re-synced; final `log.md` entry written.

---

## 6. How to work each slice (the loop)

1. Read the relevant reference system (its `.wiki` page + its code in `survivorio/Assets/_GameSurvivorIO/Script/`).
2. If authoring data, read the matching GDD sheet.
3. Add new Luzart code beside the old, gated by a `MigrationFlags` bool (default off).
4. Compile clean → flip flag on → enter Play mode → verify the slice's behaviour + the full golden path → screenshot for visual diff.
5. Fix until green. Default the flag on, delete the old path, delete the flag if dead.
6. Commit with message `migrate(<area>): <slice>` — note any visual-wiring change or the §3.6 deviation explicitly.
7. Update `.wiki/wiki/log.md`. Move to the next slice. **Never batch deletions.**

If a slice can't be verified in Unity, **stop and report** rather than pressing on.

---

## 7. AUTONOMOUS MODE (full-send, unattended) — READ BEFORE STARTING

The developer has **explicitly authorized an unattended multi-hour run** and accepts the risk. They are away. There is no human to answer questions. **You make progress without stopping for confirmation — BUT under the strict safety contract below.** This contract exists because a previous unattended refactor on this exact project broke the game by mass-deleting and then "fixing" failures with more deletion. That failure mode is forbidden here.

### 8.1 The Prime Directive: fail = STOP + ROLLBACK, never improvise
When **any** verification step goes red (compile error, console error/NRE in Play mode, white screen, missing-reference exception, broken golden path):
1. **Do NOT try to "make the error go away" by deleting more code or loosening the slice.** That is the exact move that broke the game last time. It is banned.
2. `git revert` (or `git reset --hard` to the **last green commit** if the bad work is uncommitted) so the tree returns to a provably-playable state.
3. Append a `BLOCKED` entry to the progress log (§8.5) explaining what failed, your hypothesis, and what you'd try with a human present.
4. **Skip that slice** and move to the next *independent* slice. Do not let one red slice cascade.
5. If **three** slices in a row go red, **HALT the entire run**, leave the tree at the last green commit, and write a clear "needs human" summary at the top of the progress log. Stopping early with a working game is a SUCCESS, not a failure.

### 8.2 Atomic-commit invariant (your rollback safety net)
- **One slice = one commit.** Never bundle. The game must be playable at every commit — that is what makes `git revert` safe.
- Before starting a slice, confirm `git status` is clean (last slice fully committed). If dirty, commit or stash first — never start a slice on top of unverified work.
- Commit message: `migrate(<area>): <slice>` + a one-line `[autonomous]` tag + note any visual-wiring change or the §3.6 deviation.

### 8.3 The automated verification gate (run EVERY slice, in order)
A slice is only "green" if **all** of these pass. Use the Unity MCP tools.
1. **Compile clean** — `read_console` shows zero errors; `editor_state.isCompiling == false`.
2. **Enter Play mode** (`manage_editor` play) and let it run a few seconds.
3. **Console scan in Play** — zero Errors/Exceptions/NREs. Warnings are OK.
4. **Golden-path probe** — confirm the loop still reaches gameplay: Splash→MainMenu shows, Play enters gameplay, player object exists & is controllable, enemies spawn, HUD updates. Drive what you can via MCP; assert objects exist via `find_gameobjects`.
5. **Screenshot** (`manage_camera action=screenshot`) and **diff against the baseline** you captured at run start. A blank/black/obviously-broken frame = red.
6. **Exit Play mode** with no errors logged on exit.

If Unity is disconnected or won't enter Play mode, you **cannot verify** → **do not commit blind**. Halt and log it. Committing unverified work defeats the whole safety model.

### 8.4 Self-review pass (every slice, before the commit)
After code is written and before you commit, **review your own diff** (spawn a `code-reviewer` subagent for non-trivial slices). Check specifically:
- **Visual-freeze violations** — any edit to `.prefab`/`.unity` outside the narrow expected scope (added component / added child GO). Restyled RectTransform/Image/Text/Animator/particles = **revert immediately**.
- **GUID-unchecked deletion** — every `.cs` you delete must have had its GUID grep'd across `.unity`+`.prefab` first (catches disabled refs like the `CameraController` trap, §2). No grep = don't delete.
- **Scope creep** — did the slice do only what it claimed? Extra "while I'm here" refactors are banned in autonomous mode.
- **Flag default** — new code is gated by a `MigrationFlags` bool defaulting OFF until the cutover step.

### 8.5 Progress log + human review checklist (your report home)
Maintain `docs/superpowers/AUTONOMOUS-RUN-PROGRESS.md`, appended after every slice. It is the first thing the developer reads when they return. Structure:
- **Header (kept current):** overall status (`RUNNING` / `HALTED-needs-human` / `DONE`), last green commit SHA, slices done / rolled-back / skipped.
- **Per slice:** name, commit SHA, what changed, verification result (which gate steps passed), screenshot path.
- **⚠️ MUST-REPLAY-BY-HAND checklist (the most important part):** Because the machine **cannot judge gameplay feel**, list every slice whose *correctness depends on feel* — each ported weapon (does it fire/aim/cooldown right?), the HP-bridge reversal (does damage/death feel right?), any UI interaction. The developer plays each of these by hand. Be specific: "Kunai: verify it auto-targets nearest enemy and respects 2s cooldown per GDD."
- **BLOCKED items:** anything reverted/skipped and why.

### 8.6 Risk-ordered execution (front-load the safe value)
Even full-send, do the **safest, machine-verifiable slices first** so maximum value is banked before any risky slice might trip the 3-strike halt:
1. Slice 0 re-audit (read-only) → 2. Author SOs from GDD (additive, no deletion) → 3. Add scaffold code with flags OFF (dormant, additive) → 4. GUID-verified orphan deletions (one at a time) → **then** the feel-dependent cutovers (HP bridge, 12 weapon ports, GameManager deletion) **last**, each flagged in §8.5 for hand-replay.

### 8.7 How to keep the run going
Work slice → verify → self-review → commit → log → next slice, looping until either all slices are done, the 3-strike halt trips, or Unity disconnects. Do not pause to ask the developer anything — log decisions instead and keep moving. When everything in §5's success criteria is met (or you halt), set the progress-log header to `DONE`/`HALTED` and stop.

### 8.8 What "done when they're back" honestly means
You will hand back: a game that **compiles, boots, and plays the golden path** (machine-verified), with as many slices completed as safely fit in the window, **a clean per-commit history they can revert**, and a **precise checklist of what to play-test by hand**. You will NOT hand back "100% verified perfect" — feel-testing is theirs. Optimize for *a working game + maximum safe progress + a clear report*, not for racing through every slice.

---

## 8. One-paragraph TL;DR for the executor

Finish migrating GoGo Survival off its legacy DATN stack onto the clean Luzart framework, using the sibling `survivorio` project (and its `.wiki`) as the blueprint and the `GDD - GoGo Survival.xlsx` as the data source. Keep **all visuals frozen**, delete **only legacy logic**, work **one verifiable slice per commit** from leaves (weapons, managers) inward to the `GameManager` trunk, gate risky cutovers behind `MigrationFlags`, and play-test the full Splash→MainMenu→Play→die→replay loop after every commit. Skills become GameObject children (`ZSkillRuntime`), projectiles become GameObject+SO, stats/items/currency become SO-driven with observer updates, and every UI owns its own components. No overnight mass-deletion — the last one broke the game.
