#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Luzart;

// Builds the faithful IO_Training MVVM prefabs (popups + item cells) with full View
// wiring, registers them in PopupVisualResolver, and builds a PopupOpener canvas prefab.
// Visuals are clean uGUI/TMP placeholders (item sprites are null in the data); the
// STRUCTURE + wiring mirrors IO_Training exactly.
public static class IOPortPrefabGenerator
{
    const string ROOT = "Assets/_Main/Data/IOShop";
    const string PF = ROOT + "/Prefabs";
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
    static void Set(object o, string name, object v)
    {
        var f = Field(o.GetType(), name);
        if (f == null) { Debug.LogError($"[IOPortPF] field {name} missing on {o.GetType().Name}"); return; }
        f.SetValue(o, v);
    }

    static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    static GameObject NewGO(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        return go;
    }
    static Image AddImage(GameObject go, Color c)
    {
        var im = go.AddComponent<Image>();
        im.color = c;
        return im;
    }
    static TMP_Text AddText(GameObject go, string s, int fontSize, TextAlignmentOptions align)
    {
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = s; t.fontSize = fontSize; t.alignment = align; t.color = Color.white;
        t.enableWordWrapping = true;
        return t;
    }
    static void AddClick(Button b, UnityAction call)
    {
        UnityEventTools.AddPersistentListener(b.onClick, call);
    }
    static GameObject SavePrefab(GameObject go, string path)
    {
        var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return saved;
    }

    static readonly Color PANEL = new Color(0.10f, 0.11f, 0.16f, 0.97f);
    static readonly Color CARD = new Color(0.18f, 0.20f, 0.28f, 1f);
    static readonly Color SLOT = new Color(0.22f, 0.24f, 0.32f, 1f);
    static readonly Color ACCENT = new Color(0.20f, 0.55f, 0.95f, 1f);
    static readonly Color GOOD = new Color(0.25f, 0.7f, 0.35f, 1f);

