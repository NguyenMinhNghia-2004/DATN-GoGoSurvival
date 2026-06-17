using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;
namespace Luzart
{
    public enum ERarity
    {
        Rare = 0,
        Epic = 1,
        Legend = 2,
    }
    public enum ETypeItem
    {
        Weapon = 0,
        Armor = 1,
        Necklace = 2,
        Belt = 3,
        Gloves = 4,
        Shoes = 5,
    }
    [CreateAssetMenu(fileName = "ItemConfig", menuName = "Luzart/Item/Item Config")]
    public class ItemConfig : AbstractScriptableContent, IItem
    {
        [SerializeField] private string _name;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private ERarity _eRarity;
        [SerializeField] private ETypeItem _eTypeItem;
        [SerializeField] private AssetUnlockable unlockable;
        [SerializeField] private AssetLevel assetLevel;
        [SerializeField] private ResourcePool _cardPool;
        [SerializeField] private List<UpgradeItemConfig> upgradeItemConfigs;
        [SerializeField] private List<UpgradeItemConfig> artifactModifierFactor;
        public string NameItem => _name;
        public Sprite Sprite => _sprite;
        public ERarity Rarity => _eRarity;
        public ETypeItem TypeItem => _eTypeItem;
        public ILevelable Level => assetLevel;
        public IUnlockable Unlockable => unlockable;
        public ResourcePool CardPool => _cardPool;
        public void SetCardPoolEditor(ResourcePool pool) { _cardPool = pool; }
        public IReadOnlyList<IModifierFactorGroup> ArtifactModifierFactorGroups => _artifactModifierFactorGroups;
        public IReadOnlyList<IModifierFactorGroup> EquippedModifierFactorGroups => _equippedModifierFactorGroups;
        List<IModifierFactorGroup> _artifactModifierFactorGroups = new List<IModifierFactorGroup>();
        List<IModifierFactorGroup> _equippedModifierFactorGroups = new List<IModifierFactorGroup>();
        public IReadOnlyList<IModifierPair> GetModifierPairs(int level)
        {
            if (level < 0 || level >= upgradeItemConfigs.Count)
            {
                return new List<IModifierPair>();
            }
            return upgradeItemConfigs[level].ModifierPairs;
        }
        public IReadOnlyList<IModifierPair> GetCurrentModifierPairs()
        {
            int level = Level.LevelIndex;
            return GetModifierPairs(level);
        }
        protected override void DoInitialize()
        {
            base.DoInitialize();
            SolveModifierFactorGroup();
        }
        public void UpgradeItem()
        {
            int level = Level.LevelIndex;
            int nextLevel = level + 1;
            Level.SetLevelIndex(nextLevel);
        }
        private void SolveModifierFactorGroup()
        {
            for (int i = 0; i < upgradeItemConfigs.Count; i++)
            {
                var item = upgradeItemConfigs[i];
                RuntimeModifierFactorGroup runtimeModifierFactorGroup = new RuntimeModifierFactorGroup(item.ModifierPairs);
                _equippedModifierFactorGroups.Add(runtimeModifierFactorGroup);
            }
        }
        [System.Serializable]
        class UpgradeItemConfig
        {
            [SerializeField] private List<SerializedModifierPair> modifierFactors;
            public IReadOnlyList<IModifierPair> ModifierPairs => modifierFactors;
            public List<SerializedModifierPair> ListSerializedModifierPairEditor
            {
                get => modifierFactors;
                set => modifierFactors = value;
            }
        }
#if UNITY_EDITOR
        private void CreateAndAutoAssetUnlockable()
        {
            // Tạo tên sub-asset
            string assetLevelName = $"|Unlockable_{name}";
            // Tạo instance AssetLevel
            var newAssetUnlockable = ScriptableObject.CreateInstance<AssetUnlockable>();
            newAssetUnlockable.name = assetLevelName;
            // Thêm sub-asset vào ItemConfig
            AssetDatabase.AddObjectToAsset(newAssetUnlockable, this);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            // Gán vào biến assetLevel
            unlockable = newAssetUnlockable;
            // Đánh dấu đã thay đổi
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(newAssetUnlockable);
            Debug.Log($"✅ Đã tạo và gán AssetLevel: {assetLevelName}");
        }
        private void AddItemShardToUnlockable()
        {
            unlockable.UnlockCostEditors = new List<SerializableCostCreator_CommonlyUsed>();
            var goldPool = FindItemEditor.GetGoldPool();
            var assetCostGold = new SerializableCostCreator_CommonlyUsed();
            assetCostGold.ModeEditor = Modes.Resource;
            assetCostGold.ResourcePoolEditor = goldPool;
            assetCostGold.NumberPickerEditor = new NumberPicker(NumberMode.Constant, 5);
            unlockable.UnlockCostEditors.Add(assetCostGold);
            var assetCost = new SerializableCostCreator_CommonlyUsed();
            assetCost.ModeEditor = Modes.Resource;
            char lastChar = name[^1];
            if (!char.IsDigit(lastChar)) lastChar = '0';
            var resourcePool = FindItemEditor.FindResourcesPoolItemWeapon(_eTypeItem, _eRarity, lastChar);
            assetCost.ResourcePoolEditor = resourcePool;
            assetCost.NumberPickerEditor = new NumberPicker(NumberMode.Constant, 20);
            unlockable.UnlockCostEditors.Add(assetCost);
        }
        private void AddAllToLevelUpCostAssetLevel()
        {
            var goldPool = FindItemEditor.GetGoldPool();
            assetLevel.LevelUpCostsEditor = new List<ListSerializableCostCreator_CommonlyUsed>();
            for (int i = 0; i < 100; i++)
            {
                var list = new ListSerializableCostCreator_CommonlyUsed();
                var assetCostGold = new SerializableCostCreator_CommonlyUsed();
                assetCostGold.ModeEditor = Modes.Resource;
                assetCostGold.ResourcePoolEditor = goldPool;
                assetCostGold.NumberPickerEditor = new NumberPicker(NumberMode.Constant, 5 + i * 1);
                list.List.Add(assetCostGold);
                var assetCost = new SerializableCostCreator_CommonlyUsed();
                assetCost.ModeEditor = Modes.Resource;
                char lastChar = name[^1];
                if (!char.IsDigit(lastChar)) lastChar = '0';
                var resourcePool = FindItemEditor.FindResourcesPoolItemWeapon(_eTypeItem, _eRarity, lastChar);
                assetCost.ResourcePoolEditor = resourcePool;
                assetCost.NumberPickerEditor = new NumberPicker(NumberMode.Constant, i+1);
                list.List.Add(assetCost);
                assetLevel.LevelUpCostsEditor.Add(list);
            }
        }
        private void AddAllFactor()
        {
            int random = UnityEngine.Random.Range(0, 2);
            StatType statType = StatType.ATK;
            if(random == 1)
            {
                statType = StatType.HPMax;
            }
            double firstStat = 0;
            double valueExp = 0.1;
            if(_eRarity == ERarity.Rare)
            {
                firstStat = 0.1;
            }
            else if(_eRarity == ERarity.Epic)
            {
                firstStat = .5;
                valueExp = 0.15;
            }
            else
            {
                firstStat = 1;
                valueExp = 0.2;
            }
            upgradeItemConfigs = new List<UpgradeItemConfig>();
            for (int i = 0; i < 100; i++)
                {
                    UpgradeItemConfig upgradeConfig = new UpgradeItemConfig();
                    upgradeConfig.ListSerializedModifierPairEditor = new List<SerializedModifierPair>();
                    SerializedModifierPair serializedModifierPair = new SerializedModifierPair();
                    serializedModifierPair.AssetModifierEditor = FindItemEditor.GetAssetModifierPreGame(statType, "AddRatio");
                    serializedModifierPair.FactorEditor = new SerializedModifierFactor();
                    serializedModifierPair.FactorEditor.DefinitionEditor = FindItemEditor.GetAssetModifierDefinitionPreGame(statType, "AddRatio");
                    serializedModifierPair.FactorEditor.ValueEditor = new NumberPicker(NumberMode.Constant,Math.Round(firstStat + i * valueExp,2));
                    upgradeConfig.ListSerializedModifierPairEditor.Add(serializedModifierPair);
                    upgradeItemConfigs.Add(upgradeConfig);
                }
        }
#endif
    }
}
//public void RefreshEquippedModifiers(bool isEquipped)
//{
//    for (int i = 0; i < _equippedModifierFactorGroups.Count; i++)
//    {
//        IModifierFactorGroup g = _equippedModifierFactorGroups[i];
//        if (isEquipped && i == Level.LevelIndex)
//        {
//            g.Activate();
//        }
//        else
//        {
//            g.Deactivate();
//        }
//    }
//}
//public UpgradeItemConfig GetUpgradeItemConfig(int level)
//{
//    if (level < 0 || level >= _upgradeItemConfigs.Count)
//    {
//        return null;
//    }
//    return _upgradeItemConfigs[level];
//}
//public UpgradeItemConfig GetCurrentUpgradeItemConfig()
//{
//    int level = Level.LevelIndex;
//    return GetUpgradeItemConfig(level);
//}
//public bool IsStatusUnlocked()
//{
//    return Unlockable.IsUnlocked;
//}
//public void EquippedItem()
//{
//    int level = Level.LevelIndex;
//    var upgrade = GetUpgradeItemConfig(level);
//    if (upgrade == null)
//    {
//        Debug.LogError("\"Khong Equip duoc. Null khi khong lay dc upgrade item config\"");
//        return;
//    }
//    for (int i = 0; i < upgrade.ModifierPairs.Count; i++)
//    {
//        var item = upgrade.ModifierPairs[i];
//        item.Modifier.AddFactor(item.Factor);
//    }
//}
//public void UnEquippedItem()
//{
//    int level = Level.LevelIndex;
//    var upgrade = GetUpgradeItemConfig(level);
//    if (upgrade == null)
//    {
//        Debug.LogError("\"Khong huy Equip duoc. Null khi khong lay dc upgrade item config\"");
//        return;
//    }
//    for (int i = 0; i < upgrade.ModifierPairs.Count; i++)
//    {
//        var item = upgrade.ModifierPairs[i];
//        item.Modifier.RemoveFactor(item.Factor);
//    }
//}
//#if UNITY_EDITOR
//        private void AutoSpriteItemWeapon()
//        {
//            string namePrefix = _eTypeItem.ToString();
//            if (string.IsNullOrEmpty(namePrefix))
//            {
//                Debug.LogError("❌ namePrefix trống.");
//                return;
//            }
//            // Tìm tất cả sprite bắt đầu bằng prefix
//            string[] guids = AssetDatabase.FindAssets($"t:Sprite {namePrefix}");
//            if (guids.Length == 0)
//            {
//                Debug.LogWarning($"⚠️ Không tìm thấy sprite nào với prefix: {namePrefix}");
//                return;
//            }
//            // Regex để match hậu tố _00x (3 chữ số)
//            Regex regex = new Regex($@"^{namePrefix}_\d{{3}}$", RegexOptions.Compiled);
//            Sprite chosen = null;
//            foreach (var guid in guids)
//            {
//                string path = AssetDatabase.GUIDToAssetPath(guid);
//                Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
//                if (spr == null)
//                    continue;
//                // Lấy ký tự cuối, nếu không phải số thì mặc định là '0'
//                char lastChar = name[^1];
//                if (!char.IsDigit(lastChar))
//                {
//                    lastChar = '0';
//                    Debug.Log($"ℹ️ Ký tự cuối không phải số → tự động dùng '0'");
//                }
//                // Kiểm tra format tên sprite
//                char lastCharSpr = spr.name[^1];
//                if (lastCharSpr != lastChar)
//                {
//                    Debug.LogWarning($"⚠️ Sprite '{spr.name}' không hợp lệ với pattern: {namePrefix}_00x (yêu cầu ký tự cuối là '{lastChar}') → Bỏ qua.");
//                    continue;
//                }
//                // Nếu trùng sprite hiện tại → Skip + log
//                if (_sprite == spr)
//                {
//                    Debug.LogWarning($"⚠️ Sprite '{spr.name}' đã được gán nên bỏ qua.");
//                    continue;
//                }
//                // Chọn sprite đầu tiên đúng format và không trùng
//                chosen = spr;
//                break;
//            }
//            if (chosen == null)
//            {
//                Debug.LogWarning($"⚠️ Không có sprite nào hợp lệ với pattern: {namePrefix}_00x");
//                return;
//            }
//            // Assign
//            _sprite = chosen;
//            // Save
//            EditorUtility.SetDirty(this);
//            Debug.Log($"✅ Sprite đã gán: {chosen.name}");
//        }
//        private void AutoCostUnlock()
//        {
//            // Lấy ký tự cuối, nếu không phải số thì mặc định là '0'
//            char lastChar = name[^1];
//            if (!char.IsDigit(lastChar))
//            {
//                lastChar = '0';
//                Debug.Log($"ℹ️ Ký tự cuối không phải số → tự động dùng '0'");
//            }
//            ResourcePool chosen = FindItemEditor.FindResourcesPoolItemWeapon(_eTypeItem, _eRarity, lastChar);
//            ResourcePool goldPool = FindItemEditor.GetGoldPool();
//            if (chosen == null)
//            {
//                Debug.LogError($"⚠️ Không có SO nào hợp lệ với pattern: {name}");
//                return;
//            }
//            double amountShard = 5;
//            double amountGold = 50;
//            if (_eRarity == ERarity.Epic)
//            {
//                amountShard = 10;
//                amountGold = 100;
//            }
//            else if (_eRarity == ERarity.Legend)
//            {
//                amountShard = 20;
//                amountGold = 200;
//            }
//            _costUnlock = new Cost();
//            _costUnlock.ResourcePools = new List<ResourceReward>();
//            _costUnlock.ResourcePools.Add(new ResourceReward()
//            {
//                ResourcePool = chosen,
//                Amount = amountShard,
//            });
//            _costUnlock.ResourcePools.Add(new ResourceReward()
//            {
//                ResourcePool = goldPool,
//                Amount = amountGold,
//            });
//            // Save
//            EditorUtility.SetDirty(this);
//            Debug.Log($"✅ Sprite đã gán: {chosen.name}");
//        }
//        private void AutoNameItem()
//        {
//            var allString = _name.Split('_');
//            var enum1 = allString[0];
//            if (Enum.TryParse<ETypeItem>(enum1, out var result))
//            {
//                _eTypeItem = result;
//            }
//        }
//        private void AutoUpgradeItem()
//        {
//            var goldPool = FindItemEditor.GetGoldPool();
//            double first = 0.1d;
//            if (_eRarity == ERarity.Epic)
//            {
//                first = 0.5d;
//            }
//            else if (_eRarity == ERarity.Legend)
//            {
//                first = 1f;
//            }
//            StatType eType = (StatType)(UnityEngine.Random.Range(0, 2));
//            _upgradeItemConfigs = new List<UpgradeItemConfig>();
//            int maxLevel = 100;
//            for (int i = 0; i < maxLevel; i++)
//            {
//                var upgradeItemConfig = new UpgradeItemConfig();
//                var mod = new SerializedModifierPair();
//                mod.FactorEditor = new SerializedModifierFactor();
//                mod.FactorEditor.DefinitionEditor = FindItemEditor.GetAssetModifierDefinitionPreGame(eType, "AddRatio");
//                mod.FactorEditor.ValueEditor = new NumberPicker(NumberMode.Constant, Math.Round(first + i * 0.1d, 2));
//                mod.AssetModifierEditor = FindItemEditor.GetAssetModifierPreGame(eType, "AddRatio");
//                upgradeItemConfig.ModifierPairs = new List<SerializedModifierPair>() { mod };
//                upgradeItemConfig.CostUpgrade = new Cost();
//                upgradeItemConfig.CostUpgrade.ResourcePools = new List<ResourceReward>();
//                upgradeItemConfig.CostUpgrade.ResourcePools.Add(new ResourceReward()
//                {
//                    ResourcePool = _costUnlock.ResourcePools[0].ResourcePool,
//                    Amount = 2,
//                });
//                upgradeItemConfig.CostUpgrade.ResourcePools.Add(new ResourceReward()
//                {
//                    ResourcePool = goldPool,
//                    Amount = 20,
//                });
//                _upgradeItemConfigs.Add(upgradeItemConfig);
//            }
//        }
//        
//        private void AutoSetup()
//        {
//            AutoNameItem();
//            AutoSpriteItemWeapon();
//            AutoCostUnlock();
//            AutoUpgradeItem();
//            _name = name;
//        }
//        
//        private void CreateAndAutoAssetLevel()
//        {
//            // Tạo tên sub-asset
//            string assetLevelName = $"|AssetLevel_{name}";
//            // Tạo instance AssetLevel
//            var newAssetLevel = ScriptableObject.CreateInstance<AssetLevel>();
//            newAssetLevel.name = assetLevelName;
//            // Gán maxLevelIndex bằng số lượng upgradeItemConfig
//            int maxLevel = _upgradeItemConfigs != null ? _upgradeItemConfigs.Count : 0;
//            newAssetLevel.MaxLevelIndexEditor = maxLevel;
//            // Thêm sub-asset vào ItemConfig
//            AssetDatabase.AddObjectToAsset(newAssetLevel, this);
//            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
//            // Gán vào biến assetLevel
//            assetLevel = newAssetLevel;
//            // Đánh dấu đã thay đổi
//            EditorUtility.SetDirty(this);
//            EditorUtility.SetDirty(newAssetLevel);
//            Debug.Log($"✅ Đã tạo và gán AssetLevel: {assetLevelName} với maxLevelIndex = {maxLevel}");
//        }
//#endif
