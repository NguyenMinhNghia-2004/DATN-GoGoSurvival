using System;
using System.Collections.Generic;
#if UNITY_EDITOR
#endif
namespace Luzart
{
    public class EquipmentSlotModifierWorker : IDisposable
    {
        IEquipmentSlot _slot;
        List<IModifierPair> _modifierPairs;
        public EquipmentSlotModifierWorker(IEquipmentSlot slot)
        {
            _slot = slot;
            _slot.Changed += Slot_Changed;
            _modifierPairs = new();
        }
        void IDisposable.Dispose()
        {
            _slot.Changed -= Slot_Changed;
        }
        void Slot_Changed(IEquipmentSlot obj)
        {
            throw new NotImplementedException();
        }
        void RefreshModifiers()
        {
            var needToActivateModifierPairs = _slot.EquippedItem.EquippedModifierFactorGroups[_slot.EquippedItem.Level.LevelIndex];
        }
    }
}
