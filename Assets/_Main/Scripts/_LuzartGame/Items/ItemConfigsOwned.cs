using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class ItemConfigsOwned : AbstractScriptableContent, IItemInventory
    {
        private readonly Dictionary<Type, object> _dictItemInventoryOwned = new();
        public event Action OnItemOwnedChanged;
        protected override void DoInitialize()
        {
            base.DoInitialize();
            var allItemConfigs = _domain.GetAll<ItemConfig>();
            for (int i = 0; i < allItemConfigs.Count; i++)
            {
                var item = allItemConfigs[i];
                if (item == null || item.Unlockable == null || item.Unlockable.IsUnlocked == null)
                {
                    Debug.LogError($"{item.name}");
                    Debug.LogError($"{item.Unlockable}");
                    Debug.LogError($"{item.Unlockable.IsUnlocked == null}");
                }
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    item.Unlockable.IsUnlocked.Set(true);
                }
                item.Unlockable.IsUnlocked.Changed += IsUnlocked_Changed;
            }
        }
        protected override void DoTerminate()
        {
            base.DoTerminate();
            var allItemConfigs = _domain.GetAll<ItemConfig>();
            for (int i = 0; i < allItemConfigs.Count; i++)
            {
                var item = allItemConfigs[i];
                item.Unlockable.IsUnlocked.Changed -= IsUnlocked_Changed;
            }
        }
        private void IsUnlocked_Changed(IBool obj)
        {
            OnItemOwnedChanged?.Invoke();
        }
        IReadOnlyList<IItem> IItemInventory.GetAllItems()
        {
            List<IItem> items = new();
            var allItemConfigs = _domain.GetAll<ItemConfig>();
            for (int i = 0; i < allItemConfigs.Count; i++)
            {
                var item = allItemConfigs[i];
                if (item.Unlockable.IsUnlocked.Value)
                {
                    items.Add(item);
                }
            }
            return items;
        }
        IItemLookup<T> IItemInventory.GetItemLookup<T>()
        {
            Type type = typeof(T);
            if (_dictItemInventoryOwned.TryGetValue(type, out var lookup))
            {
                IItemLookup<T> newLookup = lookup as IItemLookup<T>;
                if (newLookup == null)
                {
                    throw new InvalidCastException($"ItemInventoryOwned: Cannot create lookup for type {type}");
                }
                return newLookup;
            }
            else
            {
                if (type == typeof(ERarity))
                {
                    var newLookup = new ItemLookupByRarity(_domain.GetAll<ItemConfig>());
                    _dictItemInventoryOwned.Add(type, newLookup);
                    if (newLookup is IItemLookup<T> castedLookup)
                    {
                        return castedLookup;
                    }
                    else
                    {
                        throw new InvalidCastException($"ItemInventoryOwned: Cannot create lookup for type {type}");
                    }
                }
                else if (type == typeof(ETypeItem))
                {
                    var newLookup = new ItemLookupByItemType(_domain.GetAll<ItemConfig>());
                    _dictItemInventoryOwned.Add(type, newLookup);
                    if (newLookup is IItemLookup<T> castedLookup)
                    {
                        return castedLookup;
                    }
                    else
                    {
                        throw new InvalidCastException($"ItemInventoryOwned: Cannot create lookup for type {type}");
                    }
                }
                else
                {
                    throw new Exception($"ItemInventoryOwned: Cannot create lookup for type {type}");
                }
            }
        }
    }
    public class ItemLookupByRarity : IItemLookup<ERarity>
    {
        private readonly Dictionary<ERarity, List<IItem>> _dictItemsByRarity = new();
        public ItemLookupByRarity(IReadOnlyList<ItemConfig> allItems)
        {
            for (int i = 0; i < allItems.Count; i++)
            {
                var item = allItems[i];
                if (!_dictItemsByRarity.TryGetValue(item.Rarity, out var items))
                {
                    items = new List<IItem>();
                    _dictItemsByRarity.Add(item.Rarity, items);
                }
                items.Add(item);
            }
        }
        public IReadOnlyList<IItem> FindItems(ERarity key)
        {
            if (_dictItemsByRarity.TryGetValue(key, out var items))
            {
                return items;
            }
            return new List<IItem>();
        }
    }
    public class ItemLookupByItemType : IItemLookup<ETypeItem>
    {
        private readonly Dictionary<ETypeItem, List<IItem>> _dictItemsByType = new();
        public ItemLookupByItemType(IReadOnlyList<ItemConfig> allItems)
        {
            for (int i = 0; i < allItems.Count; i++)
            {
                var item = allItems[i];
                if (!_dictItemsByType.TryGetValue(item.TypeItem, out var items))
                {
                    items = new List<IItem>();
                    _dictItemsByType.Add(item.TypeItem, items);
                }
                items.Add(item);
            }
        }
        public IReadOnlyList<IItem> FindItems(ETypeItem key)
        {
            if (_dictItemsByType.TryGetValue(key, out var items))
            {
                return items;
            }
            return new List<IItem>();
        }
    }
}
