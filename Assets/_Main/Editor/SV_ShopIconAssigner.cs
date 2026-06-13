#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Luzart;
using Cysharp.Threading.Tasks;

/// <summary>
/// Fills in the SV_ "Daily Shop" item icons: each SV_ItemCatalog entry currently has a null
/// icon (cards fall back to the blueprint default). This matches each entry to the
/// corresponding ItemConfig (by name, preferring the same rarity) and assigns its sprite.
/// Data-only — does not touch any shop prefab/layout.
/// </summary>
public static class SV_ShopIconAssigner
{
    const string CATALOG = "Assets/_Main/Resources/Shop/SV_ItemCatalog.asset";
    const string ITEMS_DIR = "Assets/_Main/Data/Items";

    [MenuItem("Tools/IOShop/Assign SV Catalog Icons")]
    public static void Assign()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<SV_ItemCatalog>(CATALOG);
        if (catalog == null) { Debug.LogError("[SVIcons] SV_ItemCatalog not found at " + CATALOG); return; }

        var items = AssetDatabase.FindAssets("t:ItemConfig", new[] { ITEMS_DIR })
            .Select(g => AssetDatabase.LoadAssetAtPath<ItemConfig>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i => i != null && i.Sprite != null)
            .ToList();

        var so = new SerializedObject(catalog);
        var entries = so.FindProperty("entries");
        int assigned = 0, missing = 0;
        for (int i = 0; i < entries.arraySize; i++)
        {
            var e = entries.GetArrayElementAtIndex(i);
            var iconProp = e.FindPropertyRelative("icon");
            var nameProp = e.FindPropertyRelative("displayName");
            var idProp = e.FindPropertyRelative("id");
            var rarityProp = e.FindPropertyRelative("rarity");
            int rarity = rarityProp != null ? rarityProp.enumValueIndex : 0;

            string keyName = Norm(nameProp != null ? nameProp.stringValue : null);
            string keyId = Norm(idProp != null ? idProp.stringValue.Replace("Eq_", "") : null);
            var slotProp = e.FindPropertyRelative("slot");
            int slot = slotProp != null ? slotProp.enumValueIndex : -1;

            ItemConfig match = FindMatch(items, keyName, rarity) ?? FindMatch(items, keyId, rarity)
                            ?? FindMatch(items, keyName, -1) ?? FindMatch(items, keyId, -1)
                            ?? FindBySlot(items, slot, rarity) ?? FindBySlot(items, slot, -1);

            if (match != null) { iconProp.objectReferenceValue = match.Sprite; assigned++; }
            else { missing++; Debug.LogWarning($"[SVIcons] no ItemConfig match for entry '{(nameProp != null ? nameProp.stringValue : "?")}'"); }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SVIcons] entries={entries.arraySize} assigned={assigned} missing={missing}");
    }

    static ItemConfig FindMatch(List<ItemConfig> items, string key, int rarity)
    {
        if (string.IsNullOrEmpty(key)) return null;
        foreach (var it in items)
        {
            if (!Norm(it.name).Contains(key)) continue;
            if (rarity >= 0 && (int)it.Rarity != rarity) continue;
            return it;
        }
        return null;
    }

    // Play-mode: opens the SV "Daily Shop" (NinjaUI) so we can verify item icons render.
    [MenuItem("Tools/IOShop/DEBUG Open SV Shop (Play)")]
    public static void DebugOpenSVShop()
    {
        if (UIManager.Instance == null) { Debug.LogError("[SVIcons] UIManager not ready (enter Play)."); return; }
        UIManager.Instance.ShowAsync(UIId.SV_Shop).Forget();
    }

    static ItemConfig FindBySlot(List<ItemConfig> items, int slot, int rarity)
    {
        if (slot < 0) return null;
        foreach (var it in items)
        {
            if ((int)it.TypeItem != slot) continue;
            if (rarity >= 0 && (int)it.Rarity != rarity) continue;
            return it;
        }
        return null;
    }

    static string Norm(string s) =>
        new string((s ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
#endif
