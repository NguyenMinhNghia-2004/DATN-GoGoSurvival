# Re-skin Shop/Equipment Popups Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the IO_Training MVVM shop/equipment popups look like the old hand-designed `SV_Shop` / `SV_Equipement` visuals, changing only the Editor prefab generator (no View/VM code changes).

**Architecture:** Modify `Assets/_Main/Editor/IOPortPrefabGenerator.cs` only. Add a `CloneSubtree` helper that deep-copies a named subtree out of `SV_Shop.prefab` / `SV_Equipement.prefab` (via `PrefabUtility.LoadPrefabContents`), strips foreign/missing MonoBehaviours, then re-finds children by name to wire the existing IO_Training View component refs. Rebuild prefabs by re-running `IOPortPrefabGenerator.Build()`. The runtime pipeline (IOPortBootstrap canvas + PopupService) and nav wiring are untouched.

**Tech Stack:** Unity 6 Editor scripting (`PrefabUtility`, `GameObjectUtility`, `SerializedObject`, `UnityEventTools`), uGUI/TMP, Luzart MVVM (`ViewT<T>`, `ViewChilding`, `BaseSelect`/`SelectSwitchGameObject`).

**Verification model (no pytest):** Per task — edit generator → `execute_code` runs `IOPortPrefabGenerator.Build();` → `read_console` errors clean → (visual tasks) `execute_code` play-mode assertion → commit. Source SV prefabs are read-only (visual freeze). Rollback to last green on red; halt at 3 reds.

**Source paths:**
- Generator: `Assets/_Main/Editor/IOPortPrefabGenerator.cs`
- Old shop visual: `Assets/_Main/Perfabes/UI/SV_Shop.prefab`
- Old equip visual: `Assets/_Main/Perfabes/UI/SV_Equipement.prefab`
- RaritySpriteResolver asset: `Assets/_Main/Data/IOShop/Resources/RaritySpriteResolver.asset` (confirm exact path in Task 2)
- Rarity frame sheet guid: `1263f101bd3658846bfa6ca84e97543c`

---

## Task 1: Clone helper + foreign-behaviour stripper

**Files:**
- Modify: `Assets/_Main/Editor/IOPortPrefabGenerator.cs` (add helpers near the other static helpers, ~line 398)

- [ ] **Step 1: Add `using` for editor utils**

At top of file ensure these usings exist (add any missing): `using UnityEditor;`, `using UnityEditor.Events;`, `using UnityEngine;`, `using System.Collections.Generic;`. (Most already present.)

- [ ] **Step 2: Add helper methods**

Insert into the `// ---------- helpers ----------` region:

```csharp
// Deep-copy a named subtree out of a source prefab asset. Returned GO is detached
// (parent null), foreign MonoBehaviours + missing scripts stripped. Caller reparents + saves.
static GameObject CloneSubtree(string sourcePrefabPath, string childPath)
{
    var contents = PrefabUtility.LoadPrefabContents(sourcePrefabPath);
    try
    {
        var node = string.IsNullOrEmpty(childPath)
            ? contents.transform
            : contents.transform.Find(childPath);
        if (node == null)
        {
            Debug.LogError($"[IOPortPF] subtree '{childPath}' not found in {sourcePrefabPath}");
            return null;
        }
        var clone = UnityEngine.Object.Instantiate(node.gameObject);
        clone.name = node.gameObject.name;
        StripForeignBehaviours(clone);
        return clone;
    }
    finally
    {
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

// Remove missing-script components + any MonoBehaviour whose type name starts with "SV_"
// (old NinjaUI binders) so the clone carries only pure visual components (Image/Text/etc).
static void StripForeignBehaviours(GameObject root)
{
    foreach (var t in root.GetComponentsInChildren<Transform>(true))
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        foreach (var c in t.GetComponents<MonoBehaviour>())
        {
            if (c == null) continue;
            var n = c.GetType().Name;
            if (n.StartsWith("SV_")) UnityEngine.Object.DestroyImmediate(c);
        }
    }
}

// Find a descendant by exact name (depth-first). Returns null if absent.
static Transform FindDeep(Transform root, string name)
{
    if (root.name == name) return root;
    for (int i = 0; i < root.childCount; i++)
    {
        var r = FindDeep(root.GetChild(i), name);
        if (r != null) return r;
    }
    return null;
}
```

- [ ] **Step 3: Verify compile**

