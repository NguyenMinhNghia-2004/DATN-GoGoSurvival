using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "DropConfigRequire", menuName = "Luzart/Drop/Drop Config Require", order = 1)]
    public class DropConfigRequire : ScriptableObject
    {
        [SerializeField] private List<DropConfigRequireItem> dropConfigRequireItems = new List<DropConfigRequireItem>();
        public virtual List<DropConfigRequireItem> DropConfigRequireItems => dropConfigRequireItems;
    }
    [System.Serializable]
    public class DropConfigRequireItem
    {
        public DropConfig dropConfig;
        public int amount;
    }
}