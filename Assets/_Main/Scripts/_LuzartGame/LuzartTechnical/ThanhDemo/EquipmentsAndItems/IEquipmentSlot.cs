using System;
using System.Collections.Generic;
namespace Luzart
{
    public interface IEquipmentSlot
    {
        ItemConfig EquippedItem { get; }
        event Action<IEquipmentSlot> Changed;
        IBool IsUnlocked { get; }
    }
    public interface IUnlockable
    {
        IBoolSet IsUnlocked { get; }
        IReadOnlyList<ICost> UnlockCosts { get; }
    }
    public interface ILevelable
    {
        int LevelIndex { get; }
        int MaxLevelIndex { get; }
        event Action<ILevelable> Changed;
        void SetLevelIndex(int levelIndex);
        void SetMaxLevelIndex(int maxLevelIndex);
        IReadOnlyList<ICost> GetLevelUpCost(int levelIndex= -1);
    }
}