`execute_code`: `UnityEditor.EditorUtility.RequestScriptReload(); return "reload";` then after reload `read_console(types=error)`.
Expected: no compile errors referencing IOPortPrefabGenerator.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Main/Editor/IOPortPrefabGenerator.cs
git commit -m "migrate(shop-equip): reskin Task1 clone helper + foreign-behaviour stripper [autonomous]"
```

---

## Task 2: RaritySpriteResolver — assign rarity frame sprites

**Files:**
- Modify: `Assets/_Main/Editor/IOPortPrefabGenerator.cs` (new method `AssignRarityFrames()` + call it in `Build()`)

- [ ] **Step 1: Confirm asset path + field shape**

Read `Assets/_Main/Scripts/_IOPort/PopupItem/RaritySpriteResolver.cs` to confirm the serialized field that holds per-rarity sprites (e.g. a `Sprite[]` keyed by `ERarity`, or a list of {rarity, sprite}). Glob `Assets/_Main/Data/IOShop/**/RaritySpriteResolver.asset` for the exact asset path.

- [ ] **Step 2: Resolve the frame sub-sprites**

`execute_code` to list sub-sprites of the sheet and pick one frame per rarity tier:

```csharp
var path = UnityEditor.AssetDatabase.GUIDToAssetPath("1263f101bd3658846bfa6ca84e97543c");
var subs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
var names = new System.Text.StringBuilder();
foreach (var s in subs) if (s is UnityEngine.Sprite) names.Append(s.name).Append(',');
return names.ToString();
```
Record which sprite names correspond to rarity-colored frames.

- [ ] **Step 3: Add `AssignRarityFrames()`**

Write a method that loads the RaritySpriteResolver asset, loads the chosen frame sprites by name from the sheet, sets the resolver's sprite field (use the `Set(...)` reflection helper, matching the field shape found in Step 1), `EditorUtility.SetDirty`. Call `AssignRarityFrames();` at the end of `Build()` before `AssetDatabase.SaveAssets()`.

- [ ] **Step 4: Run + verify**

`execute_code`: `IOPortPrefabGenerator.Build(); return "built";` → `read_console(types=error)` clean → `execute_code` assert resolver returns non-null sprite for each rarity:
```csharp
var p = UnityEditor.AssetDatabase.GUIDToAssetPath("...resolver guid...");
var r = UnityEditor.AssetDatabase.LoadAssetAtPath<Luzart.RaritySpriteResolver>(p);
return r.GetSpriteByRarity(Luzart.ERarity.Common) != null ? "ok" : "null";
```
Expected: "ok".

- [ ] **Step 5: Commit**

```bash
git add Assets/_Main/Editor/IOPortPrefabGenerator.cs Assets/_Main/Data/IOShop
git commit -m "migrate(shop-equip): reskin Task2 rarity frame sprites on resolver [autonomous]"
```

---

## Task 3: ShopCard ← clone old Daily Shop card

**Files:**
- Modify: `Assets/_Main/Editor/IOPortPrefabGenerator.cs` (`BuildItemShopUnlockView`)

- [ ] **Step 1: Read exact card subtree**

Grep/read `SV_Shop.prefab` around the `Daily Shop` content to capture the EXACT child path of one card (e.g. `Container/Items/Viewport/Content/Daily Shop/Weapon Design`) and its child node names (icon node, name text node, price text node, "Done" overlay node). Record them.

- [ ] **Step 2: Rewrite `BuildItemShopUnlockView` visual half**

Keep the View wiring; swap construction to clone. Pattern:

```csharp
static GameObject BuildItemShopUnlockView(GameObject objectViewPf)
{
    var root = CloneSubtree("Assets/_Main/Perfabes/UI/SV_Shop.prefab",
        "<CARD_PATH_FROM_STEP1>");
    if (root == null) { /* fallback to old placeholder build */ }
    var btn = root.GetComponent<Button>() ?? root.AddComponent<Button>();
    var v = root.AddComponent<ItemShopUnlockView>();

    // ObjectView mounts the icon/bg leaf into the card's icon node
    var ov = (GameObject)PrefabUtility.InstantiatePrefab(objectViewPf);
    var iconNode = FindDeep(root.transform, "<ICON_NODE>");
    ov.transform.SetParent(iconNode != null ? iconNode : root.transform, false);

    // cost rows spawn under a costs node (reuse/clear the old price node's parent)
    var costParent = FindDeep(root.transform, "<PRICE_NODE>");
    Transform costT = costParent != null ? costParent.parent : root.transform;

    Set(v, "objectView", ov.GetComponent<ObjectView>());
    Set(v, "parentSpawnCost", costT);
    Set(v, "children", new ViewChilding[0]);
    // optional: map old "Done" overlay to bsUnlocked via a SelectSwitchGameObject
    AddClick(btn, v.OnClickUnlock);
    return SavePrefab(root, PF + "/ShopCard.prefab");
}
```
Replace `<...>` with the real names from Step 1.

- [ ] **Step 3: Build + verify compile/console**

`execute_code`: `IOPortPrefabGenerator.Build(); return "built";` → `read_console(types=error)` clean.

- [ ] **Step 4: Play-verify shop look + buy**

`execute_code` (enter play if needed): open shop via `SceneRootManager.Instance.Domain.GetFirst<IOPortBootstrap>().OpenShop();` then assert a `PopupShop` exists and card count > 0 and a card's GO name == old card name. Confirm gold spend on buy as in MEMORY Slice 5.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Main/Editor/IOPortPrefabGenerator.cs Assets/_Main/Data/IOShop
git commit -m "migrate(shop-equip): reskin Task3 ShopCard from old Daily Shop card [autonomous]"
```

