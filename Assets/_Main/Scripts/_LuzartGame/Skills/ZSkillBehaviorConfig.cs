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

        [Tooltip("Optional behavior prefab. When assigned, ZSkill instantiates it as a child " +
                 "and binds (designer can add SpriteRenderer/particles/audio on the prefab). " +
                 "When null, a fresh child GameObject is created via AddComponent.")]
        [SerializeField] protected GameObject _behaviorPrefab;
        public GameObject BehaviorPrefab => _behaviorPrefab;

        /// <summary>Create a runtime behavior instance for <paramref name="skill"/>. Each
        /// concrete config knows which ZSkillBehavior_X type to instantiate. Subclasses call
        /// <see cref="SpawnOrAdd{TBehavior}"/> to either Instantiate the prefab or AddComponent
        /// on a fresh child GameObject of the skill's transform.</summary>
        public abstract IZSkillBehavior CreateBehavior(IZSkill skill);

        /// <summary>Helper for concrete config classes: spawns the behavior either from the
        /// optional `_behaviorPrefab` or by AddComponent on a new child GameObject under the
        /// skill's transform. Returns the behavior with Bind already called.</summary>
        protected TBehavior SpawnOrAdd<TBehavior, TCfg>(IZSkill skill, TCfg cfg)
            where TBehavior : ZSkillBehavior<TCfg>
            where TCfg : ZSkillBehaviorConfig
        {
            Transform parent = (skill is MonoBehaviour mb) ? mb.transform : null;
            GameObject go;
            if (_behaviorPrefab != null)
            {
                go = Object.Instantiate(_behaviorPrefab, parent, false);
                go.name = $"Behavior_{name}";
            }
            else
            {
                go = new GameObject($"Behavior_{name}");
                if (parent != null) go.transform.SetParent(parent, false);
            }
            var b = go.GetComponent<TBehavior>();
            if (b == null) b = go.AddComponent<TBehavior>();
            b.Bind(skill, cfg);
            return b;
        }
    }
}
