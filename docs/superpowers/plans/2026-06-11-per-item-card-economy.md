# Per-Item Card Economy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the shared rarity-shard economy with per-item cards — each equipment item has its own card pool that players collect to unlock and upgrade that item; the shop sells specific items' cards for Gold and wins drop random item cards weighted by rarity.

**Architecture:** Reuse the existing `ResourcePool` cost/save rails (Approach 1). Generate one `ResourcePool`+`ResourceDefinition` per `ItemConfig` (icon = item sprite), store a runtime `CardPool` reference on each item, and re-point every unlock + level-up cost from the 3 rarity-shard pools to the item's own card pool — all via an idempotent editor converter. Shop offers and win rewards reference items and resolve `item.CardPool`. Shop prefab LAYOUT is unchanged; only an `Icon` and a `GoldIcon` child are added.

**Tech Stack:** Unity 6000.3.14f1, C# (Assembly-CSharp), MCP for Unity (editor automation), existing `_IOPort` editor generators.

**Project root:** `D:/Unity Training/survivorIOSource/DATN-GoGoSurvival`
**Unity exe:** `C:/Program Files/Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe`

**Conventions / gotchas (from prior sessions):**
- `execute_code` MCP is broken here (mono "filename too long") — do NOT rely on it. Run logic via compiled editor **menu items** (`execute_menu_item`) or Unity headless `-executeMethod`.
- No NUnit test framework exists. Pure-logic tests live in a new EditMode asmdef (Task 1). Asset/prefab tasks are verified by editor-tool asserts + compile-clean + play-mode manual checks.
- Image sprite SerializedProperty is `m_Sprite` (not `sprite`); TMP color is `m_fontColor`.
- After editing C# from the filesystem, trigger a compile: MCP `refresh_unity(compile=request, wait_for_ready=true)` or Unity headless `-executeMethod UnityEditor.SyncVS.SyncSolution`.
- Commit into THIS project's repo only. It currently has unrelated dirty files (`ItemViewInventory.prefab`, `PopupShop.prefab`) — stage files explicitly, never `git add -A`.

**Compile check (headless):**
```
"C:/Program Files/Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe" -batchmode -nographics -quit \
  -projectPath "D:/Unity Training/survivorIOSource/DATN-GoGoSurvival" \
  -logFile Temp/compile.log -executeMethod UnityEditor.SyncVS.SyncSolution
```
(Or, preferred in-session: MCP `refresh_unity` then `read_console onlyErrors=true`.)

---

## File Structure

**New runtime scripts** (`Assets/_Main/Scripts/_IOPort/Cards/`):
- `ItemCardPicker.cs` — pure C# weighted-by-rarity item picker (testable, no UnityEngine.Random).

**New editor scripts** (`Assets/_Main/Editor/`):
- `PerItemCardConverter.cs` — generates per-item card pools/defs, sets `ItemConfig._cardPool`, rewires unlock + level-up costs, registers pools into `DomainContentLoader`, plus a verify+cleanup menu.

**New tests** (`Assets/_Main/Tests/EditMode/`):
- `IOPort.Tests.EditMode.asmdef` + `ItemCardPickerTests.cs`, `ShopBuyShardLogicTests.cs`.

**Modified runtime scripts:**
- `Assets/_Main/Scripts/_LuzartGame/Items/ItemConfig.cs` — add `_cardPool` + `CardPool`.
- `Assets/_Main/Scripts/_IOPort/Shop/ShopShardOffer.cs` — reference `ItemConfig` instead of shardPool+rarity.
- `Assets/_Main/Scripts/_IOPort/Shop/ShopBuyShardView.cs` — item icon + gold icon + buy into item card pool.
- `Assets/_Main/Scripts/_IOPort/Shop/Data_WinReward.cs` — weighted random item card grant.
- `Assets/_Main/Scripts/_IOPort/Shop/Data_Shop.cs` / `PopupShopView.cs` — unchanged structurally (offers list stays `List<ShopShardOffer>`).
- `Assets/_Main/Editor/IOPortPrefabGenerator.cs` — add `Icon` + `GoldIcon` to `ShopShardCard` prefab.

**Generated assets:** `Assets/_Main/Data/IOShop/Resources/Cards/ResourcePool_Card_<Item>.asset` + `ResourceDefinition_Card_<Item>.asset` (×130).

---

## Task 1: Test harness + pure-logic weighted picker

**Files:**
- Create: `Assets/_Main/Tests/EditMode/IOPort.Tests.EditMode.asmdef`
- Create: `Assets/_Main/Scripts/_IOPort/Cards/ItemCardPicker.cs`
- Create: `Assets/_Main/Scripts/_IOPort/Cards/IOPortCards.asmdef` (so tests can reference it; only if `_IOPort` is not already its own asmdef — otherwise reference `Assembly-CSharp`)
- Test: `Assets/_Main/Tests/EditMode/ItemCardPickerTests.cs`

- [ ] **Step 1: Determine assembly layout**

