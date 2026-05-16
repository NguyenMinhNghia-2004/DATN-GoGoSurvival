using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace Luzart
{
    public interface IChestRandom
    {
        public IResourcePool ResourcePool { get; }
        public INumberPickerPair PickerPair { get; }
        public INumber Percent { get; }
    }
    public interface IChest : IResourcePool
    {
        public IReadOnlyList<IChestRandom> ChestRandoms { get; }
        IReadOnlyList<IResourceReward> GetResourcePools();
    }
    public class Chest : AbstractScriptableContent, IChest
    {
		[SerializeField] private ResourceDefinition resourceDefinition;
		[SerializeField] private List<ChestRandomResource> chestRandomResources;
		IResourceDefinition IResourcePool.Definition => resourceDefinition;
		private INumberWithSet _number;
        INumber IResourcePool.Value => _number;
        IReadOnlyList<IChestRandom> IChest.ChestRandoms => chestRandomResources;
        void IResourcePool.Set(double amount)
        {
            _number.Set(amount);
        }
        IReadOnlyList<IResourceReward> IChest.GetResourcePools()
        {
			return DoGetResourcePool();
        }
		IReadOnlyList<IResourceReward> DoGetResourcePool()
		{
			List<IResourceReward> listResult = new();
			List<IResourceReward> listShard = new ();
			List<IResourceReward> listOther = new ();
			for (int i = 0; i < chestRandomResources.Count; i++)
			{
				IChestRandom item = chestRandomResources[i];
				int random = UnityEngine.Random.Range((int)item.PickerPair.NumberMin.Value,(int)(item.PickerPair.NumberMax.Value + 1));
				IResourceReward iResourceReward = new Runtime_ResourceReward(item.ResourcePool, random);
				float randomPercent = UnityEngine.Random.Range(0f, 100f);
				if(randomPercent > item.Percent.Value)
				{
					continue;
                }
                if (item.ResourcePool.Definition.EType == ETypeResourceDefinition.Shard)
				{
					listShard.Add(iResourceReward);
				}
				else
				{
					listOther.Add(iResourceReward);
				}
			}
			listShard.Shuffle();
			int randomShard = UnityEngine.Random.Range(1,listShard.Count);
			listResult.AddRange(listOther);
			for (int i = 0; i < randomShard; i++)
			{
				listResult.Add(listShard[i]);
			}
			return listResult;
		}
#if UNITY_EDITOR
		private void GenerateRandomShard()
		{
			int min = 3;
			int max = 6;
			int minGold = 1;
			int maxGold = 50;
			int minGem = 5;
			int maxGem = 10;
			float percentGold = 0;
			float percentGem = 0;
			float rateRare = 1;
			float rateEpic = 2;
			float rateLegend = 3;
			if (name.Contains("Small"))
			{
				min = 3;
				max = 6;
				minGold = 1;
				maxGold = 50;
				minGem = 5;
				maxGem = 10;
				percentGold = Random.Range(0, 25f);
                percentGem = Random.Range(0, 10f);
				rateRare = 7;
				rateEpic = 2;
				rateLegend = 1; 
            }
			else if (name.Contains("Large"))
			{
                min = 5;
                max = 10;
                minGold = 50;
                maxGold = 200;
                minGem = 10;
                maxGem = 50;
                percentGold = Random.Range(25, 50f);
                percentGem = Random.Range(10f, 20f);
                rateRare = 5;
                rateEpic = 3;
                rateLegend = 2;
            }
			else if (name.Contains("Super"))
			{
				min = 8;
				max = 15;
				minGold = 200;
				maxGold = 500;
				minGem = 50;
				maxGem = 100;
                percentGold = Random.Range(50f, 75f);
                percentGem = Random.Range(20f, 30f);
                rateRare = 3;
                rateEpic = 4;
                rateLegend = 3;
            }
			else
			{
				min = 12;
				max = 20;
				minGold = 500;
				maxGold = 1000;
				minGem = 15;
				maxGem = 30;
                percentGold = Random.Range(75f, 100f);
                percentGem = Random.Range(30f, 40f);
                rateRare = 1;
                rateEpic = 5;
                rateLegend = 4;
            }
			List<ResourcePool> pools = FindItemEditor.GetResourcePoolItems();
			ResourcePool poolGold = FindItemEditor.GetGoldPool();
			ResourcePool poolGem = FindItemEditor.GetGemPool();
			pools.Remove(poolGold);
			pools.Remove(poolGem);
			List<ChestRandomResource> list = new();
			list.Add(new ChestRandomResource(poolGold,new PickerPair(minGold, maxGold), new NumberPicker(NumberMode.Constant, percentGold)));
			list.Add(new ChestRandomResource(poolGem, new PickerPair(minGem, maxGem), new NumberPicker(NumberMode.Constant, percentGem)));
			for (int i = 0; i < pools.Count; i++)
			{
				var pool = pools[i];
				float factor = 10f;
				if (pool.name.Contains("Rare"))
				{
					factor = factor * rateRare;
				}
				else if (pool.name.Contains("Epic"))
				{
					factor = factor * rateEpic;
				}
				else
				{
					factor = factor * rateLegend;
				}
				var chestRandom = new ChestRandomResource(pools[i], new PickerPair(min, max), new NumberPicker(NumberMode.Constant, factor));
				list.Add(chestRandom);
			}
			chestRandomResources = list;
        }
#endif
    }
	[System.Serializable]
	public class ChestRandomResource : IChestRandom
	{
		[SerializeField] private ResourcePool resourcePool;
		[SerializeField] private PickerPair pickerPair;
		[SerializeField] private NumberPicker percentRate;
		public ChestRandomResource(ResourcePool resourcePool, PickerPair pickerPair, NumberPicker percentRate)
		{
			this.resourcePool = resourcePool;
			this.pickerPair = pickerPair;
			this.percentRate = percentRate;
		}
		IResourcePool IChestRandom.ResourcePool => resourcePool;
        INumber IChestRandom.Percent => percentRate.PickNumber();
        INumberPickerPair IChestRandom.PickerPair => pickerPair;
    }    
}
