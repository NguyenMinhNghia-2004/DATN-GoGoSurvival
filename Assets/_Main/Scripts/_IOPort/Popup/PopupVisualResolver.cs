using System;
using System.Linq;
using UnityEngine;

namespace Luzart
{
    public class PopupVisualResolver : AbstractScriptableContent
    {
        [SerializeField] Popup[] popupPrefabs;

        public Popup ResolvePopupPrefab(PopupLayer popupLayer, Type popupType)
        {
            return popupPrefabs.FirstOrDefault(x => x.GetType() == popupType);
        }
    }
}
