#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Luzart;

/// <summary>
/// Converts the shared rarity-shard economy to per-item cards.
/// For each ItemConfig: ensure a card ResourcePool+Definition (icon = item sprite),
/// set ItemConfig.CardPool, and re-point unlock + level-up cost rows from the 3 rarity
/// shard pools to the item's own card pool. Also registers the new card pools + definitions
/// into the open scene's DomainContentLoader so they participate in the Init/Load/Save lifecycle
/// (required: ResourcePool.Add dereferences the loaded number).
/// Idempotent. Does NOT touch any screen prefab (PopupShop/PopupInventory).
/// </summary>
public static class PerItemCardConverter
{
    const string ITEMS_DIR = "Assets/_Main/Data/Items";
    const string CARDS_DIR = "Assets/_Main/Data/IOShop/Resources/Cards";
    const string COST_VISUAL = "Assets/_Main/Data/IOShop/Resources/CostVisual_ResourcePool.asset";

    static readonly string[] ShardPoolGuids =
    {
        "bb103affdc77c7246ab32b210c7bf81d", // Rare
        "1cb2ecffaed08b744933a07edb2ed5a0", // Epic
        "9652acfd7e72c3a43b8832c0b43e3ffa", // Legend
    };

    static readonly BindingFlags FF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static FieldInfo Field(Type t, string name)
    {
        for (var ty = t; ty != null; ty = ty.BaseType)
        {
            var f = ty.GetField(name, FF);
            if (f != null) return f;
        }
        return null;
    }
    static void Set(object obj, string name, object value)
    {
        var f = Field(obj.GetType(), name);
        if (f == null) { Debug.LogError($"[Cards] field {name} not found on {obj.GetType().Name}"); return; }
        f.SetValue(obj, value);
    }
    static object Get(object obj, string name) => Field(obj.GetType(), name)?.GetValue(obj);

    static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        var leaf = System.IO.Path.GetFileName(path);
        EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    [MenuItem("Tools/IOShop/Convert To Per-Item Cards")]
    public static void Convert()
    {
        EnsureDir(CARDS_DIR);

        // Load the 3 retired shard pools so we can detect cost rows that reference them.
        var shardPools = new HashSet<UnityEngine.Object>();
        foreach (var g in ShardPoolGuids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var pool = AssetDatabase.LoadAssetAtPath<ResourcePool>(p);
            if (pool != null) shardPools.Add(pool);
        }

        var costResolver = AssetDatabase.LoadAssetAtPath<ScriptableObject>(COST_VISUAL);

        string[] guids = AssetDatabase.FindAssets("t:ItemConfig", new[] { ITEMS_DIR });
        var createdPools = new List<ResourcePool>();
        var createdDefs = new List<ResourceDefinition>();
        int made = 0, rewired = 0;

        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var item = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
            if (item == null) continue;

            ResourcePool pool = EnsureCardPool(item, costResolver, createdPools, createdDefs, ref made);
            item.SetCardPoolEditor(pool);
            EditorUtility.SetDirty(item);

            if (RewireCosts(item, pool, shardPools)) rewired++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int registered = RegisterCardPoolsInScene();
        Debug.Log($"[Cards] items={guids.Length} poolsCreated={made} rewired={rewired} registeredInScene={registered}");
    }

    static ResourcePool EnsureCardPool(ItemConfig item, ScriptableObject costResolver,
        List<ResourcePool> createdPools, List<ResourceDefinition> createdDefs, ref int made)
    {
        if (item.CardPool != null)
        {
            createdPools.Add(item.CardPool);
            var existingDef = Get(item.CardPool, "_resourceDefinition") as ResourceDefinition;
            if (existingDef != null) createdDefs.Add(existingDef);
            return item.CardPool;
        }

        string id = item.name; // e.g. ItemConfig_Army_Belt_Good
        string defPath = $"{CARDS_DIR}/ResourceDefinition_Card_{id}.asset";
        string poolPath = $"{CARDS_DIR}/ResourcePool_Card_{id}.asset";

        var def = AssetDatabase.LoadAssetAtPath<ResourceDefinition>(defPath);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<ResourceDefinition>();
            def.name = $"ResourceDefinition_Card_{id}";
            Set(def, "_id", $"card_{id}_def");
            Set(def, "_displayName", string.IsNullOrEmpty(item.NameItem) ? id : item.NameItem);
            Set(def, "_eType", ETypeResourceDefinition.Shard);
            Set(def, "_mainImage", item.Sprite);
            if (costResolver != null) Set(def, "_assetCostVisualResolver", costResolver);
            AssetDatabase.CreateAsset(def, defPath);
        }
        createdDefs.Add(def);