---

## Task 4: PopupShop body ← clone old shop scroll frame

**Files:**
- Modify: `Assets/_Main/Editor/IOPortPrefabGenerator.cs` (`BuildPopupShop`)

- [ ] **Step 1: Read exact shop container subtree**

From `SV_Shop.prefab`, record the path of the scroll frame (`Container`) and the node that should host the card grid (inside `Daily Shop`). Note whether a GridLayoutGroup already exists.

- [ ] **Step 2: Rewrite `BuildPopupShop` body**

Clone `Container` into the popup root (keep `BuildPopupShell` for the root + Title + generated CloseButton X, OR clone Container under a fresh popup root). Set the card spawner `parent` to the grid node inside Daily Shop (add a `GridLayoutGroup` if absent: cellSize ~325x443 per MEMORY, constraint FixedColumnCount). Keep all existing wiring: `PopupShop`/`PopupShopView`, `ShopPopupUnlockedView.uIItemSpawnerGeneric`, `UIItemSpawnerGeneric.viewPrefab=ShopCard`, ViewChilding `MakeChilding("InventoryItemData", unlockedView)`, `mainView`, `closeButton`.

- [ ] **Step 3: Build + verify**

`execute_code` Build → `read_console(types=error)` clean.

- [ ] **Step 4: Play-verify**

Open shop, assert popup shows the old scroll/Daily Shop frame (root child names include `Container`/`Daily Shop`) and cards populate + scroll.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Main/Editor/IOPortPrefabGenerator.cs Assets/_Main/Data/IOShop
git commit -m "migrate(shop-equip): reskin Task4 PopupShop body from old scroll frame [autonomous]"
```

---

## Task 5: SlotItem ← clone old equipment slot

**Files:**
- Modify: `Assets/_Main/Editor/IOPortPrefabGenerator.cs` (`BuildSlotItemEquipmentView`, and `BuildItemViewWithLevel` if the slot's itemView leaf is built inline)

- [ ] **Step 1: Read exact slot subtree**

From `SV_Equipement.prefab`, record the path of one slot (e.g. `Main Content/HalfPlayerShow/Left/Firs`) and its children: icon node (`Icon`), empty placeholder (`LogoNot`), level text (`Text (Legacy)`), reward badge. Note the slot root Image (rarity frame).

- [ ] **Step 2: Rewrite `BuildSlotItemEquipmentView`**

Clone the slot; build the itemView leaf onto the cloned children + add bsState. Pattern:

```csharp
static GameObject BuildSlotItemEquipmentView(GameObject lvlViewPf)
{
    var root = CloneSubtree("Assets/_Main/Perfabes/UI/SV_Equipement.prefab",
        "<SLOT_PATH>");
    var btn = root.GetComponent<Button>() ?? root.AddComponent<Button>();
    var v = root.AddComponent<SlotItemEquipmentView>();

    var icon = FindDeep(root.transform, "Icon");
    var logoNot = FindDeep(root.transform, "LogoNot");
    var lvlText = FindDeep(root.transform, "<LEVEL_TEXT_NODE>");
    var frameImg = root.GetComponent<Image>();

    // itemView leaf lives on the slot root (imBg = frame, imIcon = Icon, txtLevel = level)
    var iv = root.AddComponent<ItemViewWithLevelInventory>();
    Set(iv, "imIcon", icon != null ? icon.GetComponent<Image>() : null);
    Set(iv, "imBg", frameImg);
    Set(iv, "txtLevel", lvlText != null ? lvlText.GetComponent<TMP_Text>() : null); // see Step 2a

    // bsState: 0=equipped(show Icon), 1=empty(show LogoNot), 2=locked(show lock)
    var bsState = root.AddComponent<Luzart.NewBase.SelectSwitchGameObject>();
    bsState.obSelects = new[]
    {
        Grp(icon?.gameObject), Grp(logoNot?.gameObject), Grp(logoNot?.gameObject),
    };

    Set(v, "itemView", iv);
    Set(v, "bsState", bsState);
    Set(v, "children", new ViewChilding[0]);
    AddClick(btn, v.OnClickItemView);
    return SavePrefab(root, PF + "/SlotItem.prefab");
}

