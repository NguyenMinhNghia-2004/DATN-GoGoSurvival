# PreGame Stat Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a PreGame stat-modifier layer (equipment / permanent progression) whose Final value feeds as the BASE of the existing InGame layer, faithfully mirroring IO_Training's two-pipeline architecture.

**Architecture:** GoGo currently has ONLY an InGame modifier pipeline (`Data/ModifierAndInGame/Player/<Stat>/`), and equipment items dump their factors directly into it. IO_Training instead has TWO parallel pipelines: equipment → **PreGame** mods → `Number_Player_PreGame_Final_<Stat>`, which is referenced as the `baseNumber` of `Number_Player_InGame_Final_<Stat>`; in-game skills feed the InGame mods. We replicate this by (1) generating a PreGame layer that REUSES the existing shared `ModDef_Player_*_<Stat>` definitions, (2) relocating equipment's serialized modifier references from `Mod_Player_InGame_*` to `Mod_Player_PreGame_*`, (3) repointing each InGame Final's base to the matching PreGame Final, (4) registering the PreGame SOs into the scene Domain. The whole change is data-only (no prefab, no gameplay-balance change) until the optional final activation task.

**Tech Stack:** Unity 6 (Luzart/Crystal framework), C# ScriptableObjects, deterministic Python YAML generation (`docs/tools/*.py`), MCP-for-Unity for refresh/compile/play verification.

---

## Background facts (verified 2026-06-12)

