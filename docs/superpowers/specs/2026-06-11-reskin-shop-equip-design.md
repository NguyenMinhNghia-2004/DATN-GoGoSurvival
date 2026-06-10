# Re-skin IO_Training Shop/Equipment popups with old SV visuals

Date: 2026-06-11
Status: Approved (user: "Ok làm hết đi")

## Goal

Keep the faithful IO_Training MVVM **logic** (Domain DI, ItemConfig/AssetUnlockable,
modifier pipeline, PopupService, View/VM classes) — **unchanged** — but make the
popups **look like the old hand-designed visuals** in `SV_Shop.prefab` /
`SV_Equipement.prefab` instead of the current placeholder boxes.

User intent (verbatim across sessions): "làm y hệt IO_Training, code cũng thế… visual
hiện tại" → "về mặt hình ảnh thì như cũ còn về logic mới là logic mới" → chose
**Re-skin popup prefab** integration strategy.

## Non-goals / constraints

- **Zero changes** to any View/VM/framework `.cs` under `Assets/_Main/Scripts/_IOPort/`.
  The only `.cs` that changes is the Editor generator `Assets/_Main/Editor/IOPortPrefabGenerator.cs`.
- Do **not** delete the old SV_Shop / SV_Equipement NinjaUI screens (risky type-refs in
  SV_MainMenuUI / ClassicModeController). They stay dead/unreachable, as today.
- Keep the working port pipeline (IOPortBootstrap canvas + PopupService) and nav wiring
  (`SV_MainMenuUI.OnShop/OnEquipment` → `IOPortBootstrap.OpenShop/OpenEquipment`) untouched.
- Visual freeze rule still applies to the *source* prefabs: we **read/clone** SV_Shop /
  SV_Equipement subtrees, we do not edit them.

## Key data facts

- `ItemConfig._sprite` is null for all 130 items → there is **no per-item icon art**.
  "Visual như cũ" therefore means reproducing the **card/slot frame design + rarity
  frames + dual-column layout + player silhouette**, not per-item icons. Item icons
  stay empty (as the old SV cards also were).
- Old slot/cell rarity frames use sprite sheet guid `1263f101bd3658846bfa6ca84e97543c`
  (sub-sprites e.g. 21300124/21300130/21300134). These feed `RaritySpriteResolver`.
- Old card backgrounds / player silhouette use guid `4fd7b8389716bda4d850af75dad00216`.

## MVVM wiring contract (must be preserved exactly)

- `Popup<T>` root holds `mainView` + `closeButton`; the View's `ViewChilding[] children`
  binds child Views by **member-path reflection** on the VM (`ViewChilding.path` → public
  field/prop/method on the View, value is `Setup()`-ed into the child View).
- Leaf Views only hold component refs — they are visual-agnostic:
  - `ItemConfigView`: `_imIcon` (Image), `_imBg` (Image, rarity frame), `_txt` (TMP).
  - `ItemViewWithLevelInventory`: `imIcon` (Image), `imBg` (Image, rarity frame), `txtLevel` (TMP).
  - `ObjectView`: `container` (Transform), `itemConfigView`/`itemDefinitionView`/`resourcePoolView` prefab refs.
  - `ResourceCostSingleLine`: `txt` (TMP).
- Spawners:
  - `UIItemSpawnerGeneric`: `parent` (Transform), `viewPrefab` (View). Spawns one View per `IEnumerable<object>` item.
  - `ScrollViewItemInventoryData`: `parentSpawn` (Transform), `itemPrefabs` (ItemViewInventory). Pools cells.
- `ItemShopUnlockView`: `objectView`, `parentSpawnCost` (Transform), `bsUnlocked` (BaseSelect, optional), `bsItem` (BaseSelect, optional). Button → `OnClickUnlock`.
- `SlotItemEquipmentView`: `itemView` (ItemViewWithLevelInventory), `bsState` (BaseSelect: 0=equipped,1=empty,2=locked), `bsWeapon` (BaseSelect, optional). Button → `OnClickItemView`.
- `ItemViewInventory`: `_itemView` (ItemViewWithLevelInventory), `_bsTypeItem` (BaseSelect, optional). Button → `OnClickItemView`.
- `PopupItemInventoryView` exposes `SlotsLeftObj` / `SlotsRightObj` / `AllSlotsObj` / `Inventory` → use **two** slot spawners (left + right) to match dual-column.
- `PopupShopView` → child `ShopPopupUnlockedView` (path `InventoryItemData`) → `uIItemSpawnerGeneric`.
- `PopupItemEquipView`: `txtName`, `txtDescription`, `objectView`, `btnEquip`/`btnUnEquip`/`btnUpgrade`, `bsEquipButton` (BaseSelect toggle), `parentSpawn` (Transform).

`BaseSelect` concrete = `SelectSwitchGameObject` (`GroupGameObject[] obSelects`, each
`GameObject[] obGroups`; `Select(int)` activates that group, deactivates others).

## Approach

