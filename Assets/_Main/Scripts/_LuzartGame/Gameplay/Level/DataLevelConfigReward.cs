using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Luzart
{
    public class DataLevelConfigReward : AbstractScriptableContent
    {
        [SerializeField]
        private List<ListResourcesReward> rewards;
        public List<ResourceReward> GetRewardOfLevel(int level)
        {
            level = Mathf.Clamp(level, 0, rewards.Count - 1);
            return rewards[level].resources;
        }
#if UNITY_EDITOR
        private void GenReward(int length = 100)
        {
            rewards = new List<ListResourcesReward>();
            for (int i = 0; i < length; i++)
            {
                ListResourcesReward listResourcesReward = new ListResourcesReward();
                listResourcesReward.resources.Add(GetGold(i + 1));
                int random = UnityEngine.Random.Range(1, 4);
                for (int j = 0; j < random; j++)
                {
                    listResourcesReward.resources.Add(GetAnyShard());
                }
                rewards.Add(listResourcesReward);
            }
        }
        private ResourceReward GetGold(int index)
        {
            ResourceReward resourceReward = new ResourceReward
            {
                ResourcePool = FindItemEditor.GetGoldPool(),
                Amount = index * 100 * UnityEngine.Random.Range(1, 5)
            };
            return resourceReward;
        }
        private ResourceReward GetAnyShard()
        {
            List<ETypeItem> list = Enum.GetValues(typeof(ETypeItem))
                                    .Cast<ETypeItem>()
                                    .ToList();
            int random = UnityEngine.Random.Range(0, list.Count);
            List<ERarity> rarityList = Enum.GetValues(typeof(ERarity))
                        .Cast<ERarity>()
                        .ToList();
            int randomRarity = UnityEngine.Random.Range(1, rarityList.Count);
            int randomLastChar = UnityEngine.Random.Range(0, 5);
            ResourceReward resourceReward = new ResourceReward
            {
                ResourcePool = FindItemEditor.FindResourcesPoolItemWeapon(list[random], rarityList[randomRarity], (char)('0' + 1)),
                Amount = UnityEngine.Random.Range(5, 100)
            };
            return resourceReward;
        }
#endif
    }
    [Serializable]
    public class ListResourcesReward
    {
        public List<ResourceReward> resources = new List<ResourceReward>();
    }
}
