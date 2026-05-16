using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
	public class MappingItemForSkill : AbstractScriptableContent
	{
		[SerializeField]
		private List<SkillItemPair> skillItemPairs;
		public IReadOnlyList<SkillItemPair> SkillItemPairs => skillItemPairs;
		public ZSkillConfig GetSkillWithItem(ItemConfig itemConfig)
		{
			for (int i = 0; i < skillItemPairs.Count; i++)
			{
				var skillItemPair = skillItemPairs[i];
				if(skillItemPair.ItemConfig == itemConfig)
				{
					return skillItemPair.ZSkillConfig;
				}
			}
			return null;
		}
	}
	[System.Serializable]
	public struct SkillItemPair
	{
		public ItemConfig ItemConfig;
		public ZSkillConfig ZSkillConfig;
	}
}