    [MenuItem("Tools/IOPort/Place Bootstrap In Scene")]
    public static void PlaceBootstrap()
    {
        var canvasPf = AssetDatabase.LoadAssetAtPath<GameObject>(PF + "/IOPortPopupCanvas.prefab");
        var existing = UnityEngine.Object.FindObjectOfType<IOPortBootstrap>(true);
        IOPortBootstrap boot = existing;
        if (boot == null)
        {
            var go = new GameObject("IOPortBootstrap");
            boot = go.AddComponent<IOPortBootstrap>();
            var loader = UnityEngine.Object.FindObjectOfType<DomainContentLoader>(true);
            if (loader != null) go.transform.SetParent(loader.transform.parent, false);
        }
        var so = new SerializedObject(boot);
        so.FindProperty("popupCanvasPrefab").objectReferenceValue = canvasPf;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(boot);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(boot.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[IOPortPF] Bootstrap placed in scene, canvas prefab assigned.");
    }

    [MenuItem("Tools/IOPort/Build Prefabs")]
    public static void Build()
    {
        EnsureDir(PF);

        var itemConfigViewPf = BuildItemConfigView();
        var objectViewPf = BuildObjectView(itemConfigViewPf);
        var costRowPf = BuildResourceCostSingleLine();
        var lvlViewPf = BuildItemViewWithLevel();
        var gridCellPf = BuildItemViewInventory(lvlViewPf);
        var shopCardPf = BuildItemShopUnlockView(objectViewPf);
        var slotPf = BuildSlotItemEquipmentView(lvlViewPf);
        var equipPopupPf = BuildPopupItemEquip(objectViewPf, costRowPf);
        var invPopupPf = BuildPopupItemInventory(slotPf, gridCellPf);
        var shopPopupPf = BuildPopupShop(shopCardPf);

        // assign cost row to the cost visual resolver
        var costResolver = AssetDatabase.LoadAssetAtPath<AssetCostVisualResolver_ResourcePool>(ROOT + "/Resources/CostVisual_ResourcePool.asset");
        if (costResolver != null)
        {
            var rcsl = costRowPf.GetComponent<ResourceCostSingleLine>();
            Set(costResolver, "resourceCostView_singleLine", rcsl);
            EditorUtility.SetDirty(costResolver);
        }

        // register popups in PopupVisualResolver
        var pvr = AssetDatabase.LoadAssetAtPath<PopupVisualResolver>(ROOT + "/System/PopupVisualResolver.asset");
        if (pvr != null)
        {
            var arr = new Popup[]
            {
                shopPopupPf.GetComponent<Popup>(),
                invPopupPf.GetComponent<Popup>(),
                equipPopupPf.GetComponent<Popup>(),
            };
            Set(pvr, "popupPrefabs", arr);
            EditorUtility.SetDirty(pvr);
        }

        BuildPopupCanvas();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[IOPortPF] Prefabs built + registered.");
    }

    static GameObject BuildItemConfigView()
    {
        var root = NewGO("ItemConfigView", null, new Vector2(120, 120));
        AddImage(root, new Color(0,0,0,0));
        var v = root.AddComponent<ItemConfigView>();
        var bg = NewGO("Bg", root.transform, new Vector2(120, 120));
        var bgIm = AddImage(bg, CARD);
        Stretch((RectTransform)bg.transform);
        var icon = NewGO("Icon", root.transform, new Vector2(96, 96));
        var iconIm = AddImage(icon, new Color(1,1,1,0.15f));
        var amt = NewGO("Amount", root.transform, new Vector2(120, 28));
        var amtT = AddText(amt, "", 20, TextAlignmentOptions.BottomRight);
        ((RectTransform)amt.transform).anchoredPosition = new Vector2(0, -46);
        Set(v, "_imIcon", iconIm); Set(v, "_imBg", bgIm); Set(v, "_txt", amtT);
        Set(v, "children", new ViewChilding[0]);
        return SavePrefab(root, PF + "/ItemConfigView.prefab");
    }

    static GameObject BuildObjectView(GameObject itemConfigViewPf)
    {
        var root = NewGO("ObjectView", null, new Vector2(120, 120));
        AddImage(root, new Color(0,0,0,0));
        var v = root.AddComponent<ObjectView>();
        Set(v, "container", root.transform);
        Set(v, "itemConfigView", itemConfigViewPf.GetComponent<ItemConfigView>());
        Set(v, "children", new ViewChilding[0]);
        return SavePrefab(root, PF + "/ObjectView.prefab");
    }

    static GameObject BuildResourceCostSingleLine()
    {
        var root = NewGO("ResourceCostRow", null, new Vector2(180, 30));
        var v = root.AddComponent<ResourceCostSingleLine>();
        var txt = AddText(root, "0/0", 18, TextAlignmentOptions.Left);
        Set(v, "txt", txt);
        Set(v, "children", new ViewChilding[0]);
        return SavePrefab(root, PF + "/ResourceCostRow.prefab");
    }

    static GameObject BuildItemViewWithLevel()
    {
        var root = NewGO("ItemViewWithLevel", null, new Vector2(110, 110));
        var v = root.AddComponent<ItemViewWithLevelInventory>();
        var bg = NewGO("Bg", root.transform, new Vector2(110, 110));
        var bgIm = AddImage(bg, SLOT); Stretch((RectTransform)bg.transform);
        var icon = NewGO("Icon", root.transform, new Vector2(84, 84));
        var iconIm = AddImage(icon, new Color(1,1,1,0.18f));
        var lvl = NewGO("Level", root.transform, new Vector2(110, 26));
        var lvlT = AddText(lvl, "Lv.0", 18, TextAlignmentOptions.BottomRight);
        ((RectTransform)lvl.transform).anchoredPosition = new Vector2(-4, 6);
        Set(v, "imIcon", iconIm); Set(v, "imBg", bgIm); Set(v, "txtLevel", lvlT);
        return SavePrefab(root, PF + "/ItemViewWithLevel.prefab");
    }

    static GameObject BuildItemViewInventory(GameObject lvlViewPf)
    {
        var root = NewGO("ItemViewInventory", null, new Vector2(120, 120));
        AddImage(root, new Color(1,1,1,0.02f));
        var btn = root.AddComponent<Button>();
        var v = root.AddComponent<ItemViewInventory>();
        var child = (GameObject)PrefabUtility.InstantiatePrefab(lvlViewPf);
        child.transform.SetParent(root.transform, false);
        Set(v, "_itemView", child.GetComponent<ItemViewWithLevelInventory>());
        AddClick(btn, v.OnClickItemView);
        return SavePrefab(root, PF + "/ItemViewInventory.prefab");
    }

    static GameObject BuildItemShopUnlockView(GameObject objectViewPf)
    {
        var root = NewGO("ShopCard", null, new Vector2(200, 260));
        AddImage(root, CARD);
        var btn = root.AddComponent<Button>();
        var v = root.AddComponent<ItemShopUnlockView>();
        var ov = (GameObject)PrefabUtility.InstantiatePrefab(objectViewPf);
        ov.transform.SetParent(root.transform, false);
        ((RectTransform)ov.transform).anchoredPosition = new Vector2(0, 50);
        var costParent = NewGO("Costs", root.transform, new Vector2(190, 70));
        ((RectTransform)costParent.transform).anchoredPosition = new Vector2(0, -90);
        var vlg = costParent.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
        Set(v, "objectView", ov.GetComponent<ObjectView>());
        Set(v, "parentSpawnCost", costParent.transform);
        Set(v, "children", new ViewChilding[0]);
        AddClick(btn, v.OnClickUnlock);
        return SavePrefab(root, PF + "/ShopCard.prefab");
    }

    static GameObject BuildSlotItemEquipmentView(GameObject lvlViewPf)
    {
        var root = NewGO("SlotItem", null, new Vector2(130, 130));
        AddImage(root, SLOT);
        var btn = root.AddComponent<Button>();
        var v = root.AddComponent<SlotItemEquipmentView>();
        var child = (GameObject)PrefabUtility.InstantiatePrefab(lvlViewPf);
        child.transform.SetParent(root.transform, false);
        Set(v, "itemView", child.GetComponent<ItemViewWithLevelInventory>());
        Set(v, "children", new ViewChilding[0]);
        AddClick(btn, v.OnClickItemView);
        return SavePrefab(root, PF + "/SlotItem.prefab");
    }

    // ---------- popups ----------
    static (GameObject panel, Button close) BuildPopupShell(string title)
    {
        var root = NewGO(title, null, new Vector2(1080, 1600));
        var rt = (RectTransform)root.transform; Stretch(rt);
        AddImage(root, PANEL);
        var header = NewGO("Title", root.transform, new Vector2(800, 70));
        AddText(header, title, 40, TextAlignmentOptions.Center);
        var ht = (RectTransform)header.transform; ht.anchorMin = new Vector2(0.5f,1); ht.anchorMax = new Vector2(0.5f,1); ht.anchoredPosition = new Vector2(0,-70);
        var closeGo = NewGO("CloseButton", root.transform, new Vector2(90, 90));
        AddImage(closeGo, new Color(0.8f,0.25f,0.25f,1f));
        var closeBtn = closeGo.AddComponent<Button>();
        AddText(NewGO("X", closeGo.transform, new Vector2(90,90)), "X", 44, TextAlignmentOptions.Center);
        var crt = (RectTransform)closeGo.transform; crt.anchorMin = new Vector2(1,1); crt.anchorMax = new Vector2(1,1); crt.anchoredPosition = new Vector2(-60,-60);
        return (root, closeBtn);
    }

    static GameObject BuildPopupItemEquip(GameObject objectViewPf, GameObject costRowPf)
    {
        var (root, close) = BuildPopupShell("Item");
        var popup = root.AddComponent<PopupItemEquip>();
        var view = root.AddComponent<PopupItemEquipView>();

        var ov = (GameObject)PrefabUtility.InstantiatePrefab(objectViewPf);
        ov.transform.SetParent(root.transform, false);
        ((RectTransform)ov.transform).anchoredPosition = new Vector2(0, 400);

        var name = NewGO("Name", root.transform, new Vector2(900, 60));
        var nameT = AddText(name, "Name", 40, TextAlignmentOptions.Center);
        ((RectTransform)name.transform).anchoredPosition = new Vector2(0, 250);
        var desc = NewGO("Desc", root.transform, new Vector2(900, 200));
        var descT = AddText(desc, "", 28, TextAlignmentOptions.Top);
        ((RectTransform)desc.transform).anchoredPosition = new Vector2(0, 60);

        var costParent = NewGO("UpgradeCosts", root.transform, new Vector2(600, 120));
        ((RectTransform)costParent.transform).anchoredPosition = new Vector2(0, -150);
        var vlg = costParent.AddComponent<VerticalLayoutGroup>(); vlg.childControlHeight = true; vlg.childForceExpandHeight = false;

        var equipBtn = MakeButton(root.transform, "Equip", ACCENT, new Vector2(-200, -380));
        var unequipBtn = MakeButton(root.transform, "Unequip", new Color(0.5f,0.5f,0.55f,1f), new Vector2(-200, -380));
        var upBtn = MakeButton(root.transform, "Upgrade", GOOD, new Vector2(200, -380));

        Set(view, "txtName", nameT); Set(view, "txtDescription", descT);
        Set(view, "objectView", ov.GetComponent<ObjectView>());
        Set(view, "btnEquip", equipBtn); Set(view, "btnUnEquip", unequipBtn); Set(view, "btnUpgrade", upBtn);
        Set(view, "parentSpawn", costParent.transform);
        Set(view, "children", new ViewChilding[0]);
        AddClick(equipBtn, view.OnClickEquip);
        AddClick(unequipBtn, view.OnClickUnEquip);
        AddClick(upBtn, view.OnClickUpgrade);

        Set(popup, "mainView", view);
        Set(popup, "closeButton", close);
        return SavePrefab(root, PF + "/PopupItemEquip.prefab");
    }

    static GameObject BuildPopupItemInventory(GameObject slotPf, GameObject gridCellPf)
    {
        var (root, close) = BuildPopupShell("Equipment");
        var popup = root.AddComponent<PopupItemInventory>();
        var view = root.AddComponent<PopupItemInventoryView>();

        // slots row
        var slotsContainer = NewGO("Slots", root.transform, new Vector2(900, 150));
        ((RectTransform)slotsContainer.transform).anchoredPosition = new Vector2(0, 480);
        var hlg = slotsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 10; hlg.childControlWidth = false; hlg.childControlHeight = false;
        var slotsSpawnerGo = NewGO("SlotsSpawner", slotsContainer.transform, new Vector2(10,10));
        var slotsSpawner = slotsSpawnerGo.AddComponent<UIItemSpawnerGeneric>();
        Set(slotsSpawner, "parent", slotsContainer.transform);
        Set(slotsSpawner, "viewPrefab", slotPf.GetComponent<View>());
        Set(slotsSpawner, "children", new ViewChilding[0]);

        // owned grid
        var gridScroll = NewGO("Grid", root.transform, new Vector2(960, 700));
        ((RectTransform)gridScroll.transform).anchoredPosition = new Vector2(0, -200);
        var grid = gridScroll.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(130,130); grid.spacing = new Vector2(12,12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 6;
        grid.childAlignment = TextAnchor.UpperCenter;
        var scrollData = gridScroll.AddComponent<ScrollViewItemInventoryData>();
        Set(scrollData, "parentSpawn", gridScroll.transform);
        Set(scrollData, "itemPrefabs", gridCellPf.GetComponent<ItemViewInventory>());
        Set(scrollData, "children", new ViewChilding[0]);

        // wire ViewChilding children of the main view
        Set(view, "children", new ViewChilding[]
        {
            MakeChilding("AllSlotsObj", slotsSpawner),
            MakeChilding("Inventory", scrollData),
        });

        Set(popup, "mainView", view);
        Set(popup, "closeButton", close);
        return SavePrefab(root, PF + "/PopupItemInventory.prefab");
    }

    static GameObject BuildPopupShop(GameObject shopCardPf)
    {
        var (root, close) = BuildPopupShell("Shop");
        var popup = root.AddComponent<PopupShop>();
        var view = root.AddComponent<PopupShopView>();

        var cardsContainer = NewGO("Cards", root.transform, new Vector2(980, 1200));
        ((RectTransform)cardsContainer.transform).anchoredPosition = new Vector2(0, -100);
        var grid = cardsContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200,260); grid.spacing = new Vector2(16,16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        var unlockedViewGo = NewGO("ShopUnlocked", root.transform, new Vector2(10,10));
        var unlockedView = unlockedViewGo.AddComponent<ShopPopupUnlockedView>();
        var spawnerGo = NewGO("CardSpawner", cardsContainer.transform, new Vector2(10,10));
        var spawner = spawnerGo.AddComponent<UIItemSpawnerGeneric>();
        Set(spawner, "parent", cardsContainer.transform);
        Set(spawner, "viewPrefab", shopCardPf.GetComponent<View>());
        Set(spawner, "children", new ViewChilding[0]);
        Set(unlockedView, "uIItemSpawnerGeneric", spawner);
        Set(unlockedView, "children", new ViewChilding[0]);

        Set(view, "children", new ViewChilding[] { MakeChilding("InventoryItemData", unlockedView) });
        Set(popup, "mainView", view);
        Set(popup, "closeButton", close);
        return SavePrefab(root, PF + "/PopupShop.prefab");
    }

    static void BuildPopupCanvas()
    {
        var root = new GameObject("IOPortPopupCanvas", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        root.AddComponent<GraphicRaycaster>();
        var openerGo = NewGO("PopupOpener", root.transform, new Vector2(10,10));
        var opener = openerGo.AddComponent<PopupOpener>();
        Set(opener, "popupsParent", root.transform);
        Set(opener, "children", new ViewChilding[0]);
        var saved = PrefabUtility.SaveAsPrefabAsset(root, PF + "/IOPortPopupCanvas.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    // ---------- helpers ----------
    static Button MakeButton(Transform parent, string label, Color c, Vector2 pos)
    {
        var go = NewGO(label + "Btn", parent, new Vector2(280, 90));
        AddImage(go, c);
        var b = go.AddComponent<Button>();
        AddText(NewGO("L", go.transform, new Vector2(280,90)), label, 32, TextAlignmentOptions.Center);
        ((RectTransform)go.transform).anchoredPosition = pos;
        return b;
    }

    static ViewChilding MakeChilding(string path, View view)
    {
        var vc = new ViewChilding();
        Set(vc, "path", path);
        Set(vc, "view", view);
        return vc;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
#endif
