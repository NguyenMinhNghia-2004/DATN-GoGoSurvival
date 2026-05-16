using System;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public interface IItemInventory
    {
        public event Action OnItemOwnedChanged;
        IReadOnlyList<IItem> GetAllItems();
        IItemLookup<T> GetItemLookup<T>();
    }
    public interface IItemLookup<T>
    {
        IReadOnlyList<IItem> FindItems(T key);
    }
    public interface IItem
    {
        public string NameItem { get; }
        public Sprite Sprite { get; }
        public ERarity Rarity { get; }
        public ETypeItem TypeItem { get; }
        public ILevelable Level { get; }
        public IUnlockable Unlockable { get; }
        public IReadOnlyList<IModifierFactorGroup> ArtifactModifierFactorGroups { get; }    
        public IReadOnlyList<IModifierFactorGroup> EquippedModifierFactorGroups { get; }
    }
    public interface IItemListing
    {
        IReadOnlyList<IItem> GetItems();
    }
}