static Luzart.NewBase.SelectSwitchGameObject.GroupGameObject Grp(GameObject go)
{
    return new Luzart.NewBase.SelectSwitchGameObject.GroupGameObject
    { obGroups = go != null ? new[] { go } : new GameObject[0] };
}
```

- [ ] **Step 2a: Level text type**

`ItemViewWithLevelInventory.txtLevel` is `TMP_Text` but the old node is legacy `Text`. If the cloned level node is legacy `Text`, create a sibling TMP child for the level (small TMP at the same anchored position) and use that as `txtLevel`; leave the legacy Text disabled. Show the code that adds the TMP child.

- [ ] **Step 3: Build + verify compile/console**

`execute_code` Build → `read_console(types=error)` clean.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Main/Editor/IOPortPrefabGenerator.cs Assets/_Main/Data/IOShop
git commit -m "migrate(shop-equip): reskin Task5 SlotItem from old equipment slot [autonomous]"
```

---

## Task 6: ItemViewInventory cell ← clone old grid cell

**Files:**
- Modify: `Assets/_Main/Editor/IOPortPrefabGenerator.cs` (`BuildItemViewInventory`)

- [ ] **Step 1: Read exact cell subtree**

From `SV_Equipement.prefab`, record the path of the grid cell (`…/DownFill/Bg/WeaponIcons/Cnte` — note trailing spaces in name) and its children (Icon/LogoNot/Text/Reward).

- [ ] **Step 2: Rewrite `BuildItemViewInventory`**

Clone the cell; add `Button` + `ItemViewInventory`; build the `ItemViewWithLevelInventory` leaf onto the cell's Icon/frame/level (same pattern as Task 5 Step 2/2a, without bsState — cell has no equipped/empty/locked states, it always shows an owned item). Wire `_itemView` + `AddClick(btn, v.OnClickItemView)`. Save to `PF + "/ItemViewInventory.prefab"`.

- [ ] **Step 3: Build + verify**

`execute_code` Build → `read_console(types=error)` clean.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Main/Editor/IOPortPrefabGenerator.cs Assets/_Main/Data/IOShop
git commit -m "migrate(shop-equip): reskin Task6 inventory cell from old grid cell [autonomous]"
```

---

## Task 7: PopupItemInventory body ← clone HalfPlayerShow (dual column + grid)

**Files:**
- Modify: `Assets/_Main/Editor/IOPortPrefabGenerator.cs` (`BuildPopupItemInventory`)

- [ ] **Step 1: Read exact equip body subtree**

From `SV_Equipement.prefab`, record paths: `Main Content/HalfPlayerShow`, `…/Left`, `…/Right`, and the owned-grid container `…/DownFill/Bg/WeaponIcons`. Confirm Left/Right each hold 3 slot anchor nodes.

- [ ] **Step 2: Rewrite `BuildPopupItemInventory`**

Clone `HalfPlayerShow` into the popup root (keep generated CloseButton X). Remove the 6 hand-placed slot template children from Left/Right (the spawner will fill them) OR keep them as visual anchors and spawn into Left/Right. Create **two** `UIItemSpawnerGeneric`:
- left: `parent` = Left node, ViewChilding path `SlotsLeftObj`, `viewPrefab` = SlotItem
- right: `parent` = Right node, ViewChilding path `SlotsRightObj`, `viewPrefab` = SlotItem

Create `ScrollViewItemInventoryData`: `parentSpawn` = WeaponIcons (or its cell parent), `itemPrefabs` = ItemViewInventory cell, ViewChilding path `Inventory`. Wire `PopupItemInventoryView.children` = [SlotsLeftObj→leftSpawner, SlotsRightObj→rightSpawner, Inventory→scrollData]. Keep `mainView`, `closeButton`.

- [ ] **Step 3: Build + verify**

`execute_code` Build → `read_console(types=error)` clean.

- [ ] **Step 4: Play-verify equipment look**

Open equipment via `IOPortBootstrap.OpenEquipment()`; assert popup root contains `HalfPlayerShow`; 6 slot views spawn across Left+Right; owned cells populate; tap a cell → `PopupItemEquip` opens.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Main/Editor/IOPortPrefabGenerator.cs Assets/_Main/Data/IOShop
git commit -m "migrate(shop-equip): reskin Task7 PopupItemInventory dual-column + grid [autonomous]"
```

