# Autonomous Run Progress — Luzart Full-Clean Migration

> First thing to read when you return. Updated after every slice.

## Header (kept current)

- **Status:** `RUNNING`
- **Run started:** 2026-05-30
- **Last green commit:** `8e42612` (baseline at run start)
- **Slices done:** 0 · **Rolled-back:** 0 · **Skipped:** 0
- **Consecutive red slices:** 0 / 3 (halt threshold)

## Environment / path mapping (macOS prompt → this Windows machine)

| Role | Prompt path (macOS) | Actual path (this machine) |
|---|---|---|
| Target (edit) | `/Users/luzart/.../DATN-GoGoSurvival` | `D:\Unity Training\survivorIOSource\DATN-GoGoSurvival` |
| Reference (read-only) | `/Users/luzart/.../survivorio` | `D:\Unity Training\IO_Training` |
| GDD (read-only) | `/Users/luzart/.../GDD - GoGo Survival.xlsx` | `D:\Unity Training\survivorIOSource\DATN-GoGoSurvival\GDD - GoGo Survival.xlsx` |

- Unity Editor: **connected via MCP**, target project open, active scene `Assets/_Main/Scenes/GamePlay.unity`. Verification gate available.
- Python 3.14.2, `openpyxl` 3.1.5 installed.

## ⚠️ Note on the prompt's assumptions

- The prompt says the reference has "an excellent wiki at `survivorio/.wiki/wiki/systems/`". **On this machine that wiki does NOT exist** — `IO_Training/.wiki/wiki/` only has an empty `analysis/` folder. Per the developer's explicit instruction, **Phase A of this run is to research the reference deeply and BUILD its wiki systems pages** to serve as the blueprint before migrating.

---

## Phase A — Reference research & wiki build (read-only, no risk) — ✅ DONE

