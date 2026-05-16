using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public sealed class AssetBool_EquipmentSlotContainItemConfig : BaseAssetBool
    {
        [SerializeField] private AssetEquipmentSlot equipmentSlot;
        [SerializeField] private List<ItemConfig> itemConfigs;
        protected override bool DoGetValue()
        {
            IEquipmentSlot iEquipmentSlot = equipmentSlot;
            if (iEquipmentSlot.EquippedItem == null)
                return false;
            for (int i = 0; i < itemConfigs.Count; i++)
            {
                if(iEquipmentSlot.EquippedItem == itemConfigs[i])
                {
                    return true;
                }
            }
            return false;
        }
    }
}