Run: search for an existing asmdef covering `_IOPort`.
```
ls Assets/_Main/Scripts/_IOPort/*.asmdef 2>/dev/null || echo "no asmdef -> code is in Assembly-CSharp"
```
If `_IOPort` is in `Assembly-CSharp` (no asmdef), the EditMode test asmdef must reference `Assembly-CSharp`. Do NOT add a new runtime asmdef (would break the monolithic Assembly-CSharp). Skip creating `IOPortCards.asmdef`.

- [ ] **Step 2: Create the EditMode test asmdef**

Create `Assets/_Main/Tests/EditMode/IOPort.Tests.EditMode.asmdef`:
```json
{
  "name": "IOPort.Tests.EditMode",
  "references": ["Assembly-CSharp"],
  "includePlatforms": ["Editor"],
  "precompiledReferences": ["nunit.framework.dll"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "overrideReferences": true,
  "autoReferenced": false
}
```

- [ ] **Step 3: Write the failing test**

Create `Assets/_Main/Tests/EditMode/ItemCardPickerTests.cs`:
```csharp
using NUnit.Framework;
using System.Collections.Generic;
using Luzart;

public class ItemCardPickerTests
{
    // weights: index 0 (Rare)=70, 1 (Epic)=25, 2 (Legend)=5
    static float[] W = { 70f, 25f, 5f };

    [Test]
    public void Pick_RollAtZero_ReturnsFirstBucket()
    {
        // roll 0 -> first non-empty bucket
        int idx = ItemCardPicker.PickWeightedIndex(W, 0f);
        Assert.AreEqual(0, idx);
    }

    [Test]
    public void Pick_RollInLastBucket_ReturnsLast()
    {
        // total=100; roll 97 -> falls into Legend bucket (95..100)
        int idx = ItemCardPicker.PickWeightedIndex(W, 97f);
        Assert.AreEqual(2, idx);
    }

    [Test]
    public void Pick_RollInMiddleBucket_ReturnsMiddle()
    {
        // roll 80 -> Epic bucket (70..95)
        int idx = ItemCardPicker.PickWeightedIndex(W, 80f);
        Assert.AreEqual(1, idx);
    }

    [Test]
    public void Pick_AllZeroWeights_ReturnsMinusOne()
    {
        int idx = ItemCardPicker.PickWeightedIndex(new float[] { 0f, 0f }, 0.5f);
        Assert.AreEqual(-1, idx);
    }
}
```

- [ ] **Step 4: Run test, verify it fails (compile error: ItemCardPicker missing)**

In session: MCP `run_tests(mode=EditMode, test_names=["ItemCardPickerTests"])` then `get_test_job`.
Expected: FAIL/compile error — `ItemCardPicker` not found.

- [ ] **Step 5: Implement the picker**

Create `Assets/_Main/Scripts/_IOPort/Cards/ItemCardPicker.cs`:
```csharp
namespace Luzart
{
    /// <summary>
    /// Pure, deterministic weighted bucket selection. No UnityEngine dependency so it is
    /// unit-testable. Caller supplies the random roll in [0, sum(weights)).
    /// </summary>
    public static class ItemCardPicker
    {
        /// <summary>
        /// Returns the index of the bucket that <paramref name="roll01TimesTotal"/> lands in,
        /// where roll is in [0, total). Returns -1 if all weights are non-positive.
        /// </summary>
        public static int PickWeightedIndex(float[] weights, float roll)
        {
            if (weights == null || weights.Length == 0) return -1;
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
                if (weights[i] > 0f) total += weights[i];
            if (total <= 0f) return -1;
            if (roll < 0f) roll = 0f;
            if (roll >= total) roll = total - 0.0001f;
            float acc = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f) continue;
                acc += weights[i];
                if (roll < acc) return i;
            }
            // fallback: last positive bucket
            for (int i = weights.Length - 1; i >= 0; i--)
                if (weights[i] > 0f) return i;
            return -1;
        }
    }
}
```

- [ ] **Step 6: Run tests, verify PASS**

MCP `run_tests(mode=EditMode, test_names=["ItemCardPickerTests"])`; expected: 4/4 pass.

- [ ] **Step 7: Commit**

```bash
git add "Assets/_Main/Tests/EditMode/IOPort.Tests.EditMode.asmdef" \
        "Assets/_Main/Tests/EditMode/ItemCardPickerTests.cs" \
        "Assets/_Main/Scripts/_IOPort/Cards/ItemCardPicker.cs"
git commit -m "feat(cards): add testable weighted item-card picker + EditMode test asmdef"
```

---

## Task 2: `ItemConfig.CardPool` field

**Files:**
- Modify: `Assets/_Main/Scripts/_LuzartGame/Items/ItemConfig.cs`

- [ ] **Step 1: Add the serialized field + accessor**

In `ItemConfig.cs`, add to the field block (after `private AssetLevel assetLevel;`):
```csharp
        [SerializeField] private ResourcePool _cardPool;
```
And add to the public accessors (near `public Sprite Sprite => _sprite;`):
```csharp
        public ResourcePool CardPool => _cardPool;
        public void SetCardPoolEditor(ResourcePool pool) { _cardPool = pool; }
```
(`SetCardPoolEditor` is used by the converter; it is a plain setter, safe to keep.)

- [ ] **Step 2: Compile clean**