Built the reference blueprint wiki at `D:\Unity Training\IO_Training\.wiki\wiki\` (it did **not** exist before — only an empty `analysis/` folder). Pages written (via 4 parallel code-audit passes over the reference's 288 C# files):
`systems/architecture-bigpicture`, `game-init-flow`, `skill-cooldown-attack-loop`, `projectile-types`, `stat-system`, `modifier-pipeline`, `item-equipment`, `currency-resource-pools`, plus `index.md` + `log.md`. Only `.wiki/wiki/` written; `.wiki/raw/` untouched. (Reference repo left uncommitted — read-only blueprint, not part of target history.)

---

## Slice 0 — Re-audit (read-only) — ✅ DONE

**Baseline verification (the green-state diff anchor):** compile clean (no CS errors; only MCP transport noise), Play mode 0 runtime errors, `LuzartPlayerController` player exists & renders (`Assets/Screenshots/baseline_runstart.png`), clean exit. **Verification gate confirmed working.**

### Confirmed runtime state — `MigrationFlags.asset`
- `UseLuzartPlayerController = 1` (ON) → legacy `PlayerManager` collision-damage gated OFF
- `UseLuzartPlayerEntityRoot = 1` (ON) → Luzart entity owns player
- `UseLuzartEnemyEntityRoot = 1` (ON) → Luzart entity owns enemies
- `FrameworkOwnsPlayerHP = 0` (OFF) → **HP bridge not yet reversed** (Slice 1 target)

### Live legacy components in `GamePlay.unity` (GUID-verified in scene YAML)
| Component | GUID | scene `m_Enabled` | note |
|---|---|---|---|
| GameManager | 68aa399b0c0c2e0478187695089f9037 | **1** | trunk; delete last |
| PlayerManager | 7161c7bf9f808f947b564d3d79921d5b | **1** | collision dmg gated off by flag |
| ManagerWeapons | b0bef7c01486e58498dd80f98630b928 | **1** | reads legacy XP fill bar |
| ControllerSpawening | 531185d8940e3aa4298e2139e15afd95 | **1** | drives SpawenManager spawn cycle |
| GunManager | 4415879f0b6358742a2a31f8641d1270 | **1** | **ACTIVE weapon path — fires bolts ~every 2s, no flag gate** |
| BooleanManager | 3f638ed77bc5d2a4e9186503863bb31b | **1** | settings singleton, 19 refs |
| SpriteWeapons | 0f82e15807c0d5548b5d803592c806e7 | **1** | UI visual toggles only (no dmg) |
| SpawenManager ×2 | a128190934d71fd4cb2017f3738aa004 | **0** (disabled) | still referenced by ControllerSpawening (7 refs) — NOT a free delete |
| ManagerEnemys | 88d96010bf1dda042afa57c3e4ae7573 | **0** (disabled) | orphaned, ~0 direct C# refs — safe-ish delete candidate |
| CameraController (legacy) | 3b3cb9cd94424674c9ab27b16ef001c3 | **0** (disabled) | **the §2 trap** — file `Assets/_Main/Scripts/Player/CameraController.cs`; disabled block in scene |
| EnemyManager | b905c2d056df7444ba090a3722493036 | n/a | NOT in scene — on enemy prefabs `Monster.prefab`, `Zombie.prefab`; gated by `UseLuzartEnemyEntityRoot` |

### Corrections to the prompt's §2 (verify-against-live findings)
1. **Weapons are NOT yet on Luzart at runtime.** The Luzart `ZSkill` scaffold exists (`Assets/_Main/Scripts/_LuzartGame/Skills/` — `ZSkillRuntime : MonoBehaviour`, `ZSkillConfig`, behaviors) **but the active firing is still legacy `GunManager`**, and the `ZSkillBehavior_*` files appear to be **empty/stub scaffolds** (behavior list empty at runtime). So "Slice 2 — port 12 weapons" = *implement the empty behaviors + author `ZSkillConfig` assets from GDD + wire to player Skills/ container + disable GunManager*. This is large and feel-dependent → LAST, flagged for hand-replay.
2. **HP / death flag ambiguity:** §2 says `LuzartOwnsDeath()` keys off `UseLuzartPlayerController`; the live read of `GameManager.LuzartOwnsDeath()` appears to key off `FrameworkOwnsPlayerHP`. **Must verify the exact branch before Slice 1.**
3. **Currency is 100% legacy** (`CurrencyManager` + `DataManager` + PlayerPrefs, ~15 refs each). No Luzart `ResourcePool` exists yet → Slice 4 is net-new additive work.
4. **`_LegacyCompat/_FrameworkStubs.cs` is compile-critical** (defines `IView`, `ViewT<T>`, `Data_ClassicEndGame`, `PopupSkillUpgradeData` used by the live death→Lose path). **Do NOT delete `_LegacyCompat` until NinjaUI fully owns those types.**

### Corrected, risk-ordered slice list for THIS run
- **S0** re-audit (this) — docs only, commit. ✅
- **Safe orphan deletions first** (already-disabled, GUID-verified, one commit each, play-verify each):
  - **S-A** legacy `CameraController` (GUID 3b3cb9cd, disabled block) — remove scene block + delete `.cs` iff 0 active C# refs.
  - **S-B** disabled `ManagerEnemys` — remove scene component + delete `.cs` iff orphaned.
- **Deferred / feel-dependent (LAST, flagged for hand-replay):**
  - **S1** reverse HP bridge (flip `FrameworkOwnsPlayerHP`) — verify `LuzartOwnsDeath` branch first.
  - **S2** implement+author+wire the 12 Luzart weapons, then retire `GunManager`/`ManagerWeapons`.
  - **S4** currency → `ResourcePool` + observer (net-new).
  - **S-C** `SpawenManager` ×2 / `ControllerSpawening` (coupled to live spawn cycle).
  - **S6** `GameManager` trunk — last.
  - **S3** UI decouple · **S7** wiki sync.

---

## Per-slice log

### Slice 0 — re-audit — commit `<pending>` — docs only, no gameplay change
- Changed: `docs/superpowers/AUTONOMOUS-RUN-PROGRESS.md`, target `.wiki/wiki/log.md`. Reference wiki built under `IO_Training/.wiki/wiki/`.
- Verification: baseline Play-mode green (see above). No gameplay code/scene touched → no regression risk.

---

## ⚠️ MUST-REPLAY-BY-HAND checklist

_(populated as feel-dependent slices land)_

---

## BLOCKED items

_(none yet)_
