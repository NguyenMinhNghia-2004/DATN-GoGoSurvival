using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace Luzart
{
	public class SkillControllerPlayerSO : AbstractScriptableContent
	{
		private SkillControllerBehavior _skillControllerBehavior;
		private List<ZSkillConfig> _availableSkillConfigs = new List<ZSkillConfig>();
		private List<ZSkill> _zSkills = new List<ZSkill>();
    }
}
