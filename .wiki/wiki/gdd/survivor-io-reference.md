---
title: Survivor.io Reference GDD (canonical)
category: gdd
tags: [gdd, survivor-io, gameplay, reference, canonical]
sources: [GDD - GoGo Survival.xlsx, survivorio/.wiki/wiki/overview.md, survivorio/.wiki/wiki/systems/game-init-flow.md]
created: 2026-05-17
updated: 2026-05-17
---

# Survivor.io Reference GDD — canonical for GoGo Survival

> **Purpose**: single source of truth for what gameplay should look like in DATN-GoGoSurvival.
> Combines: (1) Survivor.io original game mechanics, (2) user's `GDD - GoGo Survival.xlsx`,
> (3) framework constraints from the `survivorio/` Unity reference project.
> When code disagrees with this doc, fix the code.

---

## 1. Genre and core loop

**Genre**: top-down 2D auto-attack survivor / horde shooter (Survivor.io / Vampire Survivors lineage).

**Core 5-minute loop**:
```
Player chooses level/map → drops into arena → joystick movement only
   ↓
Auto-attack: weapons fire on cooldown at nearest enemy in range
   ↓
Wave spawner pushes hordes of enemies that move toward player
   ↓
Kill enemies → XP gems drop on ground → walk over to absorb
   ↓
XP fills bar → LEVEL UP → freeze game, show 3 random skill cards
   ↓
Pick 1 → resume → repeat for ~10-15 minutes
   ↓
Survive to clock-end → WIN, or HP=0 → LOSE
```

**Survival window**: classic Survivor.io stage is 12-15 minutes, with wave intensity escalating roughly every 60s. Boss spawns at ~50% and ~100% wave timers.

---

## 2. Game flow — screen by screen

This is the **MANDATORY** sequence for GoGo Survival. The previous "Refactor UI" session broke this; fixed code now matches.

```
┌──────────┐   fill bar    ┌──────────┐   Play btn   ┌──────────┐
│  Splash  │ ───────────► │ MainMenu │ ───────────► │ Gameplay │
└──────────┘               └──────────┘              └────┬─────┘
                              ▲ ▲                         │
                              │ │                         │ HP=0       │ clock-end
                              │ │                         ▼            ▼
                              │ │                    ┌──────────┐ ┌──────────┐
                              │ └────────────────────│   Lose   │ │   Win    │
                              │                      └────┬─────┘ └────┬─────┘
                              │                           │ MainMenu   │ Continue
                              └───────────────────────────┴────────────┘
                                                          │ Retry
                                                          ▼
                                                     (back to Gameplay
                                                      fresh state)
```

### MainMenu — side branches (not part of main flow)
```
                    ┌─────────┐
   MainMenu ───────►│  Shop   │ buy weapons / equipment with coins
                    └─────────┘
                    ┌──────────┐
   MainMenu ───────►│Equipment │ equip / enhance owned gear (5 sets x 6 slots)
                    └──────────┘
                    ┌──────────┐
   MainMenu ───────►│ Settings │ music / sound / vibration / language
                    └──────────┘
                    ┌──────────┐
   MainMenu ───────►│ Messages │ in-game mail / notifications
                    └──────────┘
```
Each side screen returns to MainMenu via close/back. **Never** to gameplay directly.

### In-gameplay popups (do NOT switch scene / screen)
- **PausePopup** — Resume / Home (= back to MainMenu) / Sound toggle
- **LevelUpPopup** — force-pick 3 skill cards; ESC blocked; `Time.timeScale=0`
- **SettingsPopup** (sub) — from PausePopup → music/sound/vibration

---

## 3. Player character

| Attribute       | Default | Source                                                              |
|---|---|---|
| Max HP          | 100     | `StatsBehavior` fallback ([DATNGameplayBridge:53](Assets/_Main/Scripts/_LuzartGame/Gameplay/System/DATNGameplayBridge.cs:53)) |
| Move Speed      | 5       | StatsBehavior fallback                                              |
| Base ATK        | 10      | StatsBehavior fallback                                              |
| Auto-attack     | yes     | Skill behaviors fire on cooldown                                    |
| Weapons capacity | up to 6 active skills + 6 passive |  Survivor.io standard          |

**Movement**: joystick only (drag anywhere on left-half of screen). No tap-to-shoot.

**Death**: HP→0 fires `GameController.OnHPChange` → `Broadcaster.Broadcast(Data_ClassicEndGame{IsWin=false})` → `SV_EndGameBridge` shows `SV_LoseScreen`.

---

## 4. Active Skills (10) — from user GDD