---

## Task 8: PopupItemEquip (detail) — style to old detail popup + fix bsEquipButton

**Files:**
- Modify: `Assets/_Main/Editor/IOPortPrefabGenerator.cs` (`BuildPopupItemEquip`)

- [ ] **Step 1: Add bsEquipButton + tidy layout**

Keep the existing programmatic detail popup (no old prefab to clone). Add a `SelectSwitchGameObject bsEquipButton` whose obSelects = [ {Equip button GO}, {Unequip button GO} ] so `Select(0/1)` shows exactly one (fixes the overlap from follow-up #5). Wire `Set(view, "bsEquipButton", bsEquipButton)`. Reposition Equip and Unequip to the same anchored position (they now toggle, not overlap). Optionally apply the panel/frame sprite (`4fd7b8389716bda4d850af75dad00216`) to the popup background to match the old look.

- [ ] **Step 2: Build + verify**

`execute_code` Build → `read_console(types=error)` clean.

- [ ] **Step 3: Play-verify equip toggle**

Open a detail popup for an owned item; assert only one of Equip/Unequip is visible; equip → Unequip shows; unequip → Equip shows; Upgrade spends + level increments (per MEMORY Slice 5).

- [ ] **Step 4: Commit**

```bash
git add Assets/_Main/Editor/IOPortPrefabGenerator.cs Assets/_Main/Data/IOShop
git commit -m "migrate(shop-equip): reskin Task8 detail popup style + bsEquipButton toggle [autonomous]"
```

---

## Task 9: Full play-verify + memory update

- [ ] **Step 1: End-to-end nav verification**

Via real nav buttons (not direct calls): Main menu → Shop (old card look, buy, toggle-close) → Equipment (silhouette + dual-column slots + grid, cell→detail, equip/unequip/upgrade). Confirm no new console exceptions across the flow. Capture a screenshot via `manage_camera screenshot` for the record.

- [ ] **Step 2: Update MEMORY.md**

Append a dated entry under the faithful-port section: re-skin done, generator now clones SV subtrees, RaritySpriteResolver frames assigned, bsEquipButton fixed, old SV screens still dead. Note remaining follow-ups still open (icons null by data; equip→gameplay activation step; SaveService).

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers Assets/_Main
git commit -m "migrate(shop-equip): reskin Task9 full play-verify + memory [autonomous]"
```

---

## Self-review notes

- **Spec coverage:** Tasks 1–8 cover every row of the spec's per-prefab table; Task 2 covers RaritySpriteResolver; Task 8 covers the detail-popup scope decision + bsEquipButton fix; old SV screens intentionally untouched (spec non-goal). Task 9 = verification + memory.
- **Type consistency:** `CloneSubtree`/`StripForeignBehaviours`/`FindDeep`/`Grp` defined in Task 1/5 and reused by name in Tasks 3–8. `Set(...)` is the existing reflection helper. `ItemViewWithLevelInventory` fields = `imIcon`/`imBg`/`txtLevel`; `SlotItemEquipmentView` = `itemView`/`bsState`/`bsWeapon`; `ItemShopUnlockView` = `objectView`/`parentSpawnCost`/`bsUnlocked`/`bsItem`; `PopupItemInventoryView` paths = `SlotsLeftObj`/`SlotsRightObj`/`Inventory`; `PopupShopView` child path = `InventoryItemData`. All verified against the `.cs` reads.
- **Known risk:** exact child node names (Icon/LogoNot/level text/price) are confirmed as Step 1 of each visual task before code is finalized — the plan does not hardcode unverified names.
