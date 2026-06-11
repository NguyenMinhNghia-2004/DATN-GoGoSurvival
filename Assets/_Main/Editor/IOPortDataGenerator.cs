#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Luzart;

// One-off generator that wires the existing Luzart ItemConfig data into a faithful
// IO_Training shop/equipment data graph: ResourcePools (Gold + per-rarity Shards),
// per-item AssetUnlockable + AssetLevel sub-assets, 6 AssetEquipmentSlots,
// ItemConfigsOwned, RaritySpriteResolver, InventoryItemData, Data_Shop,
// PopupService, PopupVisualResolver — then registers them in the scene's
// DomainContentLoader. Idempotent: re-running updates in place.
public static class IOPortDataGenerator
{
    const string ROOT = "Assets/_Main/Data/IOShop";
    const string ITEMS_DIR = "Assets/_Main/Data/Items";

    static readonly BindingFlags FF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // ---------- reflection helpers ----------
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
        if (f == null) { Debug.LogError($"[IOPort] field {name} not found on {obj.GetType().Name}"); return; }
        f.SetValue(obj, value);
    }
    static object Get(object obj, string name)
    {
        var f = Field(obj.GetType(), name);
        return f?.GetValue(obj);
    }

    static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        var leaf = System.IO.Path.GetFileName(path);
        EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static T LoadOrCreate<T>(string assetPath, Action<T> init = null) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (existing != null) return existing;
        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, assetPath);
        init?.Invoke(so);
        EditorUtility.SetDirty(so);
        return so;
    }

    // ---------- cost authoring ----------
    static SerializableCostCreator_CommonlyUsed MakeCost(ResourcePool pool, double amount)
    {
        var c = new SerializableCostCreator_CommonlyUsed();
        Set(c, "mode", (Modes)1); // Modes.Resource
        Set(c, "resourcePool", pool);
        var np = new NumberPicker(NumberMode.Constant, amount, null);
        Set(c, "resourceAmount", np);
        return c;
    }

    static void SetId(ScriptableObject so, string id)
    {
        // content uses _id; service uses id
        if (Field(so.GetType(), "_id") != null) Set(so, "_id", id);
        else if (Field(so.GetType(), "id") != null) Set(so, "id", id);
    }

    // shared currency assets
    static ResourcePool _gold;
    static ResourcePool[] _shards; // index by ERarity 0..2
    static AssetCostVisualResolver_ResourcePool _costResolver;
    static ResourceDefinition[] _shardDefs;
    static Data_WinReward _winReward;

    // Register the modifier-pipeline infra SOs so equipped items can AddFactor without NRE.
    [MenuItem("Tools/IOPort/Register Modifier Infra")]
    public static void RegisterModifierInfra()
    {
        var loader = UnityEngine.Object.FindObjectOfType<DomainContentLoader>(true);
        if (loader == null) { Debug.LogError("[IOPort] No DomainContentLoader in open scene."); return; }

        var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Main/Data/ModifierAndInGame" });
        var so = new SerializedObject(loader);
        var contentsProp = so.FindProperty("contents");
        var existing = new HashSet<UnityEngine.Object>();
        for (int i = 0; i < contentsProp.arraySize; i++)
            existing.Add(contentsProp.GetArrayElementAtIndex(i).objectReferenceValue);

        int added = 0;
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var asc = AssetDatabase.LoadAssetAtPath<AbstractScriptableContent>(p);
            if (asc == null || existing.Contains(asc)) continue;
            contentsProp.arraySize++;
            contentsProp.GetArrayElementAtIndex(contentsProp.arraySize - 1).objectReferenceValue = asc;
            existing.Add(asc);
            added++;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(loader);
        EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[IOPort] Registered {added} modifier-infra SOs into DomainContentLoader.");
    }

    [MenuItem("Tools/IOPort/Generate Shop+Equip Data")]
    public static void Generate()
    {
        EnsureDir(ROOT);
        EnsureDir(ROOT + "/Resources");
        EnsureDir(ROOT + "/Slots");
        EnsureDir(ROOT + "/System");

        // 1) cost visual resolver (prefab assigned later in Slice 4)
        _costResolver = LoadOrCreate<AssetCostVisualResolver_ResourcePool>(ROOT + "/Resources/CostVisual_ResourcePool.asset");
        SetId(_costResolver, "io_costvisual");

        // 2) currency: Gold + 3 shard pools, each with a definition
        _gold = MakeResource("Gold", ETypeResourceDefinition.None, "io_gold");
        string[] rarityNames = { "Rare", "Epic", "Legend" };
        _shards = new ResourcePool[3];
        _shardDefs = new ResourceDefinition[3];
        for (int r = 0; r < 3; r++)
        {
            _shards[r] = MakeResource("Shard_" + rarityNames[r], ETypeResourceDefinition.Shard, "io_shard_" + r, "Shard " + rarityNames[r]);
            _shardDefs[r] = (ResourceDefinition)Get(_shards[r], "_resourceDefinition");
        }
        AssetDatabase.SaveAssets();

        // 3) author unlockable + level on every ItemConfig
        var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { ITEMS_DIR });
        var items = new List<ItemConfig>();
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var ic = AssetDatabase.LoadAssetAtPath<ItemConfig>(p);
            if (ic != null) items.Add(ic);
        }
        Debug.Log($"[IOPort] Found {items.Count} ItemConfig assets");

        int processed = 0;
        foreach (var ic in items)
        {
            AuthorItem(ic);
            processed++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[IOPort] Authored unlockable+level on {processed} items");

        // 4) 6 equipment slots
        var slots = new List<AssetEquipmentSlot>();
        string[] slotNames = { "Weapon", "Armor", "Necklace", "Belt", "Gloves", "Shoes" };
        for (int i = 0; i < 6; i++)
        {
            var slot = LoadOrCreate<AssetEquipmentSlot>(ROOT + "/Slots/Slot_" + slotNames[i] + ".asset");
            Set(slot, "eTypeItem", (ETypeItem)i);
            SetId(slot, "io_slot_" + i);
            EditorUtility.SetDirty(slot);
            slots.Add(slot);
        }

        // 5) ItemConfigsOwned
        var owned = LoadOrCreate<ItemConfigsOwned>(ROOT + "/System/ItemConfigsOwned.asset");
        SetId(owned, "io_owned");
        EditorUtility.SetDirty(owned);

        // 6) RaritySpriteResolver (sprites left null -> views fall back gracefully)
        var rarityResolver = LoadOrCreate<RaritySpriteResolver>(ROOT + "/System/RaritySpriteResolver.asset");
        SetId(rarityResolver, "io_rarity");
        EditorUtility.SetDirty(rarityResolver);

        // 7) InventoryItemData
        var inv = LoadOrCreate<InventoryItemData>(ROOT + "/System/InventoryItemData.asset");
        SetId(inv, "io_inventory");
        Set(inv, "_assetEquipmentSlots", slots);
        Set(inv, "_frameStatInventoryVM", new List<FrameStatInventoryVM>());
        Set(inv, "itemUpgradeCostVisualResolver", _costResolver);
        EditorUtility.SetDirty(inv);

        // 8) Data_Shop
        var shop = LoadOrCreate<Data_Shop>(ROOT + "/System/Data_Shop.asset");
        SetId(shop, "io_shop");
        Set(shop, "inventoryItemData", inv);
        Set(shop, "chests", new List<Chest>());
        // 8a) "buy shards with gold" offers — one per rarity (Rare/Epic/Legend).
        int[] shardPrices = { 100, 250, 500 };
        int[] shardAmounts = { 5, 3, 2 };
        var offers = new List<ShopShardOffer>();
        for (int r = 0; r < 3; r++)
            offers.Add(new ShopShardOffer(_shards[r], _gold, (ERarity)r, shardPrices[r], shardAmounts[r]));
        Set(shop, "shardOffers", offers);
        EditorUtility.SetDirty(shop);

        // 8b) win reward content: on win, grant a random card (default 15%) or random shards.
        _winReward = LoadOrCreate<Data_WinReward>(ROOT + "/System/Data_WinReward.asset");
        SetId(_winReward, "io_winreward");
        Set(_winReward, "shardPools", new List<ResourcePool> { _shards[0], _shards[1], _shards[2] });
        Set(_winReward, "inventoryItemData", inv);
        EditorUtility.SetDirty(_winReward);

        // 9) PopupService + PopupVisualResolver
        var popupService = LoadOrCreate<PopupService>(ROOT + "/System/PopupService.asset");
        SetId(popupService, "io_popupservice");
        EditorUtility.SetDirty(popupService);

        var popupResolver = LoadOrCreate<PopupVisualResolver>(ROOT + "/System/PopupVisualResolver.asset");
        SetId(popupResolver, "io_popupresolver");
        EditorUtility.SetDirty(popupResolver);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 10) register into the scene's DomainContentLoader
        RegisterInScene(items, slots, owned, rarityResolver, inv, shop, popupService, popupResolver);

        Debug.Log("[IOPort] Generation complete.");
    }

    static ResourcePool MakeResource(string name, ETypeResourceDefinition type, string id, string displayName = null)
    {
        var def = LoadOrCreate<ResourceDefinition>(ROOT + "/Resources/ResourceDefinition_" + name + ".asset");
        SetId(def, id + "_def");
        Set(def, "_eType", type);
        Set(def, "_displayName", displayName ?? name);
        Set(def, "_assetCostVisualResolver", _costResolver);
        EditorUtility.SetDirty(def);

        var pool = LoadOrCreate<ResourcePool>(ROOT + "/Resources/ResourcePool_" + name + ".asset");
        SetId(pool, id);
        Set(pool, "_resourceDefinition", def);
        EditorUtility.SetDirty(pool);
        return pool;
    }

    static void AuthorItem(ItemConfig ic)
    {
        string path = AssetDatabase.GetAssetPath(ic);
        int rarity = RarityForItem(path);
        Set(ic, "_eRarity", (ERarity)rarity);

        // number of upgrade levels
        var upgradeList = Get(ic, "upgradeItemConfigs") as System.Collections.IList;
        int levels = (upgradeList != null && upgradeList.Count > 0) ? upgradeList.Count : 10;

        // load existing sub-assets if present, else create
        var subs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        AssetUnlockable unlock = subs.OfType<AssetUnlockable>().FirstOrDefault();
        AssetLevel level = subs.OfType<AssetLevel>().FirstOrDefault();

        if (unlock == null)
        {
            unlock = ScriptableObject.CreateInstance<AssetUnlockable>();
            unlock.name = "Unlock_" + ic.name;
            AssetDatabase.AddObjectToAsset(unlock, ic);
        }
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<AssetLevel>();
            level.name = "Level_" + ic.name;
            AssetDatabase.AddObjectToAsset(level, ic);
        }

        SetId(unlock, ic.name + "_unlock");
        SetId(level, ic.name + "_level");

        // unlock costs: gold + shard scaled by rarity
        double goldUnlock = 50 * (rarity + 1);
        double shardUnlock = 10 + 5 * rarity;
        var unlockCosts = new List<SerializableCostCreator_CommonlyUsed>
        {
            MakeCost(_gold, goldUnlock),
            MakeCost(_shards[rarity], shardUnlock),
        };
        Set(unlock, "unlockCosts", unlockCosts);

        // level-up costs per level
        Set(level, "levelIndex", 0);
        Set(level, "maxLevelIndex", levels);
        var levelUpCosts = new List<ListSerializableCostCreator_CommonlyUsed>();
        for (int i = 0; i < levels; i++)
        {
            var entry = new ListSerializableCostCreator_CommonlyUsed();
            entry.List = new List<SerializableCostCreator_CommonlyUsed>
            {
                MakeCost(_gold, 5 + i),
                MakeCost(_shards[rarity], i + 1),
            };
            levelUpCosts.Add(entry);
        }
        Set(level, "levelUpCosts", levelUpCosts);

        Set(ic, "unlockable", unlock);
        Set(ic, "assetLevel", level);

        EditorUtility.SetDirty(unlock);
        EditorUtility.SetDirty(level);
        EditorUtility.SetDirty(ic);
    }

    static int RarityForItem(string path)
    {
        if (path.Contains("/Epic/")) return 2;       // Legend
        if (path.Contains("/Better/")) return 1;     // Epic
        if (path.Contains("/Excellent/")) return 1;  // Epic
        return 0;                                     // Rare (Normal/Good)
    }

    static void RegisterInScene(List<ItemConfig> items, List<AssetEquipmentSlot> slots,
        ItemConfigsOwned owned, RaritySpriteResolver rarity, InventoryItemData inv, Data_Shop shop,
        PopupService popupService, PopupVisualResolver popupResolver)
    {
        var loader = UnityEngine.Object.FindObjectOfType<DomainContentLoader>(true);
        if (loader == null)
        {
            Debug.LogError("[IOPort] No DomainContentLoader in open scene — open GamePlay.unity first.");
            return;
        }
        var so = new SerializedObject(loader);
        var contentsProp = so.FindProperty("contents");
        var servicesProp = so.FindProperty("services");

        var existing = new HashSet<UnityEngine.Object>();
        for (int i = 0; i < contentsProp.arraySize; i++)
            existing.Add(contentsProp.GetArrayElementAtIndex(i).objectReferenceValue);

        void AddContent(UnityEngine.Object o)
        {
            if (o == null || existing.Contains(o)) return;
            contentsProp.arraySize++;
            contentsProp.GetArrayElementAtIndex(contentsProp.arraySize - 1).objectReferenceValue = o;
            existing.Add(o);
        }

        // ItemConfigs first (so ItemConfigsOwned.DoInitialize finds them via GetAll<ItemConfig>),
        // but registration order vs Initialize order: all are added before InitializeAll. OK.
        foreach (var ic in items)
        {
            AddContent(ic);
            // The AssetUnlockable + AssetLevel sub-assets are separate ScriptableObjects that
            // must be Domain-registered so their DoInitialize builds the runtime cost lists.
            var subs = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(ic));
            foreach (var sub in subs)
            {
                if (sub is AssetUnlockable || sub is AssetLevel)
                    AddContent(sub);
            }
        }
        AddContent(rarity);
        // currency pools + their definitions + cost visual resolver
        AddContent(_costResolver);
        AddContent(_gold);
        AddContent((ResourceDefinition)Get(_gold, "_resourceDefinition"));
        if (_shards != null)
            foreach (var sp in _shards)
            {
                AddContent(sp);
                AddContent((ResourceDefinition)Get(sp, "_resourceDefinition"));
            }
        foreach (var s in slots) AddContent(s);
        AddContent(owned);
        AddContent(inv);
        AddContent(shop);
        AddContent(_winReward);
        AddContent(popupResolver);

        // services
        var existingSvc = new HashSet<UnityEngine.Object>();
        for (int i = 0; i < servicesProp.arraySize; i++)
            existingSvc.Add(servicesProp.GetArrayElementAtIndex(i).objectReferenceValue);
        if (!existingSvc.Contains(popupService))
        {
            servicesProp.arraySize++;
            servicesProp.GetArrayElementAtIndex(servicesProp.arraySize - 1).objectReferenceValue = popupService;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(loader);
        EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[IOPort] Registered {items.Count} items + {slots.Count} slots + config + PopupService into DomainContentLoader.");
    }
}
#endif