MCP `refresh_unity(compile=request, wait_for_ready=true)` then `read_console(onlyErrors=true)`.
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add "Assets/_Main/Scripts/_LuzartGame/Items/ItemConfig.cs"
git commit -m "feat(cards): add per-item CardPool reference to ItemConfig"
```

---

## Task 3: Per-item card converter (generate pools + rewire costs)

**Files:**
- Create: `Assets/_Main/Editor/PerItemCardConverter.cs`

This is editor-only. It is verified by its own asserts + console log + spot-checking an item asset.

- [ ] **Step 1: Write the converter**

Create `Assets/_Main/Editor/PerItemCardConverter.cs`:
```csharp
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Luzart.EditorTools
{
    /// <summary>
    /// Converts the shared rarity-shard economy to per-item cards:
    /// for each ItemConfig, create a card ResourcePool+Definition (icon = item sprite),
    /// set ItemConfig.CardPool, and re-point unlock + level-up costs from the 3 rarity
    /// shard pools to the item's own card pool. Idempotent.
    /// </summary>
    public static class PerItemCardConverter
    {
        const string ITEMS_DIR = "Assets/_Main/Data/Items";
        const string CARDS_DIR = "Assets/_Main/Data/IOShop/Resources/Cards";

        // Retired shard pool GUIDs (Rare, Epic, Legend).
        static readonly HashSet<string> ShardPoolGuids = new HashSet<string>
        {
            "bb103affdc77c7246ab32b210c7bf81d",
            "1cb2ecffaed08b744933a07edb2ed5a0",
            "9652acfd7e72c3a43b8832c0b43e3ffa",
        };

        [MenuItem("Tools/IOShop/Convert To Per-Item Cards")]
        public static void Convert()
        {
            if (!Directory.Exists(CARDS_DIR)) Directory.CreateDirectory(CARDS_DIR);
            AssetDatabase.Refresh();

            string[] guids = AssetDatabase.FindAssets("t:ItemConfig", new[] { ITEMS_DIR });
            int made = 0, rewired = 0;
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var item = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
                if (item == null) continue;

                ResourcePool pool = EnsureCardPool(item, ref made);
                item.SetCardPoolEditor(pool);
                EditorUtility.SetDirty(item);

                if (RewireCosts(item, pool)) rewired++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PerItemCardConverter] items={guids.Length} poolsCreated={made} rewired={rewired}");
        }

        static ResourcePool EnsureCardPool(ItemConfig item, ref int made)
        {
            if (item.CardPool != null) return item.CardPool;

            string id = item.name; // e.g. ItemConfig_Army_Belt_Good
            string defPath = $"{CARDS_DIR}/ResourceDefinition_Card_{id}.asset";
            string poolPath = $"{CARDS_DIR}/ResourcePool_Card_{id}.asset";

            var def = AssetDatabase.LoadAssetAtPath<ResourceDefinition>(defPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ResourceDefinition>();
                def.name = $"ResourceDefinition_Card_{id}";
                SetPrivate(def, "_id", $"card_{id}_def");
                SetPrivate(def, "_displayName", item.NameItem);
                SetPrivate(def, "_eType", ETypeResourceDefinition.Shard);
                SetPrivate(def, "_mainImage", item.Sprite);
                AssetDatabase.CreateAsset(def, defPath);
            }

            var pool = AssetDatabase.LoadAssetAtPath<ResourcePool>(poolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<ResourcePool>();
                pool.name = $"ResourcePool_Card_{id}";
                SetPrivate(pool, "_id", $"card_{id}");
                SetPrivate(pool, "_resourceDefinition", def);
                AssetDatabase.CreateAsset(pool, poolPath);
                made++;
            }
            return pool;
        }

        // Replace shard-pool cost rows with the item's card pool, via SerializedObject so we
        // do not depend on the exact private field names of the cost classes.
        static bool RewireCosts(ItemConfig item, ResourcePool cardPool)
        {
            bool changed = false;
            var so = new SerializedObject(item);
            so.Update();
            var root = so.GetIterator();
            while (root.Next(true))
            {
                if (root.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (root.name != "resourcePool") continue;
                var refObj = root.objectReferenceValue;
                if (refObj == null) continue;
                string assetPath = AssetDatabase.GetAssetPath(refObj);
                string refGuid = AssetDatabase.AssetPathToGUID(assetPath);
                if (ShardPoolGuids.Contains(refGuid))
                {
                    root.objectReferenceValue = cardPool;
                    changed = true;
                }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        static void SetPrivate(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"field {field} not found on {target.name}"); return; }
            switch (p.propertyType)
            {
                case SerializedPropertyType.String: p.stringValue = (string)value; break;
                case SerializedPropertyType.ObjectReference: p.objectReferenceValue = (Object)value; break;
                case SerializedPropertyType.Enum: p.enumValueIndex = (int)value; break;
                default: Debug.LogError($"unsupported field type {p.propertyType} for {field}"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
```

NOTE: `ItemConfig.unlockable`/`assetLevel` are sub-assets embedded in the ItemConfig `.asset`,
but their cost rows are referenced through nested `SerializedObject` iteration on the **sub-asset**
objects, not the ItemConfig root. Adjust Step 2 if the spot-check shows shard refs remain.

- [ ] **Step 2: Compile + spot-check iteration scope**

MCP `refresh_unity(compile=request, wait_for_ready=true)`, `read_console(onlyErrors=true)` → no errors.
Then inspect one item BEFORE running: open `Assets/_Main/Data/Items/Good/ItemConfig_Army_Belt_Good.asset` and confirm the cost rows (`resourcePool` referencing shard guid `bb103…`) live on the embedded `AssetUnlockable`/`AssetLevel` sub-assets.

If costs are on sub-assets (they are — confirmed in spec), extend `RewireCosts` to also iterate the sub-assets:
```csharp
        static bool RewireCosts(ItemConfig item, ResourcePool cardPool)
        {
            bool changed = false;
            var targets = new List<Object> { item };
            string p = AssetDatabase.GetAssetPath(item);
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(p))
                if (sub != null && sub != item) targets.Add(sub);
            foreach (var t in targets)
                changed |= RewireOne(t, cardPool);
            return changed;
        }

        static bool RewireOne(Object target, ResourcePool cardPool)
        {
            bool changed = false;
            var so = new SerializedObject(target);
            so.Update();
            var it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.name != "resourcePool") continue;
                var refObj = it.objectReferenceValue;
                if (refObj == null) continue;
                string refGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(refObj));
                if (ShardPoolGuids.Contains(refGuid)) { it.objectReferenceValue = cardPool; changed = true; }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }
```
Replace the original `RewireCosts` with these two methods.

- [ ] **Step 3: Run the converter on a backup branch**

```bash
git checkout -b feat/per-item-cards
```
In session: MCP `execute_menu_item(menu_path="Tools/IOShop/Convert To Per-Item Cards")`.
Read console: expect `items=130 poolsCreated=130 rewired=130` (numbers may differ if some items lacked a shard row).

- [ ] **Step 4: Verify a converted item**

Read `Assets/_Main/Data/Items/Good/ItemConfig_Army_Belt_Good.asset`:
- `_cardPool` set to a `ResourcePool_Card_…` guid (not 0).
- unlock cost + every levelUp tier: the former shard guid (`bb103…`) is now the card pool guid; Gold guid `0c7143…` unchanged.
Confirm `Assets/_Main/Data/IOShop/Resources/Cards/` has 130 pool + 130 def assets.

- [ ] **Step 5: Verify no remaining shard refs in item costs**

```bash
cd "D:/Unity Training/survivorIOSource/DATN-GoGoSurvival"
for g in bb103affdc77c7246ab32b210c7bf81d 1cb2ecffaed08b744933a07edb2ed5a0 9652acfd7e72c3a43b8832c0b43e3ffa; do
  echo -n "$g item-refs: "; grep -rl "$g" Assets/_Main/Data/Items --include=*.asset 2>/dev/null | wc -l
done
```
Expected: all 0.

- [ ] **Step 6: Register card pools into DomainContentLoader**

The 130 pools must be in the SO save lifecycle. Inspect how `IOPortDataGenerator` registers pools into `DomainContentLoader.contents` (SerializedProperty append). Add a menu `Tools/IOShop/Register Card Pools` (in `PerItemCardConverter`) that loads every `ResourcePool_Card_*` under `CARDS_DIR` and appends any missing ones to the scene/prefab `DomainContentLoader.contents` list (mirror the existing generator's append code exactly). Run it. Verify count increases by 130 (idempotent on re-run).

- [ ] **Step 7: Commit**

```bash
git add "Assets/_Main/Editor/PerItemCardConverter.cs" \
        "Assets/_Main/Data/IOShop/Resources/Cards" \
        "Assets/_Main/Data/Items"
# plus the DomainContentLoader host scene/prefab file actually modified
git commit -m "feat(cards): per-item card pools + cost rewire converter"
```

---

## Task 4: Shop offer references an item

**Files:**
- Modify: `Assets/_Main/Scripts/_IOPort/Shop/ShopShardOffer.cs`

- [ ] **Step 1: Rewrite ShopShardOffer to reference an ItemConfig**

Replace the body of `ShopShardOffer.cs` with:
```csharp
using System;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// One "buy item cards with Gold" offer in the shop: spend <see cref="price"/> Gold from
    /// <see cref="goldPool"/> to gain <see cref="amount"/> cards of <see cref="item"/>
    /// (added to <c>item.CardPool</c>). Rarity/icon are read from the item.
    /// </summary>
    [Serializable]
    public class ShopShardOffer
    {
        [SerializeField] private ItemConfig item;
        [SerializeField] private ResourcePool goldPool;
        [SerializeField] private int price = 100;
        [SerializeField] private int amount = 5;

        public ItemConfig Item => item;
        public ResourcePool GoldPool => goldPool;
        public ResourcePool CardPool => item != null ? item.CardPool : null;
        public ERarity Rarity => item != null ? item.Rarity : ERarity.Rare;
        public Sprite Icon => item != null ? item.Sprite : null;
        public int Price => price;
        public int Amount => amount;

        public ShopShardOffer() { }
    }
}
```

- [ ] **Step 2: Compile clean**

`ShopBuyShardView` will now have compile errors (references `Data.ShardPool`, `Data.Rarity`). That is expected — Task 5 fixes it. Compile after Task 5. For now just ensure `ShopShardOffer.cs` itself has no syntax error by reading it back.

- [ ] **Step 3: Commit**

```bash
git add "Assets/_Main/Scripts/_IOPort/Shop/ShopShardOffer.cs"
git commit -m "feat(shop): ShopShardOffer references a specific item"
```

---

## Task 5: Shop card view — item icon, gold icon, buy into item pool

**Files:**
- Modify: `Assets/_Main/Scripts/_IOPort/Shop/ShopBuyShardView.cs`
- Test: `Assets/_Main/Tests/EditMode/ShopBuyShardLogicTests.cs`

- [ ] **Step 1: Write a failing logic test for the buy guard**

Create `Assets/_Main/Tests/EditMode/ShopBuyShardLogicTests.cs`:
```csharp
using NUnit.Framework;
using Luzart;

public class ShopBuyShardLogicTests
{
    [Test]
    public void CanAfford_True_WhenGoldAtLeastPrice()
    {
        Assert.IsTrue(ShopBuyLogic.CanAfford(gold: 100, price: 100));
        Assert.IsTrue(ShopBuyLogic.CanAfford(gold: 150, price: 100));
    }

    [Test]
    public void CanAfford_False_WhenGoldBelowPrice()
    {
        Assert.IsFalse(ShopBuyLogic.CanAfford(gold: 99, price: 100));
    }
}
```

- [ ] **Step 2: Run, verify fail (ShopBuyLogic missing)**

MCP `run_tests(mode=EditMode, test_names=["ShopBuyShardLogicTests"])`. Expected: compile fail.

- [ ] **Step 3: Add the pure guard + rewrite the view**

Replace `ShopBuyShardView.cs` with:
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    /// <summary>Pure affordability check, separated for unit testing.</summary>
    public static class ShopBuyLogic
    {
        public static bool CanAfford(double gold, int price) => gold >= price;
    }

    /// <summary>
    /// One item-card offer card in the shop. Shows the item icon over a rarity frame, the
    /// player's current card count for that item, a Gold price (with Gold icon), and a Buy
    /// button. Buying spends Gold and adds cards to the item's own card pool.
    /// Prefab layout is unchanged; this only fills in Icon + GoldIcon if wired.
    /// </summary>
    public class ShopBuyShardView : ViewT<ShopShardOffer>
    {
        [SerializeField] private Image imFrame;   // rarity-colored backing (existing)
        [SerializeField] private Image imIcon;    // NEW: the item card image
        [SerializeField] private Image imGoldIcon;// NEW: gold coin next to price
        [SerializeField] private Sprite goldSprite;
        [SerializeField] private TMP_Text txtCount;
        [SerializeField] private TMP_Text txtPrice;

        private INumber _cardNumber;

        protected override void OnSetup()
        {
            base.OnSetup();
            if (imFrame != null)
            {
                var resolver = FindRarityResolver();
                if (resolver != null) imFrame.sprite = resolver.GetSpriteByRarity(Data.Rarity);
            }
            if (imIcon != null) imIcon.sprite = Data.Icon;
            if (imGoldIcon != null && goldSprite != null) imGoldIcon.sprite = goldSprite;
            if (txtPrice != null) txtPrice.text = $"{Data.Price}";

            if (Data.CardPool != null)
            {
                _cardNumber = ((IResourcePool)Data.CardPool).Value;
                if (_cardNumber != null) _cardNumber.Changed += OnCardChanged;
            }
            RefreshCount();
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            if (_cardNumber != null) _cardNumber.Changed -= OnCardChanged;
            _cardNumber = null;
        }

        private void OnCardChanged(INumber number) => RefreshCount();

        private void RefreshCount()
        {
            if (txtCount == null || Data == null || Data.CardPool == null) return;
            int count = (int)((IResourcePool)Data.CardPool).Value.Value;
            txtCount.text = $"x{count}";
        }

        public void OnClickBuy()
        {
            if (Data == null || Data.CardPool == null || Data.GoldPool == null) return;
            double gold = ((IResourcePool)Data.GoldPool).Value.Value;
            if (!ShopBuyLogic.CanAfford(gold, Data.Price)) return;
            if (!Data.GoldPool.TryRemove(Data.Price)) return;
            Data.CardPool.Add(Data.Amount);
        }

        private RaritySpriteResolver FindRarityResolver()
        {
            if (SceneRootManager.Instance == null || SceneRootManager.Instance._domain == null) return null;
            var all = SceneRootManager.Instance._domain.GetAll<IVisualResolver>();
            foreach (var resolver in all)
                if (resolver is RaritySpriteResolver r) return r;
            return null;
        }
    }
}
```

- [ ] **Step 4: Run tests + compile**

MCP `refresh_unity(compile=request)`, `read_console(onlyErrors=true)` → no errors.
MCP `run_tests(mode=EditMode, test_names=["ShopBuyShardLogicTests"])` → 2/2 pass.

- [ ] **Step 5: Commit**

```bash
git add "Assets/_Main/Scripts/_IOPort/Shop/ShopBuyShardView.cs" \
        "Assets/_Main/Tests/EditMode/ShopBuyShardLogicTests.cs"
git commit -m "feat(shop): show item card icon + gold price, buy into item card pool"
```

---

## Task 6: Add Icon + GoldIcon to ShopShardCard prefab (layout preserved)

**Files:**
- Modify: `Assets/_Main/Editor/IOPortPrefabGenerator.cs`
- Asset: `Assets/_Main/Data/IOShop/Prefabs/ShopShardCard.prefab`

- [ ] **Step 1: Inspect current prefab build code**

Read `IOPortPrefabGenerator.cs`; find where `ShopShardCard` is built and `ShopBuyShardView` fields are wired (`imFrame`, `txtCount`, `txtPrice`). Note the method name.

- [ ] **Step 2: Add Icon + GoldIcon creation, idempotently**

In that method, after `Frame` is created, add (adapt variable names to the file):
```csharp
            // NEW: item card icon centered over the frame (additive, layout unchanged)
            var iconGo = GetOrCreateChild(card.transform, "Icon");
            var icon = EnsureComponent<Image>(iconGo);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(70f, 70f);
            icon.raycastTarget = false;

            // NEW: gold icon to the LEFT of the price text inside BuyButton
            var goldGo = GetOrCreateChild(priceText.transform.parent, "GoldIcon");
            var goldImg = EnsureComponent<Image>(goldGo);
            var goldRt = goldGo.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0f, 0.5f);
            goldRt.anchorMax = new Vector2(0f, 0.5f);
            goldRt.pivot = new Vector2(0f, 0.5f);
            goldRt.anchoredPosition = new Vector2(8f, 0f);
            goldRt.sizeDelta = new Vector2(28f, 28f);
            goldImg.raycastTarget = false;
            goldImg.sprite = LoadGoldSprite();

            // wire the new view fields
            view.GetType(); // (use SerializedObject or direct field set as the file already does)
```
Then wire `imIcon = icon`, `imGoldIcon = goldImg`, `goldSprite = LoadGoldSprite()` the same way the file already wires `imFrame`/`txtCount`/`txtPrice` (direct field assignment if the generator uses reflection/SerializedObject, otherwise via the public setters).

Add helpers if not present:
```csharp
        static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
        static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
        static Sprite LoadGoldSprite()
        {
            // Use the project's gold/coin sprite. Resolve by known path; adjust if different.
            return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Main/UI/User Interface/13.png");
        }
```
NOTE: confirm the actual gold/coin sprite path during Step 1 (search the existing shop prefabs for the coin sprite guid) and set `LoadGoldSprite` accordingly. Do NOT change any existing RectTransform of Frame/Count/Price (layout must stay).

- [ ] **Step 3: Rebuild the prefab + compile**

MCP `refresh_unity(compile=request)`, `read_console(onlyErrors=true)` → no errors.
Run the shop-card build menu (the one `IOPortPrefabGenerator` exposes). Re-run once → confirm idempotent (no duplicate Icon/GoldIcon).

- [ ] **Step 4: Verify prefab hierarchy**

MCP `manage_prefabs(get_hierarchy, ShopShardCard.prefab)` → children now include `Frame`, `Icon`, `Count`, `BuyButton/{Price, GoldIcon}`. Positions of Frame/Count/Price unchanged.

- [ ] **Step 5: Commit**

```bash
git add "Assets/_Main/Editor/IOPortPrefabGenerator.cs" \
        "Assets/_Main/Data/IOShop/Prefabs/ShopShardCard.prefab"
git commit -m "feat(shop): add item icon + gold icon to shard card (layout unchanged)"
```

---

## Task 7: Author shop offers as item cards

**Files:**
- Modify: `Assets/_Main/Editor/IOPortDataGenerator.cs`
- Asset: `Assets/_Main/Data/IOShop/System/Data_Shop.asset`

- [ ] **Step 1: Update the offer-authoring menu**

Find where `IOPortDataGenerator` writes `shardOffers` (Rare/Epic/Legend). Change it to author a fixed
list of item-card offers. Use a small author-chosen list (one per rarity to start):
```csharp
            // Author a fixed list of item-card offers. Pick representative items by rarity.
            var so = new SerializedObject(dataShop);
            var offers = so.FindProperty("shardOffers");
            offers.arraySize = 0;
            AddItemOffer(offers, "ItemConfig_Army_Belt_Good", goldPool, price: 100, amount: 5);
            AddItemOffer(offers, "ItemConfig_Bone_Pendant_Better", goldPool, price: 250, amount: 3);
            AddItemOffer(offers, "ItemConfig_Carapace_Best", goldPool, price: 500, amount: 2);
            so.ApplyModifiedPropertiesWithoutUndo();
```
With helper (resolves the ItemConfig by asset name):
```csharp
        static void AddItemOffer(SerializedProperty offers, string itemAssetName, ResourcePool goldPool, int price, int amount)
        {
            var guids = AssetDatabase.FindAssets($"{itemAssetName} t:ItemConfig");
            ItemConfig item = null;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var cand = AssetDatabase.LoadAssetAtPath<ItemConfig>(p);
                if (cand != null && cand.name == itemAssetName) { item = cand; break; }
            }
            if (item == null) { Debug.LogWarning($"offer item not found: {itemAssetName}"); return; }
            int i = offers.arraySize; offers.arraySize = i + 1;
            var e = offers.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("item").objectReferenceValue = item;
            e.FindPropertyRelative("goldPool").objectReferenceValue = goldPool;
            e.FindPropertyRelative("price").intValue = price;
            e.FindPropertyRelative("amount").intValue = amount;
        }
```
(`item` names above are placeholders for whatever 3+ items the user wants — confirm exact asset names exist with the verify step. The user can edit `Data_Shop.asset` afterward to add/remove offers.)

- [ ] **Step 2: Compile + run the menu**

`refresh_unity(compile=request)`, no errors. Run the data-author menu. Read `Data_Shop.asset`: `shardOffers` now has entries with `item`/`goldPool`/`price`/`amount`, no `shardPool`/`rarity`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/_Main/Editor/IOPortDataGenerator.cs" \
        "Assets/_Main/Data/IOShop/System/Data_Shop.asset"
git commit -m "feat(shop): author item-card offers (fixed list) instead of rarity shards"
```

---

## Task 8: Win reward — weighted random item card

**Files:**
- Modify: `Assets/_Main/Scripts/_IOPort/Shop/Data_WinReward.cs`

- [ ] **Step 1: Rewrite Data_WinReward**

Replace `Data_WinReward.cs` with:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// On a WON Classic run, grants cards of a random item, weighted by the item's rarity.
    /// Silent grant: cards accumulate toward that item's unlock/upgrade cost.
    /// </summary>
    public class Data_WinReward : AbstractScriptableContent
    {
        [Header("Card grant (per win)")]
        [SerializeField] private int cardMin = 3;
        [SerializeField] private int cardMax = 8;

        [Header("Rarity weights (Rare/Epic/Legend)")]
        [SerializeField] private float weightRare = 70f;
        [SerializeField] private float weightEpic = 25f;
        [SerializeField] private float weightLegend = 5f;

        [Header("Refs")]
        [SerializeField] private InventoryItemData inventoryItemData;

        private bool _registered;

        protected override void DoInitialize()
        {
            base.DoInitialize();
            if (inventoryItemData == null && _domain != null)
                inventoryItemData = _domain.Get<InventoryItemData>();
            Broadcaster.Register<Data_ClassicEndGame>(OnEndGame);
            _registered = true;
        }

        protected override void DoTerminate()
        {
            base.DoTerminate();
            if (_registered) { Broadcaster.Unregister<Data_ClassicEndGame>(OnEndGame); _registered = false; }
        }

        private void OnEndGame(Data_ClassicEndGame data)
        {
            if (!data.IsWin) return;
            GrantRandomItemCards();
        }

        private void GrantRandomItemCards()
        {
            if (inventoryItemData == null) return;
            var all = inventoryItemData.GetAllItemConfigDontBuy();
            if (all == null || all.Count == 0) return;

            // Pick a rarity bucket by weight, then a random item of that rarity.
            float[] weights = { weightRare, weightEpic, weightLegend };
            float total = weights[0] + weights[1] + weights[2];
            if (total <= 0f) return;
            float roll = Random.Range(0f, total);
            int rarityIdx = ItemCardPicker.PickWeightedIndex(weights, roll);
            if (rarityIdx < 0) return;
            var rarity = (ERarity)rarityIdx;

            var pool = new List<ItemConfig>();
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].Rarity == rarity && all[i].CardPool != null) pool.Add(all[i]);
            // fallback to any item with a card pool if no locked item of that rarity
            if (pool.Count == 0)
                for (int i = 0; i < all.Count; i++)
                    if (all[i] != null && all[i].CardPool != null) pool.Add(all[i]);
            if (pool.Count == 0) return;

            var item = pool[Random.Range(0, pool.Count)];
            int lo = Mathf.Min(cardMin, cardMax);
            int hi = Mathf.Max(cardMin, cardMax);
            int amount = Random.Range(lo, hi + 1);
            if (amount <= 0) return;
            item.CardPool.Add(amount);
        }
    }
}
```
NOTE: if `GetAllItemConfigDontBuy()` only returns LOCKED items, wins won't drop cards for already-unlocked items (so you can't farm upgrade cards). If upgrade-card farming is desired, switch to an "all items" accessor on `InventoryItemData`; confirm with the user. Default keeps locked-only (matches existing accessor) — flagged for the review checkpoint.