Modify only `IOPortPrefabGenerator.cs`. Replace each `BuildXxx` method's **visual
construction** (the `NewGO`+`AddImage` placeholder boxes) with a **clone of the matching
old subtree + real sprite assignment**, while keeping every `Set(view, "ref", …)`
wiring line and the popup/ViewChilding registration logic.

### Clone helper (new, Editor-side)

```
CloneSubtree(string sourcePrefabPath, string childPath) -> GameObject
  contents = PrefabUtility.LoadPrefabContents(sourcePrefabPath)
  node     = contents.transform.Find(childPath)
  clone    = Object.Instantiate(node.gameObject)   // deep copy, detached
  StripForeignBehaviours(clone)                     // remove SV_* + missing scripts
  PrefabUtility.UnloadPrefabContents(contents)
  return clone
```
`StripForeignBehaviours`: `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` on
every transform + `DestroyImmediate` any component whose type name starts with `SV_`.
Child refs are then re-found by name (`clone.transform.Find("Icon")`, etc.).

### Per-prefab mapping (slices)

1. **ShopCard** ← clone `Container/Items/Viewport/Content/Daily Shop/<a card>` (e.g. "Weapon Design").
   - Re-find: `Icon`→ItemConfigView `_imIcon`; card root Image → `_imBg`; `Name`/price Text → `_txt`.
   - Add `ObjectView` (container = card body) + `ItemShopUnlockView` (objectView, parentSpawnCost = a costs node under the card, Button=root → OnClickUnlock).
   - Map old `Done` overlay → optional bsUnlocked group.
2. **SlotItem** ← clone `HalfPlayerShow/Left/Firs`.
   - `ItemViewWithLevelInventory`: imIcon=`Icon`, imBg=root frame Image, txtLevel=`Text (Legacy)` (replace Legacy Text with TMP, or keep TMP child — itemView wants TMP_Text).
   - `SelectSwitchGameObject bsState`: obSelects = [ {Icon}, {LogoNot}, {lock overlay (clone or new)} ].
   - `SlotItemEquipmentView`: itemView, bsState, Button=root → OnClickItemView.
3. **ItemViewInventory** (cell) ← clone `…/DownFill/Bg/WeaponIcons/Cnte`.
   - Same itemView wiring as slot; `ItemViewInventory` + Button.
4. **PopupShop body** ← clone `Container` (scroll frame + Daily Shop header).
   - Spawner parent = a grid node inside Daily Shop (add/keep GridLayoutGroup so cards flow + ScrollRect scrolls). Keep `PopupShop`/`PopupShopView`/`ShopPopupUnlockedView`/`UIItemSpawnerGeneric` wiring + ViewChilding(`InventoryItemData`→unlockedView). Close button = a generated X (old screen had none).
5. **PopupItemInventory body** ← clone `Main Content/HalfPlayerShow` (silhouette + DownFill + Left/Right columns + WeaponIcons grid).
   - Two `UIItemSpawnerGeneric`: left (parent=`Left`, ViewChilding path `SlotsLeftObj`), right (parent=`Right`, path `SlotsRightObj`). Cell grid `ScrollViewItemInventoryData` parent=`WeaponIcons` (or its Cnte parent), path `Inventory`. Slot prefab = #2, cell prefab = #3. Close = generated X.
6. **PopupItemEquip (detail)** — no old prefab; **style** to evoke old detail popup
   (icon+frame via ObjectView, name, desc + Next preview, Equip/Unequip via `bsEquipButton`
   SelectSwitchGameObject toggling the two buttons, Upgrade + cost rows, balance, X).
   Fixes follow-up #5 (bsEquipButton null overlap).
7. **RaritySpriteResolver.asset** — assign per-rarity frame sprites from sheet
   `1263f101…` so frames render by rarity (Common/Rare/Epic/Legendary…).

### Unchanged leaf prefabs

`ItemConfigView`, `ObjectView`, `ItemViewWithLevel`, `ResourceCostRow`,
`IOPortPopupCanvas` keep their current build (or get the cloned frame for the
icon/bg nodes where they are the visual leaf — ItemConfigView/ItemViewWithLevel
get rarity-frame `imBg` + icon `imIcon` from cloned card/slot art).

## Verification (per project contract)

Per slice: rebuild via `IOPortPrefabGenerator.Build()` (invoke through `execute_code`
since MCP menu cache is flaky) → `read_console(errors)` clean → commit. After all
slices: play-mode verify via real nav buttons (Shop opens with old card look + buy
spends gold + card removed; Equipment opens with silhouette + dual-column slots + grid;
cell tap → detail popup; equip/unequip/upgrade flow + modifier stat text). Compare
against the previously-verified behavior in MEMORY.md (Slice 5).

Rollback to last green on red; halt at 3 reds.

## Commit plan

One slice = one commit, message `migrate(shop-equip): reskin <part> [autonomous]`.
Order: clone-helper+RaritySpriteResolver → ShopCard+PopupShop → SlotItem+cell+PopupItemInventory
→ PopupItemEquip detail → final play-verify. Game playable (compile clean) each commit.
