using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public abstract class ZSkillBehaviorConfig : AbstractScriptableContent
    {
        [SerializeField] protected SearchTargetType _searchTargetType = SearchTargetType.Nearest;
        public SearchTargetType SearchTargetType => _searchTargetType;
        [SerializeField] protected List<AssetStatDefinition> skillBehaviorStats;
        public IReadOnlyList<AssetStatDefinition> SkillBehaviorStats => skillBehaviorStats;
        public abstract IZSkillBehavior CreateBehavior(IZSkill skill);
    }
}