5-star scaling per skill. Each star raises ATK multiplier and/or scale and/or count.
Stored in `Assets/_Main/Data/Skills/SV_SkillCatalog.asset`.

| # | Skill          | Mechanic                                                         | ★→★★★★★ ATK mult |
|---|---|---|---|
| 1 | **Kunai**      | Default weapon; 1 kunai forward, +1 per star                     | 1.5 → 5.5         |
| 2 | **Boomerang**  | Throws boomerang(s) that return; +1 boomerang at ★★, double dmg at ★★★ | 2.4 → 6.0   |
| 3 | **Brick**      | Drops bricks above the player in patterns                        | 1.0 → 1.5         |
| 4 | **Drill Shot** | Projectile that pierces multiple enemies                          | per excel         |
| 5 | **Durian**     | Bomb-pattern AoE                                                  | per excel         |
| 6 | **Forcefield** | Orbits player damaging contact enemies                            | per excel         |
| 7 | **Guardian**   | Lightning chain to nearest enemy                                   | per excel         |
| 8 | **Molotov**    | Throws AoE fire DoT zones                                          | per excel         |
| 9 | **RPG**        | High-damage rocket projectile                                      | per excel         |
| 10| **Soccer Ball**| Bouncing projectile                                                | per excel         |

> [!info] Icon coverage: 9/10 done. Soccer Ball icon missing. See [[open-questions#q-20260516-04]].

---

## 5. Passive Skills (12) — from user GDD

| # | Skill              | Effect (★ → ★★★★★)               |
|---|---|---|
| 1 | **Hi-Power Magnet**| Item loot range +100% → +500%   |
| 2 | **Fitness Guide**  | Max HP +20% → +100%             |
| 3 | **Ammo Thruster**  | Bullet flight speed +10% → +50% |
| 4 | **HE Fuel**        | Ammo/weapon range +10% → +50%   |
| 5 | **Energy Drink**   | Restores 1% HP/5s → 5% HP/5s    |
| 6 | **Exo-Bracer**     | DoT duration +10% → +50%        |
| 7 | **Energy Cube**    | All attack CD -8% → -40%        |
| 8 | **Oil Bond**       | Gold gain +8% → +40%            |
| 9 | **Ronin Oyoroi**   | Received damage -10% → -50%     |
| 10| **Sports Shoes**   | Movement Speed +10% → +50%      |
| 11| **Hi-Power Bullet**| (see excel — icon missing)      |
| 12| **Koga Ninja Scroll**| (see excel)                   |

Stored in same `SV_SkillCatalog.asset`. Behavior configs in `Assets/_Main/Data/Skills/Behaviors/` (all `ZSkillBehaviorConfig_AddStat`).

---

## 6. Equipment system (meta progression — between runs)

**5 Sets × 6 slots = 30 items** in user GDD. Currently 26 authored (some sets have 5 slots).

### Quality tiers (7) — base stats scale per tier
`Normal → Good → Better → Excellent → Epic → Legendary → Relic`
(GDD only shows 5; framework extrapolated to 7.)

### Slots
1. **Weapon** — primary ATK source (Kunai is starter)
2. **Necklace** — ATK
3. **Gloves** — ATK
4. **Armor** — HP
5. **Belt** — HP
6. **Shoes** — HP

### Sets
- **Army Set** (6 slots): Kunai, Army Nameplate, Army Gloves, Army Uniform, Army Belt, Army Boots
- **Monster Set** (5): Bone Pendant, Leather Gloves, Carapace, Leather Belt, Prosthetic Legs
- **Protective Set** (5): Emerald Pendant, Protective Gloves, Protective Suit, Broad Waistguard, Layered Snowshoes
- **Metal Set** (5): Metal Neckguard, Shiny Wristguard, Full Metal Suit, Waist Sensor, Light Runners
- **Stylish Set** (5): Trendy Charm, Fingerless Gloves, Traveler's Jacket, Stylish Belt, Stylish Boots

### Enhance levels
- 10 levels per item, each +5% bonus on base stat
- Cost ramps exponentially in coins (see `EquipmentData.enhanceCostCoins[10]`)

### Grade skills (per quality tier)
Each item carries 3 grade-conditional skills (Excellent / Epic / Legendary). Examples from Army Set:
- Kunai (Excellent): "Start with LV.2 Kunai"
- Army Nameplate (Excellent): "+50% damage in 10s after Elite/Boss kill"
- Army Uniform (Excellent): "Heal 3% HP/5s"

---

## 7. Enemies (4 types per GDD)

| Type                 | HP   | DMG  | Speed | Behavior                                  | Drops              |
|---|---|---|---|---|---|
| **Regular Zombie**   | 100  | 5    | 2.5   | Walks at player, contact damage           | Small Biofuel      |
| **Zombie Hound**     | 80   | 4    | 3.5   | Fast walker, contact                      | Small Biofuel      |
| **Elite Zombie Hound**| 400 | 10   | 3.0   | Tougher elite version, drops upgrade box  | Upgrade Box (excl) |
| **Boss Boucebloom**  | 5000 | 25   | 1.5   | Flower boss; 3-shot pattern at intervals  | Upgrade Box        |

Per-wave scaling: HP +15%/wave, DMG +10%/wave, Speed +2%/wave (current `EnemyData` SOs).

---

## 8. Drops (9 types per GDD)

| Item              | Type   | Value | Notes                                    |
|---|---|---|---|
| Small Biofuel     | XP     | 10    | Most common drop                         |
| Medium Biofuel    | XP     | 20    |                                          |
| Big Biofuel       | XP     | 50    | Boss/elite drop                          |
| Small Coin        | Coin   | 10    | Currency for shop/enhance                |
| Medium Coin       | Coin   | 20    |                                          |
| Big Coin          | Coin   | 50    |                                          |
| Magnet            | Magnet | -     | Absorbs all XP+coins in radius 30        |
| Food              | Heal   | 20%   | Restores 20% max HP                      |
| Bomb              | Bomb   | 200   | Damages enemies in radius 8              |

> [!info] Upgrade Box: excel marks `delete` — out of scope for thesis.

---

## 9. Level / XP curve (from GDD Note sheet)

| Level Band | XP per kill (avg) | Total XP to next |
|---|---|---|
| 1-20       | ~10               | 50                |
| 21-40      | ~10               | 60                |
| 41-60      | ~10               | 70                |
| 61-80      | ~10               | 80                |

**Player Level bonuses (every 20 levels)**:
- Lv 20: HP +1000
- Lv 40: Crit Rate +5%
- Lv 60: ATK +600
- Lv 80: Heal +5% on level up
- Lv 100: +1000 HP
- Lv 120: Crit Rate +5%

---

## 10. Economy / resources

| Resource | Source                              | Sink                          |
|---|---|---|
| **Coin** | drops in gameplay, win rewards      | shop purchases, item enhance   |
| **Gem**  | rare drops, premium / IAP placeholder| premium shop                  |
| **Energy** | meta gate to entry (regen over time)| play one stage = N energy    |
| **XP (in-run)**| biofuel pickups               | level-up popup picker         |

Note: thesis decision = **no Google AdMob** (no dev account); IAP is mocked.

---

## 11. Settings (in-game)

Toggles in `SV_SettingsPopup`:
- Music on/off
- Sound (SFX) on/off
- Vibration on/off

(Language, FPS counter, etc. — optional for thesis.)

---

## 12. Save data (`DataManager` legacy + `SaveService` framework)

Per `survivorio/.wiki/wiki/systems/game-init-flow.md` and DATN's `DataManager.cs`:

- `level` — highest unlocked level
- `namePlayer` — chosen at first launch
- `idAvt` — avatar id
- `timeFirstTime` — first-launch timestamp
- `isUserIAP` — premium flag
- `adsLimit` — daily ad watch counter (legacy; can ignore)
- + framework `SaveService` JSON for ISaveable subsystems (resources, unlocked equipment, etc.)

---

## 13. Conformance checklist (for code review)

When working on this project, every change should preserve these invariants:

- [ ] Splash auto-plays once on app start; only shows on cold start
- [ ] MainMenu is the **only** screen the player returns to after Win/Lose
- [ ] Pause does not switch scene — overlay popup with `Time.timeScale=0`
- [ ] LevelUp popup blocks gameplay until pick (ESC disabled)
- [ ] Win/Lose screen → MainMenu **resets** player HP, XP, level, wave, kills, spawned enemies, instantiated level prefab
- [ ] Win/Lose screen → Retry does same reset, then re-enters gameplay
- [ ] Shop / Equipment / Settings / Messages are reachable **only** from MainMenu
- [ ] All drops (XP/Coin/Magnet/Food/Bomb) implemented per §8
- [ ] At least 10 active + 12 passive skills usable in level-up roll
- [ ] At least 4 enemy types spawnable; boss at end of stage
- [ ] Save load works between sessions (DataManager + SaveService)

---

## Backlinks
- [[overview]]
- [[systems/ui-flow]]
- [[systems/skill-system]]
- [[systems/equipment-system]]
- [[systems/enemy-spawn]]
- [[systems/drop-system]]
