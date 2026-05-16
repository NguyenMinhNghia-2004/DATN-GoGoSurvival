using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    [System.Serializable]
    public class CostConfig
    {
        [SerializeField] private List<ResourceReward> _resourcePools;
        public List<ResourceReward> ResourcePools
        {
            get { return _resourcePools; }
            set { _resourcePools = value; }
        }
    }
}