- [ ] **Step 2: Update Data_WinReward.asset fields**

The asset still has old fields (`cardChancePercent`, `shardMin/Max`, `shardPools`). After recompile, open `Data_WinReward.asset` and confirm new serialized fields appear; set weights + cardMin/Max. Old fields drop out automatically. Re-assign `inventoryItemData` if it cleared.

- [ ] **Step 3: Compile clean**

`refresh_unity(compile=request)`, `read_console(onlyErrors=true)` → no errors.

- [ ] **Step 4: Commit**

```bash
git add "Assets/_Main/Scripts/_IOPort/Shop/Data_WinReward.cs" \
        "Assets/_Main/Data/IOShop/System/Data_WinReward.asset"
git commit -m "feat(reward): win grants weighted random item cards"
```

---

## Task 9: Item / Inventory screen reflects per-item cards

**Files:**
- Inspect/Modify: inventory item view prefab + binding (`ItemViewInventory.prefab`, `ItemViewInventory.cs` / `ItemShopUnlockView.cs`)

- [ ] **Step 1: Inspect current binding**

Read `Assets/_Main/Scripts/_IOPort/PopupItem/ItemViewInventory.cs` and `ItemShopUnlockView.cs`. They show cost rows via `objectView.Setup(...)` + the unlock cost list. Since costs now reference `item.CardPool`, the require/current numbers already read the per-item card pool. Confirm by reading the view code — no logic change expected.

