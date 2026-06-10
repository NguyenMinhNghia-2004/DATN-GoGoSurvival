#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
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

        AssignRarityFrames();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[IOPortPF] Prefabs built + registered.");
    }

    // Rarity frames come from the game's equip-quality sheet (13.png, guid 1263f101...).
    // Map our 3 rarities onto ascending EquipUI_Quality_* frames so imBg shows a tier color.
    const string RARITY_SHEET_GUID = "1263f101bd3658846bfa6ca84e97543c";
    static void AssignRarityFrames()
    {
        var resolver = AssetDatabase.LoadAssetAtPath<RaritySpriteResolver>(ROOT + "/System/RaritySpriteResolver.asset");
        if (resolver == null) { Debug.LogError("[IOPortPF] RaritySpriteResolver asset missing"); return; }
        var sheetPath = AssetDatabase.GUIDToAssetPath(RARITY_SHEET_GUID);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToList();
        Sprite Pick(string n) => sprites.FirstOrDefault(s => s.name == n);
        var list = new List<SpriteRarity>
        {
            new SpriteRarity { eRarity = ERarity.Rare,   sprite = Pick("EquipUI_Quality_2") },
            new SpriteRarity { eRarity = ERarity.Epic,   sprite = Pick("EquipUI_Quality_3") },
            new SpriteRarity { eRarity = ERarity.Legend, sprite = Pick("EquipUI_Quality_4") },
        };
        Set(resolver, "_listSpriteRarity", list);
        EditorUtility.SetDirty(resolver);
        Debug.Log($"[IOPortPF] Rarity frames assigned: Rare={list[0].sprite?.name}, Epic={list[1].sprite?.name}, Legend={list[2].sprite?.name}");
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

    const string WEAPONICONS_PATH = "Background/Scrolling/Viewport/Content/HalfPlayerShow/DownFill/Bg/WeaponIcons";

    static GameObject BuildItemViewInventory(GameObject lvlViewPf)
    {
        // Clone an old owned-grid cell ("Cnte": Image + Button + Icon/LogoNot/Text).
        var root = CloneChildByIndex(SV_EQUIP, WEAPONICONS_PATH, 0);
        if (root == null) { root = NewGO("ItemViewInventory", null, new Vector2(120, 120)); AddImage(root, new Color(1, 1, 1, 0.02f)); }
        root.name = "ItemViewInventory";
        var btn = root.GetComponent<Button>() ?? root.AddComponent<Button>();
        var v = root.AddComponent<ItemViewInventory>();
        var iv = root.AddComponent<ItemViewWithLevelInventory>();

        var icon = FindDeep(root.transform, "Icon");
        var logoNot = FindDeep(root.transform, "LogoNot");
        var legacyLvl = FindDeep(root.transform, "Text (Legacy)");
        var frameImg = root.GetComponent<Image>();
        Set(iv, "imIcon", icon != null ? icon.GetComponent<Image>() : null);
        Set(iv, "imBg", frameImg);
        Set(iv, "txtLevel", legacyLvl != null ? AddTmpAtLegacy(legacyLvl) : null);
        // a cell always renders an owned item — hide the empty placeholder + ad badge
        if (logoNot != null) logoNot.gameObject.SetActive(false);
        DisableByPrefix(root.transform, "Reward");

        Set(v, "_itemView", iv);
        AddClick(btn, v.OnClickItemView);
        return SavePrefab(root, PF + "/ItemViewInventory.prefab");
    }

    const string SV_SHOP = "Assets/_Main/Perfabes/UI/SV_Shop.prefab";
    const string SV_EQUIP = "Assets/_Main/Perfabes/UI/SV_Equipement.prefab";

    static GameObject BuildItemShopUnlockView(GameObject objectViewPf)
    {
        // Clone the old "Weapon Design" shop card (bg Image + Button + Name/Icon/Coins/Done).
        var root = CloneSubtree(SV_SHOP, "Container/Items/Viewport/Content/Daily Shop/Weapon Design");
        if (root == null) { root = NewGO("ShopCard", null, new Vector2(200, 260)); AddImage(root, CARD); }
        root.name = "ShopCard";
        var btn = root.GetComponent<Button>() ?? root.AddComponent<Button>();
        var v = root.AddComponent<ItemShopUnlockView>();

        // Item icon + rarity frame render through ObjectView spawned into the card's Icon slot.
        var iconNode = FindDeep(root.transform, "Icon");
        if (iconNode != null)
            foreach (Transform ch in iconNode) ch.gameObject.SetActive(false);
        var ov = (GameObject)PrefabUtility.InstantiatePrefab(objectViewPf);
        ov.transform.SetParent(iconNode != null ? iconNode : root.transform, false);
        Stretch((RectTransform)ov.transform);

        // Cost rows spawn in a VLG placed over the old static price ("Coins"); blank static labels.
        var coins = FindDeep(root.transform, "Coins");
        var costGo = new GameObject("Costs", typeof(RectTransform));
        costGo.transform.SetParent(root.transform, false);
        var crt = (RectTransform)costGo.transform;
        if (coins != null)
        {
            var cr = (RectTransform)coins;
            crt.anchorMin = cr.anchorMin; crt.anchorMax = cr.anchorMax;
            crt.pivot = cr.pivot; crt.anchoredPosition = cr.anchoredPosition; crt.sizeDelta = cr.sizeDelta;
            coins.gameObject.SetActive(false);
        }
        var vlg = costGo.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false; vlg.childAlignment = TextAnchor.MiddleCenter;
        var nameNode = FindDeep(root.transform, "Name");
        if (nameNode != null) { var nt = nameNode.GetComponent<Text>(); if (nt != null) nt.text = ""; }
        var done = FindDeep(root.transform, "Done");
        if (done != null) done.gameObject.SetActive(false);

        Set(v, "objectView", ov.GetComponent<ObjectView>());
        Set(v, "parentSpawnCost", costGo.transform);
        Set(v, "children", new ViewChilding[0]);
        AddClick(btn, v.OnClickUnlock);
        return SavePrefab(root, PF + "/ShopCard.prefab");
    }

    const string SLOT_PATH = "Background/Scrolling/Viewport/Content/HalfPlayerShow/Left/Firs";

    static GameObject BuildSlotItemEquipmentView(GameObject lvlViewPf)
    {
        // Clone an old equipment slot frame (Image + Button + Icon/LogoNot/Text/Reward).
        var root = CloneSubtree(SV_EQUIP, SLOT_PATH);
        if (root == null) { root = NewGO("SlotItem", null, new Vector2(130, 130)); AddImage(root, SLOT); }
        root.name = "SlotItem";
        var btn = root.GetComponent<Button>() ?? root.AddComponent<Button>();
        var v = root.AddComponent<SlotItemEquipmentView>();
        var iv = root.AddComponent<ItemViewWithLevelInventory>();

        var icon = FindDeep(root.transform, "Icon");
        var logoNot = FindDeep(root.transform, "LogoNot");
        var legacyLvl = FindDeep(root.transform, "Text (Legacy)");
        var frameImg = root.GetComponent<Image>();
        Set(iv, "imIcon", icon != null ? icon.GetComponent<Image>() : null);
        Set(iv, "imBg", frameImg);
        Set(iv, "txtLevel", legacyLvl != null ? AddTmpAtLegacy(legacyLvl) : null);

        // bsState: 0=equipped(show Icon), 1=empty(show LogoNot), 2=locked(reuse LogoNot)
        var bsState = root.AddComponent<Luzart.NewBase.SelectSwitchGameObject>();
        bsState.obSelects = new[]
        {
            Grp(icon != null ? icon.gameObject : null),
            Grp(logoNot != null ? logoNot.gameObject : null),
            Grp(logoNot != null ? logoNot.gameObject : null),
        };
        DisableByPrefix(root.transform, "Reward");

        Set(v, "itemView", iv);
        Set(v, "bsState", bsState);
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
        // Full-screen overlay with dim backdrop; clone the old HalfPlayerShow body.
        var root = NewGO("Equipment", null, new Vector2(1080, 1920));
        Stretch((RectTransform)root.transform);
        AddImage(root, new Color(0, 0, 0, 0.55f));
        var popup = root.AddComponent<PopupItemInventory>();
        var view = root.AddComponent<PopupItemInventoryView>();

        Transform leftCol = null, rightCol = null, gridContent = null;
        var hp = CloneSubtree(SV_EQUIP, "Background/Scrolling/Viewport/Content/HalfPlayerShow");
        if (hp != null)
        {
            hp.transform.SetParent(root.transform, false);
            Stretch((RectTransform)hp.transform);
            leftCol = hp.transform.Find("Left");
            rightCol = hp.transform.Find("Right");
            foreach (var col in new[] { leftCol, rightCol })
            {
                if (col == null) continue;
                for (int i = col.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(col.GetChild(i).gameObject);
                var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.childControlWidth = false; vlg.childControlHeight = false;
                vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
                vlg.childAlignment = TextAnchor.MiddleCenter; vlg.spacing = 30;
            }
            // Turn WeaponIcons into a scroll viewport hosting a responsive grid content.
            var weaponIcons = hp.transform.Find("DownFill/Bg/WeaponIcons");
            if (weaponIcons != null)
            {
                for (int i = weaponIcons.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(weaponIcons.GetChild(i).gameObject);
                if (weaponIcons.GetComponent<Mask>() == null) weaponIcons.gameObject.AddComponent<Mask>();
                var sr = weaponIcons.gameObject.AddComponent<ScrollRect>();
                var contentGo = new GameObject("GridContent", typeof(RectTransform));
                contentGo.transform.SetParent(weaponIcons, false);
                var crt = (RectTransform)contentGo.transform;
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1); crt.anchoredPosition = Vector2.zero; crt.sizeDelta = Vector2.zero;
                var g = contentGo.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(150, 150); g.spacing = new Vector2(12, 12);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 4;
                g.childAlignment = TextAnchor.UpperCenter; g.padding = new RectOffset(12, 12, 12, 12);
                var csf = contentGo.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                sr.content = crt; sr.viewport = (RectTransform)weaponIcons;
                sr.horizontal = false; sr.vertical = true;
                gridContent = contentGo.transform;
            }
        }
        // Fallbacks if the clone failed.
        if (leftCol == null) { var g = NewGO("LeftCol", root.transform, new Vector2(200, 700)); ((RectTransform)g.transform).anchoredPosition = new Vector2(-380, 200); g.AddComponent<VerticalLayoutGroup>(); leftCol = g.transform; }
        if (rightCol == null) { var g = NewGO("RightCol", root.transform, new Vector2(200, 700)); ((RectTransform)g.transform).anchoredPosition = new Vector2(380, 200); g.AddComponent<VerticalLayoutGroup>(); rightCol = g.transform; }
        if (gridContent == null) { var g = NewGO("OwnedGridContent", root.transform, new Vector2(960, 700)); ((RectTransform)g.transform).anchoredPosition = new Vector2(0, -300); var gl = g.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(150, 150); gl.spacing = new Vector2(12, 12); gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 6; gridContent = g.transform; }

        // Generated close X.
        var closeGo = NewGO("CloseButton", root.transform, new Vector2(90, 90));
        AddImage(closeGo, new Color(0.8f, 0.25f, 0.25f, 1f));
        var close = closeGo.AddComponent<Button>();
        AddText(NewGO("X", closeGo.transform, new Vector2(90, 90)), "X", 44, TextAlignmentOptions.Center);
        var ccrt = (RectTransform)closeGo.transform; ccrt.anchorMin = new Vector2(1, 1); ccrt.anchorMax = new Vector2(1, 1); ccrt.anchoredPosition = new Vector2(-60, -60);

        // Two slot spawners (dual column) + the owned-grid scroll data.
        var leftSpawnerGo = NewGO("SlotsLeftSpawner", root.transform, new Vector2(10, 10));
        var leftSpawner = leftSpawnerGo.AddComponent<UIItemSpawnerGeneric>();
        Set(leftSpawner, "parent", leftCol); Set(leftSpawner, "viewPrefab", slotPf.GetComponent<View>()); Set(leftSpawner, "children", new ViewChilding[0]);
        var rightSpawnerGo = NewGO("SlotsRightSpawner", root.transform, new Vector2(10, 10));
        var rightSpawner = rightSpawnerGo.AddComponent<UIItemSpawnerGeneric>();
        Set(rightSpawner, "parent", rightCol); Set(rightSpawner, "viewPrefab", slotPf.GetComponent<View>()); Set(rightSpawner, "children", new ViewChilding[0]);
        var gridGo = NewGO("OwnedGrid", root.transform, new Vector2(10, 10));
        var scrollData = gridGo.AddComponent<ScrollViewItemInventoryData>();
        Set(scrollData, "parentSpawn", gridContent); Set(scrollData, "itemPrefabs", gridCellPf.GetComponent<ItemViewInventory>()); Set(scrollData, "children", new ViewChilding[0]);

        Set(view, "children", new ViewChilding[]
        {
            MakeChilding("SlotsLeftObj", leftSpawner),
            MakeChilding("SlotsRightObj", rightSpawner),
            MakeChilding("Inventory", scrollData),
        });

        Set(popup, "mainView", view);
        Set(popup, "closeButton", close);
        return SavePrefab(root, PF + "/PopupItemInventory.prefab");
    }

    static GameObject BuildPopupShop(GameObject shopCardPf)
    {
        // Full-screen overlay root with dim backdrop.
        var root = NewGO("Shop", null, new Vector2(1080, 1920));
        Stretch((RectTransform)root.transform);
        AddImage(root, new Color(0, 0, 0, 0.55f));
        var popup = root.AddComponent<PopupShop>();
        var view = root.AddComponent<PopupShopView>();

        // Clone the old shop scroll frame (Items/Viewport(Mask)/Content + Scrollbar) for the look.
        var container = CloneSubtree(SV_SHOP, "Container");
        Transform gridParent;
        if (container != null)
        {
            container.transform.SetParent(root.transform, false);
            Stretch((RectTransform)container.transform);
            var content = FindDeep(container.transform, "Content");
            var host = content != null ? content : container.transform;
            // drop the hand-placed static sections; spawn into a fresh responsive grid
            for (int i = host.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(host.GetChild(i).gameObject);
            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(host, false);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(325, 443); grid.spacing = new Vector2(16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter; grid.padding = new RectOffset(10, 10, 10, 10);
            var csf = gridGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            gridParent = gridGo.transform;
        }
        else
        {
            var gridGo = NewGO("Grid", root.transform, new Vector2(980, 1200));
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(325, 443); grid.spacing = new Vector2(16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            gridParent = gridGo.transform;
        }

        // generated close X (old screen had no close affordance)
        var closeGo = NewGO("CloseButton", root.transform, new Vector2(90, 90));
        AddImage(closeGo, new Color(0.8f, 0.25f, 0.25f, 1f));
        var close = closeGo.AddComponent<Button>();
        AddText(NewGO("X", closeGo.transform, new Vector2(90, 90)), "X", 44, TextAlignmentOptions.Center);
        var crt = (RectTransform)closeGo.transform; crt.anchorMin = new Vector2(1, 1); crt.anchorMax = new Vector2(1, 1); crt.anchoredPosition = new Vector2(-60, -60);

        var unlockedViewGo = NewGO("ShopUnlocked", root.transform, new Vector2(10, 10));
        var unlockedView = unlockedViewGo.AddComponent<ShopPopupUnlockedView>();
        var spawnerGo = NewGO("CardSpawner", root.transform, new Vector2(10, 10));
        var spawner = spawnerGo.AddComponent<UIItemSpawnerGeneric>();
        Set(spawner, "parent", gridParent);
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

    // ---------- old-visual cloning ----------

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

    // Like CloneSubtree but copies the child at `index` of a parent (robust for nodes whose
    // names carry trailing spaces, e.g. the "Cnte " grid cells).
    static GameObject CloneChildByIndex(string sourcePrefabPath, string parentPath, int index)
    {
        var contents = PrefabUtility.LoadPrefabContents(sourcePrefabPath);
        try
        {
            var parent = contents.transform.Find(parentPath);
            if (parent == null || index < 0 || index >= parent.childCount)
            {
                Debug.LogError($"[IOPortPF] child {index} of '{parentPath}' not found in {sourcePrefabPath}");
                return null;
            }
            var node = parent.GetChild(index);
            var clone = UnityEngine.Object.Instantiate(node.gameObject);
            clone.name = node.gameObject.name.Trim();
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

    // Build a single-object SelectSwitchGameObject group (or empty group when go is null).
    static Luzart.NewBase.SelectSwitchGameObject.GroupGameObject Grp(GameObject go)
    {
        return new Luzart.NewBase.SelectSwitchGameObject.GroupGameObject
        { obGroups = go != null ? new[] { go } : new GameObject[0] };
    }

    // ItemViewWithLevelInventory.txtLevel is TMP, but old level nodes are legacy Text.
    // Add a TMP sibling at the legacy node's rect, disable the legacy node, return the TMP.
    static TMP_Text AddTmpAtLegacy(Transform legacy)
    {
        var src = legacy.GetComponent<Text>();
        var go = new GameObject("LevelTMP", typeof(RectTransform));
        go.transform.SetParent(legacy.parent, false);
        var rt = (RectTransform)go.transform; var srt = (RectTransform)legacy;
        rt.anchorMin = srt.anchorMin; rt.anchorMax = srt.anchorMax; rt.pivot = srt.pivot;
        rt.anchoredPosition = srt.anchoredPosition; rt.sizeDelta = srt.sizeDelta;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = src != null ? src.text : "Lv.0";
        t.fontSize = 26; t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
        legacy.gameObject.SetActive(false);
        return t;
    }

    // Disable every descendant whose name starts with prefix (e.g. "Reward" ad badges).
    static void DisableByPrefix(Transform root, string prefix)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t != root && t.name.StartsWith(prefix)) t.gameObject.SetActive(false);
    }
}
#endif
