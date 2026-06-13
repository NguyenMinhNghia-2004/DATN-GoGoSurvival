using UnityEngine;
using Luzart.NewBase;

namespace Luzart
{
    // Base-typed counterpart for the bool selection path (PopupItemEquipView.bsEquipButton
    // calls Select(bool) through a BaseSelect field). Overrides the non-generic virtual so the
    // call fires; the stock SelectToggleGameObject only overrides BaseSelect<bool>.Select(bool)
    // which the base-typed call never reaches. No IO_Training View/VM code is modified.
    //
    // NOTE: one class per file, filename == classname (see IOSwitchGameObject for why).
    public class IOToggleGameObject : BaseSelect
    {
        public GameObject[] obSelect;
        public GameObject[] obUnSelect;

        public override void Select(bool value)
        {
            if (obSelect != null)
                foreach (var g in obSelect) if (g != null) g.SetActive(value);
            if (obUnSelect != null)
                foreach (var g in obUnSelect) if (g != null) g.SetActive(!value);
        }
    }
}