- [ ] **Step 2: Play-mode visual check**

Enter play mode (`manage_editor action=play`), open the inventory screen, pick a card.
Capture with `ScreenCapture.CaptureScreenshot` (NOT Main Camera) or `manage_camera screenshot capture_source=scene_view`.
Verify: each card shows the item icon + `cards owned / required` from its own pool; different items show independent counts (not the old shared rarity number). Stop play mode.

- [ ] **Step 3: Fix only if mismatched**

If a view still shows a shard label or shared count, adjust that view's binding to read `item.CardPool` / the rewired cost row. Make the minimal change. Recompile, re-verify.

- [ ] **Step 4: Commit (only if changed)**

```bash
git add <changed files>
git commit -m "fix(inventory): show per-item card progress"
```

---

## Task 10: Retire the rarity shard assets

**Files:**
- Delete: `ResourcePool_Shard_{Rare,Epic,Legend}.asset` + `ResourceDefinition_Shard_{Rare,Epic,Legend}.asset` (and `.meta`)

- [ ] **Step 1: Verify zero references project-wide**

```bash
cd "D:/Unity Training/survivorIOSource/DATN-GoGoSurvival"
for g in bb103affdc77c7246ab32b210c7bf81d 1cb2ecffaed08b744933a07edb2ed5a0 9652acfd7e72c3a43b8832c0b43e3ffa; do
  echo -n "$g all-refs: "; grep -rl "$g" Assets --include=*.asset --include=*.prefab --include=*.unity 2>/dev/null | grep -v "ResourcePool_Shard\|ResourceDefinition_Shard" | wc -l
done
```
Expected: all 0 (only the shard assets themselves reference their own def). If any non-shard file still references them (e.g. `Data_WinReward.asset` leftover, `DomainContentLoader`), clean those first. Do NOT delete while refs remain.

