using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Luzart
{
#if UNITY_EDITOR
    public class FindItemEditor 
	{
        private static Dictionary<string, AssetStatDefinition> _dictAssetStatDefinition = new();
        private static Dictionary<string, AssetModifier> _dictAssetModifier = new Dictionary<string, AssetModifier>();
        private static Dictionary<string, AssetModifierDefinition> _dictAssetModifierDefinition = new Dictionary<string, AssetModifierDefinition>();
        public static AssetModifier GetAssetModifierPreGame(StatType statType, string typeAdd)
        {
            string key = $"t:AssetModifier PreGame {statType} {typeAdd}";
            if (_dictAssetModifier.ContainsKey(key))
            {
                return _dictAssetModifier[key];
            }
            string[] guids = AssetDatabase.FindAssets(key);
            if (guids.Length == 0)
            {
                Debug.LogWarning($"⚠️ Không tìm thấy AssetModifier nào với tên: {statType}");
                return null;
            }
            AssetModifier chosen = null;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetModifier statDef = AssetDatabase.LoadAssetAtPath<AssetModifier>(path);
                if (statDef != null)
                {
                    chosen = statDef;
                    break;
                }
                else
                {
                    continue;
                }
            }
            if(chosen != null)
            {
                _dictAssetModifier.Add(key, chosen);
            }
            return chosen;
        }
        public static AssetModifierDefinition GetAssetModifierDefinitionPreGame(StatType statType, string typeAdd)
        {
            string key = $"t:AssetModifierDefinition {statType} {typeAdd}";
            if(_dictAssetModifierDefinition.ContainsKey(key))
            {
                return _dictAssetModifierDefinition[key];
            }
            string[] guids = AssetDatabase.FindAssets(key);
            if (guids.Length == 0)
            {
                Debug.LogWarning($"⚠️ Không tìm thấy AssetModifierDefinition nào với tên: {statType}");
                return null;
            }
            AssetModifierDefinition chosen = null;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetModifierDefinition statDef = AssetDatabase.LoadAssetAtPath<AssetModifierDefinition>(path);
                if (statDef != null)
                {
                    chosen = statDef;
                    break;
                }
                else
                {
                    continue;
                }
            }
            if (chosen != null)
            {
                _dictAssetModifierDefinition.Add(key, chosen);
            }
            return chosen;
        }
        public static AssetStatDefinition FindAssetStatDefinition(StatType statType)
        {
            string key = $"t:AssetStatDefinition {statType}";
            if (_dictAssetStatDefinition.ContainsKey(key))
            {
                return _dictAssetStatDefinition[key];
            }
            string[] guids = AssetDatabase.FindAssets(key);
            if (guids.Length == 0)
            {
                Debug.LogWarning($"⚠️ Không tìm thấy AssetStatDefinition nào với tên: {statType}");
                return null;
            }
            AssetStatDefinition chosen = null;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetStatDefinition statDef = AssetDatabase.LoadAssetAtPath<AssetStatDefinition>(path);
                if (statDef != null)
                {
                    if (statDef is IStatDefinition isStatDefinition && isStatDefinition.StatType != statType)
                    {
                        continue;
                    }
                    chosen = statDef;
                    break;
                }
                else
                {
                    continue;
                }
            }
            if (chosen == null)
            {
                Debug.LogWarning($"⚠️ Không có AssetStatDefinition nào hợp lệ với tên: {statType}");
                return null;
            }
            _dictAssetStatDefinition.Add(key, chosen);
            return chosen;
        }
        public static ResourcePool FindResourcesPoolItemWeapon(ETypeItem _eTypeItem, ERarity _eRarity, char lastChar)
        {
            string namePrefix = _eTypeItem.ToString();
            if (string.IsNullOrEmpty(namePrefix))
            {
                Debug.LogError("❌ namePrefix trống.");
                return null;
            }
            var goldPool = GetGoldPool();
            // Tìm tất cả sprite bắt đầu bằng prefix
            string[] guids = AssetDatabase.FindAssets($"t:ResourcePool {namePrefix} {_eRarity}");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"⚠️ Không tìm thấy ResourcesPool nào với prefix: {namePrefix} {_eRarity}");
                return null;
            }
            ResourcePool chosen = null;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ResourcePool resources = AssetDatabase.LoadAssetAtPath<ResourcePool>(path);
                if (resources == null)
                    continue;
                char lastCharSpr = resources.name[^1];
                if (!char.IsDigit(lastCharSpr))
                {
                    lastCharSpr = '0';
                    Debug.Log($"ℹ️ Ký tự cuối không phải số → tự động dùng '0'");
                }
                if (lastCharSpr != lastChar)
                {
                    Debug.LogWarning($"⚠️ SO '{resources.name}' không hợp lệ với pattern: {namePrefix}_00x (yêu cầu ký tự cuối là '{lastChar}') → Bỏ qua.");
                    continue;
                }
                // Chọn sprite đầu tiên đúng format và không trùng
                chosen = resources;
                break;
            }
            return chosen;
        }
        public static List<ResourcePool> GetResourcePoolItems()
        {
            string[] guids = AssetDatabase.FindAssets($"t:ResourcePool");
            List<ResourcePool> list = new List<ResourcePool>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ResourcePool resources = AssetDatabase.LoadAssetAtPath<ResourcePool>(path);
                if (resources == null)
                    continue;
                list.Add(resources);
            }
            return list;
        }
        public static ResourcePool GetGoldPool()
        {
            // Tìm tất cả sprite bắt đầu bằng prefix
            string[] guids = AssetDatabase.FindAssets($"t:ResourcePool Gold");
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            ResourcePool spr = AssetDatabase.LoadAssetAtPath<ResourcePool>(path);
            return spr;
        }
        public static ResourcePool GetGemPool()
        {
            // Tìm tất cả sprite bắt đầu bằng prefix
            string[] guids = AssetDatabase.FindAssets($"t:ResourcePool Gem");
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            ResourcePool spr = AssetDatabase.LoadAssetAtPath<ResourcePool>(path);
            return spr;
        }
        public static AssetNumber GetAssetNumberForStatEditor(string skillType,string itemDefinition, int index)
        {
            string[] guids = AssetDatabase.FindAssets($"t:AssetNumber Number_{skillType}_{itemDefinition} {index}");
            if (guids.Length == 0)
            {
                return null;
            }
            AssetNumber chosen = null;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetNumber statDef = AssetDatabase.LoadAssetAtPath<AssetNumber>(path);
                if (statDef != null)
                {
                    chosen = statDef;
                    break;
                }
                else
                {
                    continue;
                }
            }
            return chosen;
        }
        public static bool IsHaveStatDefinition(StatType statType, List<Stat> list)
        {
            var has = list.Find(stat => stat.StatType == statType);
            if(has != null)
            {
                return false;
            }
            return true;
        }
        public static List<T> FindAllScriptableObjectWithInterface<T, T1>(string findT = "ScriptableObject")
        {
            var contentList = new List<T>();
            // Find all ScriptableObject assets in the project
            string[] guids = AssetDatabase.FindAssets($"t:{findT}");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                // Load all assets (including sub-assets) at this path
                UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (UnityEngine.Object asset in allAssets)
                {
                    // Check if the asset is a ScriptableObject and implements IContent
                    if (asset is T scriptableObject && scriptableObject is T1)
                    {
                        contentList.Add(scriptableObject);
                    }
                }
            }
            return contentList;
        }
    }
#endif
}
