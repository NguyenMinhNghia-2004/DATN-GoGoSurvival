using UnityEngine;
using Luzart.NewBase;

namespace Luzart
{
    // The IO_Training Views drive selection through fields typed as the base `BaseSelect`
    // (e.g. SlotItemEquipmentView.bsState). They call Select(int) on that base reference.
    // The stock SelectSwitchGameObject only overrides the GENERIC overload
    // (BaseSelect<int>.Select(int)), which a base-typed call never reaches — so it is a no-op.
    // This component derives DIRECTLY from BaseSelect and overrides the non-generic virtual,
    // so it responds correctly. No IO_Training View/VM code is modified.
    //
    // NOTE: one class per file, filename == classname — required so the prefab generator's
    // AddComponent serializes a valid MonoScript reference (a secondary class in a multi-class
    // file serializes as a Missing Script).
    public class IOSwitchGameObject : BaseSelect
    {
        public GameObject[] groups;

        public override void Select(int index)
        {
            if (groups == null) return;
            for (int i = 0; i < groups.Length; i++)
                if (groups[i] != null) groups[i].SetActive(i == index);
        }
    }
}