- [ ] **Step 2: Delete the assets**

Via MCP `manage_asset(action=delete, ...)` for each of the 6 assets (so Unity updates the AssetDatabase). Then `refresh_unity`.

- [ ] **Step 3: Compile + console clean**

`read_console(onlyErrors=true)` → no missing-reference errors at boot.

- [ ] **Step 4: Commit**

```bash
git add -u "Assets/_Main/Data/IOShop/Resources"
git commit -m "chore(cards): remove retired rarity-shard pools/definitions"
```

---

## Task 11: Full end-to-end verification

- [ ] **Step 1: Headless EditMode test run**

```
"C:/Program Files/Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "D:/Unity Training/survivorIOSource/DATN-GoGoSurvival" \
  -runTests -testPlatform editmode -testResults Temp/tests.xml -logFile Temp/tests.log
```
Check `Temp/tests.xml` `result="Passed"` and `failed="0"`. (Runner exits 0 even on failures — read the XML.)

- [ ] **Step 2: Play-mode loop check**

Enter play, open Card Shop: each offer shows item icon over rarity frame, `x{owned}` count, Gold icon + price. Buy with enough Gold → Gold decreases, item card count increases, count text updates live. Buy with insufficient Gold → blocked. Open inventory → that item's progress increased. Simulate a win (or call the win broadcast) → a random item's card pool increased; over many wins, Rare most frequent.

- [ ] **Step 3: Final console audit**

`read_console(onlyErrors=true)` → zero new errors/exceptions, no `Invalid UIConfig`, no missing references.

- [ ] **Step 4: Final commit / branch ready**

```bash
git status
# branch feat/per-item-cards ready for review/merge
```

---

## Self-Review notes (author)

- Spec D1–D6 coverage: D1 same-item cards (Task 3 cost rewire), D2 weighted win (Task 8), D3 fixed item offers + Gold (Task 7), D4 unlock+upgrade consume cards/keep Gold (Task 3), D5 Approach-1 pools (Task 3), D6 shop layout preserved + item/inventory updated (Tasks 5,6,9).
- Open decision for review checkpoint (Task 8 NOTE): whether wins can drop cards for already-unlocked items (upgrade farming). Surface to user before finalizing Task 8.
- Risk: cost rows live on embedded sub-assets — Task 3 Step 2 explicitly handles sub-asset iteration; verify before bulk run.