        var pool = AssetDatabase.LoadAssetAtPath<ResourcePool>(poolPath);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<ResourcePool>();
            pool.name = $"ResourcePool_Card_{id}";
            Set(pool, "_id", $"card_{id}");
            Set(pool, "_resourceDefinition", def);
            AssetDatabase.CreateAsset(pool, poolPath);
            made++;
        }
        createdPools.Add(pool);
        return pool;
    }

    // Swap any cost row referencing a retired shard pool to the item's card pool.
    // Costs live on the embedded AssetUnlockable (unlockCosts) + AssetLevel (levelUpCosts) sub-assets.
    static bool RewireCosts(ItemConfig item, ResourcePool cardPool, HashSet<UnityEngine.Object> shardPools)
    {
        bool changed = false;

        var unlock = Get(item, "unlockable");
        if (unlock != null)
        {
            var unlockCosts = Get(unlock, "unlockCosts") as IList;
            if (unlockCosts != null)
            {
                foreach (var cost in unlockCosts)
                    changed |= RewireCost(cost, cardPool, shardPools);
                if (changed) EditorUtility.SetDirty((UnityEngine.Object)unlock);
            }
        }

        var level = Get(item, "assetLevel");
        if (level != null)
        {
            bool levelChanged = false;
            var levelUpCosts = Get(level, "levelUpCosts") as IList;
            if (levelUpCosts != null)
            {
                foreach (var tier in levelUpCosts)
                {
                    var list = Get(tier, "List") as IList;
                    if (list == null) continue;
                    foreach (var cost in list)
                        levelChanged |= RewireCost(cost, cardPool, shardPools);
                }
            }
            if (levelChanged) { EditorUtility.SetDirty((UnityEngine.Object)level); changed = true; }
        }

        return changed;
    }

    static bool RewireCost(object cost, ResourcePool cardPool, HashSet<UnityEngine.Object> shardPools)
    {
        if (cost == null) return false;
        var refObj = Get(cost, "resourcePool") as UnityEngine.Object;
        if (refObj == null) return false;
        if (!shardPools.Contains(refObj)) return false;
        Set(cost, "resourcePool", cardPool);
        return true;
    }

    const string GOLD_GUID = "0c7143df3f325a34d91ccca22b5687de";
    const string DATA_SHOP = "Assets/_Main/Data/IOShop/System/Data_Shop.asset";

    // Authors a fixed shop offer list: one item per rarity (Rare/Epic/Legend), paid in Gold.
    // Surgical — only edits Data_Shop.shardOffers, never rebuilds any screen prefab.
    [MenuItem("Tools/IOShop/Author Item Card Offers")]
    public static void AuthorItemCardOffers()
    {
        var shop = AssetDatabase.LoadAssetAtPath<Data_Shop>(DATA_SHOP);
        if (shop == null) { Debug.LogError("[Cards] Data_Shop not found."); return; }
        var gold = AssetDatabase.LoadAssetAtPath<ResourcePool>(AssetDatabase.GUIDToAssetPath(GOLD_GUID));
        if (gold == null) { Debug.LogError("[Cards] Gold pool not found."); return; }

        // pick first item of each rarity (by ItemConfig.Rarity), ensuring it has a card pool
        ItemConfig rare = null, epic = null, legend = null;
        foreach (var g in AssetDatabase.FindAssets("t:ItemConfig", new[] { ITEMS_DIR }))
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemConfig>(AssetDatabase.GUIDToAssetPath(g));
            if (item == null || item.CardPool == null) continue;
            if (rare == null && item.Rarity == ERarity.Rare) rare = item;
            else if (epic == null && item.Rarity == ERarity.Epic) epic = item;
            else if (legend == null && item.Rarity == ERarity.Legend) legend = item;
            if (rare != null && epic != null && legend != null) break;
        }

        var offers = new List<ShopShardOffer>();
        if (rare != null) offers.Add(new ShopShardOffer(rare, gold, 100, 5));
        if (epic != null) offers.Add(new ShopShardOffer(epic, gold, 250, 3));
        if (legend != null) offers.Add(new ShopShardOffer(legend, gold, 500, 2));

        Set(shop, "shardOffers", offers);
        EditorUtility.SetDirty(shop);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Cards] Authored {offers.Count} item-card offers: " +
                  $"Rare={(rare ? rare.name : "-")} Epic={(epic ? epic.name : "-")} Legend={(legend ? legend.name : "-")}");
    }

    [MenuItem("Tools/IOShop/Register Card Pools In Scene")]
    public static void RegisterCardPoolsMenu()
    {
        int n = RegisterCardPoolsInScene();
        Debug.Log($"[Cards] registeredInScene={n}");
    }

    // Unregisters the 3 retired rarity shard pools + their definitions from the scene's
    // DomainContentLoader and deletes the 6 assets. Run AFTER conversion (zero cost refs).
    [MenuItem("Tools/IOShop/Retire Shard Pools")]
    public static void RetireShardPools()
    {
        var shardObjs = new HashSet<UnityEngine.Object>();
        foreach (var g in ShardPoolGuids)
        {
            var pool = AssetDatabase.LoadAssetAtPath<ResourcePool>(AssetDatabase.GUIDToAssetPath(g));
            if (pool == null) continue;
            shardObjs.Add(pool);
            var def = Get(pool, "_resourceDefinition") as ResourceDefinition;
            if (def != null) shardObjs.Add(def);
        }
        if (shardObjs.Count == 0) { Debug.Log("[Cards] No shard pools found (already retired)."); return; }

        int removed = 0;
        var loader = UnityEngine.Object.FindObjectOfType<DomainContentLoader>(true);
        if (loader != null)
        {
            var so = new SerializedObject(loader);
            var contents = so.FindProperty("contents");
            for (int i = contents.arraySize - 1; i >= 0; i--)
            {
                var o = contents.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o != null && shardObjs.Contains(o))
                {
                    contents.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    contents.DeleteArrayElementAtIndex(i);
                    removed++;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loader);
            EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
        }

        int deleted = 0;
        var paths = new List<string>();
        foreach (var o in shardObjs)
        {
            var p = AssetDatabase.GetAssetPath(o);
            if (!string.IsNullOrEmpty(p)) paths.Add(p);
        }
        foreach (var p in paths)
            if (AssetDatabase.DeleteAsset(p)) deleted++;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Cards] Retire shards: removedFromContents={removed} assetsDeleted={deleted}");
    }

    static int RegisterCardPoolsInScene()
    {
        var loader = UnityEngine.Object.FindObjectOfType<DomainContentLoader>(true);
        if (loader == null) { Debug.LogError("[Cards] No DomainContentLoader in open scene — open GamePlay.unity."); return 0; }

        var so = new SerializedObject(loader);
        var contentsProp = so.FindProperty("contents");
        var existing = new HashSet<UnityEngine.Object>();
        for (int i = 0; i < contentsProp.arraySize; i++)
            existing.Add(contentsProp.GetArrayElementAtIndex(i).objectReferenceValue);

        int added = 0;
        void Add(UnityEngine.Object o)
        {
            if (o == null || existing.Contains(o)) return;
            contentsProp.arraySize++;
            contentsProp.GetArrayElementAtIndex(contentsProp.arraySize - 1).objectReferenceValue = o;
            existing.Add(o);
            added++;
        }

        foreach (var g in AssetDatabase.FindAssets("t:ResourcePool", new[] { CARDS_DIR }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var pool = AssetDatabase.LoadAssetAtPath<ResourcePool>(p);
            if (pool == null) continue;
            Add(pool);
            var def = Get(pool, "_resourceDefinition") as ResourceDefinition;
            if (def != null) Add(def);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(loader);
        EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        return added;
    }

    // Play-mode verification helper: opens the shop popup so its visuals can be inspected.
    [MenuItem("Tools/IOShop/DEBUG Open Shop (Play)")]
    public static void DebugOpenShop()
    {
        var bs = UnityEngine.Object.FindObjectOfType<IOPortBootstrap>();
        if (bs == null) { Debug.LogError("[Cards] No IOPortBootstrap in scene (enter Play mode first)."); return; }
        bs.OpenShop();
    }

    // Play-mode verification: report the live shop offer views (finds DontDestroyOnLoad too).
    [MenuItem("Tools/IOShop/DEBUG Report Shop Cards (Play)")]
    public static void DebugReportShopCards()
    {
        var views = UnityEngine.Object.FindObjectsByType<ShopBuyShardView>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[Cards] Live shop offer views = {views.Length}");
        foreach (var v in views)
        {
            var icon = Get(v, "imIcon") as UnityEngine.UI.Image;
            var gold = Get(v, "imGoldIcon") as UnityEngine.UI.Image;
            var price = Get(v, "txtPrice") as TMPro.TMP_Text;
            var count = Get(v, "txtCount") as TMPro.TMP_Text;
            Debug.Log($"[Cards]  '{v.name}': icon={(icon != null && icon.sprite ? icon.sprite.name : "NULL")} " +
                      $"gold={(gold != null && gold.sprite ? gold.sprite.name : "NULL")} " +
                      $"count={(count ? count.text : "?")} price={(price ? price.text : "?")}");
        }
    }

    // Play-mode: dump the open shop popup tree so we can see static mockup cards vs spawned offers.
    [MenuItem("Tools/IOShop/DEBUG Dump Shop Tree (Play)")]
    public static void DebugDumpShopTree()
    {
        var shops = UnityEngine.Object.FindObjectsByType<PopupShop>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[Cards] PopupShop instances = {shops.Length}");
        foreach (var s in shops)
            DumpTree(s.transform, 0, new System.Text.StringBuilder()).ToString();
    }

    static System.Text.StringBuilder DumpTree(Transform t, int depth, System.Text.StringBuilder sb)
    {
        string pad = new string('-', depth);
        bool hasOffer = t.GetComponent<ShopBuyShardView>() != null;
        bool hasBtn = t.GetComponent<UnityEngine.UI.Button>() != null;
        Debug.Log($"[Tree] {pad}{t.name}{(t.gameObject.activeSelf ? "" : " [OFF]")}{(hasOffer ? " <OFFER>" : "")}{(hasBtn ? " <btn>" : "")}");
        if (depth < 3)
            for (int i = 0; i < t.childCount; i++) DumpTree(t.GetChild(i), depth + 1, sb);
        return sb;
    }

    [MenuItem("Tools/IOShop/Verify No Shard Refs")]
    public static void VerifyNoShardRefs()
    {
        var shardPools = new HashSet<UnityEngine.Object>();
        foreach (var g in ShardPoolGuids)
        {
            var pool = AssetDatabase.LoadAssetAtPath<ResourcePool>(AssetDatabase.GUIDToAssetPath(g));
            if (pool != null) shardPools.Add(pool);
        }
        int hits = 0;
        foreach (var g in AssetDatabase.FindAssets("t:ItemConfig", new[] { ITEMS_DIR }))
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemConfig>(AssetDatabase.GUIDToAssetPath(g));
            if (item == null) continue;
            var unlock = Get(item, "unlockable");
            if (unlock != null && (Get(unlock, "unlockCosts") as IList) is IList ul)
                foreach (var c in ul) if (shardPools.Contains(Get(c, "resourcePool") as UnityEngine.Object)) hits++;
            var level = Get(item, "assetLevel");
            if (level != null && (Get(level, "levelUpCosts") as IList) is IList ll)
                foreach (var tier in ll)
                    if ((Get(tier, "List") as IList) is IList lst)
                        foreach (var c in lst) if (shardPools.Contains(Get(c, "resourcePool") as UnityEngine.Object)) hits++;
        }
        Debug.Log($"[Cards] Verify: remaining shard cost refs = {hits} (expected 0 after conversion)");
    }
}
#endif