- **The bridge is data-driven, not code.** IO_Training: `Number_Player_InGame_Final_ATK.baseNumber.asset` (guid `ca90c40b…`) resolves to `…/ModifierAndPreGame/Player/ATK/Number_Player_PreGame_Final_ATK.asset`. Same `AssetNumber_SimpleBoosted` class (script guid `8657fc30…`) used for both layers.
- **GoGo gap:** no `ModifierAndPreGame` folder. `Number_Player_InGame_Final_<Stat>.baseNumber` → `Number_Player_InGame_Base_<Stat>` (an `AssetNumber_Constant`, e.g. ATK=10, HPMax=1000).
- **Equipment → modifier runtime path:** `AssetEquipmentSlot.EquipItem` → `RuntimeModifierFactorGroup.Activate()` → `f.Modifier.AddFactor(f.Factor)` → `AssetModifier_ContributeToAggregatedNumber.OnFactorIncluded` → `ContributionNumber.Contribute(factor.Value)`. The `Modifier` reference per factor lives in the **serialized item asset** (`ItemConfig_*.asset` → `upgradeItemConfigs[].modifierFactors[].modifier: {guid}`), NOT in code. `ItemConfig.cs:154 AddAllFactor` + `FindItemEditor.GetAssetModifierPreGame` are dead leftover authoring code (they search assets literally named "PreGame", which don't exist yet → return null).
- **AddFactor validates `factor.Definition == modifier.Definition`** (`AssetModifier.cs`). Each item factor's `definition` is a shared `ModDef_Player_<Kind>_<Stat>` asset. PreGame mods MUST reference those SAME ModDefs so item factors still validate → **REUSE ModDefs, do not create new ones.**
- **Items only carry HPMax and ATK factors**, each as `AddNormal` + `AddRatio` pairs (e.g. `ItemConfig_Army_Belt_Better` references `Mod_Player_InGame_HPMax_AddNormal` + `Mod_Player_InGame_HPMax_AddRatio`). The guid swap map must still cover all 8 stats × 3 kinds = 24 mods for safety.
- **Existing guid map:** `docs/created-guids-modifier-infra.json` (96 keys) maps every InGame asset name → guid (ModDefs, Numbers, Mods, Base constants).
- **Domain registration:** `IOPortDataGenerator.RegisterModifierInfra()` scans `Assets/_Main/Data/ModifierAndInGame` and adds every ScriptableObject to the scene `DomainContentLoader`. PreGame SOs need the same treatment or their `DoInitialize` never runs.
- **Gameplay activation is STILL pending:** `Stat_Player_<Stat>.asset` value is `mode:0` (Constant) — so NONE of the modifier pipeline reaches `StatsBehavior.Get()` yet, in either layer. This means Tasks 1–6 are **inert at the gameplay level** (zero balance change) and safe to land. Task 8 (activation) is the only behavior-changing step and is optional + flagged.

## ⚠️ Safety constraints (from project memory)

- **DO NOT run any prefab-regenerating generator** (`Tools/IOPort/Build Prefabs`, `Generate Shop+Equip Data`, etc.). `PopupShop.prefab` is currently uncommitted (`M`); those generators would overwrite it. This plan is **data-only** and touches no prefab.
- **Play mode blocks editor recompile.** Before running any C# editor menu after a `.cs` edit, `manage_editor stop` and wait ~15s.
- **Commit only this plan's own files.** Never `git add -A`. Leave the unrelated `M` PopupShop.prefab / TMP asset alone.
- Verify gate each task: `read_console` (errors) must be clean before commit.

## File Structure

- **Create:** `docs/tools/gen_pregame_infra.py` — generates the 72 PreGame SOs (reusing ModDef guids) + dumps `docs/created-guids-pregame-infra.json`.
- **Create:** `docs/tools/rewire_items_to_pregame.py` — swaps item modifier guids InGame→PreGame, and repoints InGame Final base → PreGame Final.
- **Create (by generator):** `Assets/_Main/Data/ModifierAndPreGame/Player/<Stat>/…` — 72 new `.asset`+`.meta` (8 stats × 9 SOs).
- **Modify:** 130× `Assets/_Main/Data/Items/**/ItemConfig_*.asset` (guid swaps only).
- **Modify:** 8× `Assets/_Main/Data/ModifierAndInGame/Player/<Stat>/Number_Player_InGame_Final_<Stat>.asset` (baseNumber asset guid).
- **Modify:** `Assets/_Main/Editor/IOPortDataGenerator.cs` (`RegisterModifierInfra` to also scan `ModifierAndPreGame`).
- **Optional (Task 8):** 8× `Assets/_Main/Data/Stats/Player/Stat_Player_<Stat>.asset` (Constant → AssetNumber).

Per-stat PreGame SO set (9 each, ModDefs reused from InGame):
```
Number_Player_PreGame_Base_<Stat>           (AssetNumber_Constant, same base value as InGame)
Number_Player_PreGame_TotalAddNormal_<Stat> (AssetNumber_Aggregation Sum, empty)
Number_Player_PreGame_TotalAddRatio_<Stat>  (AssetNumber_Aggregation Sum, empty)
Number_Player_PreGame_TotalAddSubRatio_<Stat> (Aggregation Sum of [const 1, TotalAddRatio])
Number_Player_PreGame_TotalMultiply_<Stat>  (Aggregation Multiply of [const 1])
Number_Player_PreGame_Final_<Stat>          (SimpleBoosted: base=Base, add=TotalAddNormal, mul=TotalAddSubRatio)
Mod_Player_PreGame_<Stat>_AddNormal         (def=ModDef_AddNormal_<Stat>, contribution=TotalAddNormal)
Mod_Player_PreGame_<Stat>_AddRatio          (def=ModDef_AddRatio_<Stat>,  contribution=TotalAddRatio)
Mod_Player_PreGame_<Stat>_Multiply          (def=ModDef_Multiply_<Stat>,  contribution=TotalMultiply)
```

---

### Task 1: Write the PreGame infra generator

**Files:**
- Create: `docs/tools/gen_pregame_infra.py`

- [ ] **Step 1: Write the generator script**

```python
"""
Generate the PreGame modifier+aggregation pipeline for 8 player stats, mirroring
the existing InGame layer but REUSING the shared ModDef_Player_*_<Stat> definitions.
Output: Assets/_Main/Data/ModifierAndPreGame/Player/<Stat>/...
Run from project root:  python docs/tools/gen_pregame_infra.py
"""
import os, sys, io, json, uuid
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

INGAME = json.load(open('docs/created-guids-modifier-infra.json', encoding='utf-8'))

SG = {  # script guids (identical to gen_modifier_infra.py)
    'AssetModifier_ContributeToAggregatedNumber': 'c6e54ce9a0886de4fbf23f641cc5ade3',
    'AssetNumber_Constant':      '538fdae1998489b498206b2d9d675495',
    'AssetNumber_Aggregation':   'b691dab8d6c2f16449978ce0d28b190c',
    'AssetNumber_SimpleBoosted': '8657fc305e8686c429dcfb1d5b10d381',
}
STATS = [('HPMax',1000.0),('ATK',10.0),('Speed',4.0),('Cooldown',1.0),
         ('FireSpeed',10.0),('Heal',0.0),('Armor',0.0),('RangeFind',8.0)]
CREATED = {}

def gen(): return uuid.uuid4().hex
def write_asset(folder, fname, body):
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, fname).replace(os.sep, '/')
    guid = gen()
    with open(path, 'w', encoding='utf-8', newline='\n') as f: f.write(body)
    with open(path + '.meta', 'w', encoding='utf-8', newline='\n') as f:
        f.write(f'fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n'
                f'  externalObjects: {{}}\n  mainObjectFileID: 11400000\n'
                f'  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    return guid
def header(sg, name, cid):
    return ('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\n'
            'MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
            '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n'
            f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {sg}, type: 3}}\n'
            f'  m_Name: {name}\n  m_EditorClassIdentifier: {cid}\n')
def ref(g): return ('{fileID: 11400000, guid: ' + g + ', type: 2}') if g else '{fileID: 0}'

def make_constant(name, folder, value):
    b = header(SG['AssetNumber_Constant'], name, 'Assembly-CSharp::Luzart.AssetNumber_Constant')
    b += f'  _id: {name}\n  value: {value}\n'
    CREATED[name] = write_asset(folder, name + '.asset', b)
def make_aggregation(name, folder, mode, numbers):
    b = header(SG['AssetNumber_Aggregation'], name, 'Assembly-CSharp::Luzart.AssetNumber_Aggregation')
    b += f'  _id: {name}\n  aggregationMode: {mode}\n'
    if not numbers:
        b += '  numbers: []\n'
    else:
        b += '  numbers:\n'
        for (m, c, g) in numbers:
            b += f'  - mode: {m}\n    constant: {c}\n    asset: {ref(g)}\n'
    CREATED[name] = write_asset(folder, name + '.asset', b)
def make_simple_boosted(name, folder, base_g, add_g, mul_g):
    b = header(SG['AssetNumber_SimpleBoosted'], name, 'Assembly-CSharp::Luzart.AssetNumber_SimpleBoosted')
    b += f'  _id: {name}\n'
    b += f'  baseNumber:\n    mode: 1\n    constant: 0\n    asset: {ref(base_g)}\n'
    b += f'  addNumber:\n    mode: 1\n    constant: 0\n    asset: {ref(add_g)}\n'
    b += f'  multiplyNumber:\n    mode: 1\n    constant: 0\n    asset: {ref(mul_g)}\n'
    b += '  powNumber:\n    mode: 0\n    constant: 1\n    asset: {fileID: 0}\n'
    CREATED[name] = write_asset(folder, name + '.asset', b)
def make_modifier(name, folder, def_g, contrib_g):
    b = header(SG['AssetModifier_ContributeToAggregatedNumber'], name,
               'Assembly-CSharp::Luzart.AssetModifier_ContributeToAggregatedNumber')
    b += f'  _id: {name}\n  definition: {ref(def_g)}\n  factors: []\n  contributionNumber: {ref(contrib_g)}\n'
    CREATED[name] = write_asset(folder, name + '.asset', b)

base_folder = 'Assets/_Main/Data/ModifierAndPreGame/Player'
for stat, base_value in STATS:
    sf = f'{base_folder}/{stat}'
    make_constant(f'Number_Player_PreGame_Base_{stat}', sf, base_value)
    make_aggregation(f'Number_Player_PreGame_TotalAddNormal_{stat}', sf, 0, [])
    make_aggregation(f'Number_Player_PreGame_TotalAddRatio_{stat}', sf, 0, [])
    addratio_g = CREATED[f'Number_Player_PreGame_TotalAddRatio_{stat}']
    make_aggregation(f'Number_Player_PreGame_TotalAddSubRatio_{stat}', sf, 0,
                     [(0, 1, None), (1, 0, addratio_g)])
    make_aggregation(f'Number_Player_PreGame_TotalMultiply_{stat}', sf, 1, [(0, 1, None)])
    make_simple_boosted(f'Number_Player_PreGame_Final_{stat}', sf,
                        CREATED[f'Number_Player_PreGame_Base_{stat}'],
                        CREATED[f'Number_Player_PreGame_TotalAddNormal_{stat}'],
                        CREATED[f'Number_Player_PreGame_TotalAddSubRatio_{stat}'])
    # REUSE existing ModDef guids from the InGame index
    make_modifier(f'Mod_Player_PreGame_{stat}_AddNormal', sf,
                  INGAME[f'ModDef_Player_AddNormal_{stat}'],
                  CREATED[f'Number_Player_PreGame_TotalAddNormal_{stat}'])
    make_modifier(f'Mod_Player_PreGame_{stat}_AddRatio', sf,
                  INGAME[f'ModDef_Player_AddRatio_{stat}'],
                  CREATED[f'Number_Player_PreGame_TotalAddRatio_{stat}'])
    make_modifier(f'Mod_Player_PreGame_{stat}_Multiply', sf,
                  INGAME[f'ModDef_Player_Multiply_{stat}'],
                  CREATED[f'Number_Player_PreGame_TotalMultiply_{stat}'])
    print(f'  {stat}: 9 SOs')

json.dump(CREATED, open('docs/created-guids-pregame-infra.json', 'w', encoding='utf-8'),
          ensure_ascii=False, indent=1)
print(f'\nDone. {len(CREATED)} PreGame SOs created.')
```

- [ ] **Step 2: Run the generator**

Run: `cd "D:/Unity Training/survivorIOSource/DATN-GoGoSurvival" && python docs/tools/gen_pregame_infra.py`
Expected: prints 8 stat lines + `Done. 72 PreGame SOs created.`

- [ ] **Step 3: Verify structure (before Unity sees it)**

Run: `find Assets/_Main/Data/ModifierAndPreGame -name "*.asset" | wc -l` → expect `72`.
Run: `grep -L "guid:" docs/created-guids-pregame-infra.json` (sanity) and confirm `Mod_Player_PreGame_HPMax_AddNormal`'s `definition` guid equals the InGame `ModDef_Player_AddNormal_HPMax` guid (`f771bd9f5a7d43618989d1e2e9b56f0a`):
`grep -A4 "_id: Mod_Player_PreGame_HPMax_AddNormal" Assets/_Main/Data/ModifierAndPreGame/Player/HPMax/Mod_Player_PreGame_HPMax_AddNormal.asset`
Expected: `definition: {fileID: 11400000, guid: f771bd9f5a7d43618989d1e2e9b56f0a, type: 2}`

- [ ] **Step 4: Refresh Unity + verify clean compile**

Use MCP: `manage_editor stop` (if playing) → `refresh_unity(scope=all, mode=force, wait_for_ready=true)` → `read_console(types=error)`.
Expected: PreGame assets imported, **zero** compile/import errors.

- [ ] **Step 5: Commit**

```bash
git add docs/tools/gen_pregame_infra.py docs/created-guids-pregame-infra.json "Assets/_Main/Data/ModifierAndPreGame"
git commit -m "feat(stats): generate PreGame modifier pipeline (72 SOs, reuses ModDefs) [autonomous]"
```

---

### Task 2: Write the rewire script (items + InGame base bridge)

**Files:**
- Create: `docs/tools/rewire_items_to_pregame.py`

- [ ] **Step 1: Write the rewire script**

```python
"""
1) Swap every item's modifier reference guid  Mod_Player_InGame_*  ->  Mod_Player_PreGame_*.
2) Repoint each Number_Player_InGame_Final_<Stat>.baseNumber asset guid
   from Number_Player_InGame_Base_<Stat>  ->  Number_Player_PreGame_Final_<Stat>.
Pure deterministic YAML guid replacement. Run from project root.
"""
import os, sys, io, json, glob, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

INGAME = json.load(open('docs/created-guids-modifier-infra.json', encoding='utf-8'))
PRE    = json.load(open('docs/created-guids-pregame-infra.json', encoding='utf-8'))
STATS = ['HPMax','ATK','Speed','Cooldown','FireSpeed','Heal','Armor','RangeFind']
KINDS = ['AddNormal','AddRatio','Multiply']

# (1) build Mod guid swap map  InGame -> PreGame
mod_swap = {}
for s in STATS:
    for k in KINDS:
        ig = INGAME[f'Mod_Player_InGame_{s}_{k}']
        pg = PRE[f'Mod_Player_PreGame_{s}_{k}']
        mod_swap[ig] = pg

item_files = glob.glob('Assets/_Main/Data/Items/**/ItemConfig_*.asset', recursive=True)
changed_items = 0; total_subs = 0
for path in item_files:
    txt = open(path, encoding='utf-8').read()
    new = txt; subs = 0
    for ig, pg in mod_swap.items():
        if ig in new:
            cnt = new.count(ig)
            new = new.replace(ig, pg)
            subs += cnt
    if new != txt:
        open(path, 'w', encoding='utf-8', newline='\n').write(new)
        changed_items += 1; total_subs += subs
print(f'Items rewired: {changed_items}/{len(item_files)} files, {total_subs} guid subs')

# (2) bridge: InGame Final base  Base_<Stat> -> PreGame Final_<Stat>
bridged = 0
for s in STATS:
    final_path = f'Assets/_Main/Data/ModifierAndInGame/Player/{s}/Number_Player_InGame_Final_{s}.asset'
    base_ig = INGAME[f'Number_Player_InGame_Base_{s}']
    pre_final = PRE[f'Number_Player_PreGame_Final_{s}']
    txt = open(final_path, encoding='utf-8').read()
    # only the baseNumber block references base_ig; swap that single guid
    if base_ig in txt:
        new = txt.replace(base_ig, pre_final)
        open(final_path, 'w', encoding='utf-8', newline='\n').write(new)
        bridged += 1
    else:
        print(f'  WARN: {s} Final did not reference its InGame Base guid (already bridged?)')
print(f'InGame Final bases bridged to PreGame Final: {bridged}/8')
```

- [ ] **Step 2: Run the rewire script**

Run: `python docs/tools/rewire_items_to_pregame.py`
Expected: `Items rewired: N/130 files, M guid subs` (N>0) and `InGame Final bases bridged to PreGame Final: 8/8`.

- [ ] **Step 3: Verify an item now points to PreGame + Final base bridged**

Run: `grep -c "$(python -c "import json;print(json.load(open('docs/created-guids-pregame-infra.json'))['Mod_Player_PreGame_HPMax_AddNormal'])")" Assets/_Main/Data/Items/Better/ItemConfig_Army_Belt_Better.asset`
Expected: `> 0` (Army_Belt now references the PreGame HPMax AddNormal mod).
Run: `grep -A2 "baseNumber" Assets/_Main/Data/ModifierAndInGame/Player/HPMax/Number_Player_InGame_Final_HPMax.asset`
Expected: `asset:` guid equals `Number_Player_PreGame_Final_HPMax` guid (from `docs/created-guids-pregame-infra.json`), NOT the old `Number_Player_InGame_Base_HPMax`.

- [ ] **Step 4: Refresh Unity + verify no broken references**

MCP: `refresh_unity(scope=all, mode=force)` → `read_console(types=error)`.
Expected: zero import errors, no "missing reference" warnings on items.

- [ ] **Step 5: Commit**

```bash
git add docs/tools/rewire_items_to_pregame.py "Assets/_Main/Data/Items" "Assets/_Main/Data/ModifierAndInGame"
git commit -m "feat(stats): route equipment modifiers to PreGame + bridge InGame Final base to PreGame Final [autonomous]"
```

---

### Task 3: Register PreGame SOs into the scene Domain

**Files:**
- Modify: `Assets/_Main/Editor/IOPortDataGenerator.cs` (`RegisterModifierInfra`, around line 92–120)

- [ ] **Step 1: Read the current method**

Read `Assets/_Main/Editor/IOPortDataGenerator.cs` lines 90–125 to confirm the exact `FindAssets(..., new[] { "Assets/_Main/Data/ModifierAndInGame" })` call and how it adds to `DomainContentLoader`.

- [ ] **Step 2: Extend the search root to include PreGame**

Change the single search-folder array to include both layers. Edit the `FindAssets` line:

```csharp
// FROM:
var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Main/Data/ModifierAndInGame" });
// TO:
var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] {
    "Assets/_Main/Data/ModifierAndInGame",
    "Assets/_Main/Data/ModifierAndPreGame",
});
```

(Keep the rest of the method — the dedupe/add loop — unchanged; it already guards against duplicate registration.)

- [ ] **Step 3: Compile the editor change**

MCP: `manage_editor stop` (ensure not playing) → wait ~15s → `refresh_unity(scope=scripts, mode=force)` → `read_console(types=error)`.
Expected: clean compile.

- [ ] **Step 4: Run the registration menu**

MCP: `execute_menu_item("Tools/IOPort/Register Modifier Infra")` → `read_console`.
Expected: log `[IOPort] Registered <N> modifier-infra SOs` where N increased by ~72 vs before.

- [ ] **Step 5: Verify PreGame SOs are in DomainContentLoader**

Inspect the scene `DomainContentLoader` content list (grep `Assets/_Main/Scenes/GamePlay.unity` for one PreGame guid, e.g. `Number_Player_PreGame_Final_HPMax`):
`grep -c "<pregame_final_hpmax_guid>" "Assets/_Main/Scenes/GamePlay.unity"` → expect `>= 1`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Main/Editor/IOPortDataGenerator.cs "Assets/_Main/Scenes/GamePlay.unity"
git commit -m "feat(stats): register PreGame modifier infra into scene Domain [autonomous]"
```

---

### Task 4: Runtime verification (no balance change expected)

**Files:** none (verification only)

- [ ] **Step 1: Enter play mode and confirm clean boot**

MCP: `manage_editor play` → wait → `read_console(types=error)`.
Expected: **zero** new exceptions. Specifically NO NRE from `AssetModifier.AddFactor` (factors list null) and NO null `ContributionNumber` — proves PreGame mods initialized via Domain registration.

- [ ] **Step 2: Confirm equip drives the PreGame aggregation (best-effort)**

If `execute_code` is functional this session (test with `return 1+1;` first — it is frequently BROKEN on this machine with "filename too long"): equip an item via the existing flow and read the matching `Number_Player_PreGame_TotalAddNormal_<Stat>` value before/after. If `execute_code` is broken, fall back to structural proof already done in Tasks 1–3 (item → PreGame mod → PreGame TotalAddNormal → PreGame Final → InGame Final base) and note the limitation. **Do NOT claim runtime-verified if you could not actually observe the delta** (per verification-before-completion).

- [ ] **Step 3: Stop play**

MCP: `manage_editor stop`.

- [ ] **Step 4: Record findings**

Update project memory (`MEMORY.md`) with the new two-layer architecture, the guid maps, and whether runtime equip-delta was observed or only structurally verified.

---

### Task 5 (OPTIONAL — FLAGGED, ASK USER FIRST): Activation — make stats reach gameplay

> This is the only task that changes gameplay numbers. It flips `Stat_Player_<Stat>` from a flat constant to read the live `Number_Player_InGame_Final_<Stat>`. After this, equipment bonuses (via PreGame → InGame) actually affect the player. Memory notes this can shift balance and that an old `SV_EquipmentStatApplier` may double-apply stale PlayerPrefs equips. **Confirm with the user before doing this task.**

**Files:**
- Modify: 8× `Assets/_Main/Data/Stats/Player/Stat_Player_<Stat>.asset`

- [ ] **Step 1: Confirm current state**

Run: `grep -A3 "value:" Assets/_Main/Data/Stats/Player/Stat_Player_ATK.asset`
Expected: `mode: 0`, `constant: 10`, `asset: {fileID: 0}`.

- [ ] **Step 2: Repoint each Stat_Player to its InGame Final (Python)**

Create + run `docs/tools/activate_stats.py`:

```python
import json, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
INGAME = json.load(open('docs/created-guids-modifier-infra.json', encoding='utf-8'))
STATS = ['HPMax','ATK','Speed','Cooldown','FireSpeed','Heal','Armor','RangeFind']
import re
for s in STATS:
    p = f'Assets/_Main/Data/Stats/Player/Stat_Player_{s}.asset'
    try:
        txt = open(p, encoding='utf-8').read()
    except FileNotFoundError:
        print(f'  skip (no asset): {s}'); continue
    final_g = INGAME[f'Number_Player_InGame_Final_{s}']
    # replace the value: block (mode/constant/asset) with AssetNumber mode
    new = re.sub(r'  value:\n    mode: \d+\n    constant: [^\n]+\n    asset: \{[^}]*\}',
                 f'  value:\n    mode: 1\n    constant: 0\n    asset: {{fileID: 11400000, guid: {final_g}, type: 2}}',
                 txt, count=1)
    if new != txt:
        open(p, 'w', encoding='utf-8', newline='\n').write(new); print(f'  activated {s}')
    else:
        print(f'  WARN no value block matched: {s}')
```

Run: `python docs/tools/activate_stats.py` → expect `activated <stat>` for each existing Stat_Player asset.

- [ ] **Step 3: Refresh + play-test for balance**

MCP: `refresh_unity(scope=all, mode=force)` → `manage_editor play` → start a run → observe player HP/ATK reflect equipped items. `read_console(types=error)` clean.
Expected: stats now include equipment bonuses; no exceptions; no double-application (verify the old `SV_EquipmentStatApplier` path is not also active — if it is, disable it).

- [ ] **Step 4: Commit**

```bash
git add docs/tools/activate_stats.py "Assets/_Main/Data/Stats/Player"
git commit -m "feat(stats): activate Stat_Player -> InGame Final so equipment bonuses reach gameplay [autonomous]"
```

---

## Self-Review

- **Spec coverage:** PreGame pipeline (Task 1) ✓; equipment → PreGame (Task 2 part 1) ✓; PreGame Final → InGame base bridge (Task 2 part 2) ✓; Domain registration (Task 3) ✓; verification (Task 4) ✓; gameplay activation (Task 5, optional) ✓. PreGame StatsConfig for menu DISPLAY is intentionally **out of scope** — IO_Training uses it for the equipment screen; GoGo's shop/equipment UI currently reads stats via `ItemStatUtil`/modifier-factor display, not a PreGame StatsConfig. Add later only if the menu must show aggregated loadout stats.
- **Placeholder scan:** all code blocks complete; verification commands concrete.
- **Type/guid consistency:** PreGame mods reuse InGame ModDef guids (validated against `AssetModifier.AddFactor` definition check); swap map keyed by the exact `created-guids-*.json` names; both generators use identical script guids and YAML header shape as the proven `gen_modifier_infra.py`.
- **Risk:** Tasks 1–4 are gameplay-inert (Stat_Player still Constant) → safe to land independently. Task 5 is the sole behavior change and is gated on user confirmation.
