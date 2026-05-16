using System;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Enemy data SO — **prefab-first design**. The prefab IS the source of truth for visual
    /// (sprite, scale, animator, collider). This SO only adds gameplay data (HP scaling, skills, etc.)
    /// that doesn't naturally live on a prefab.
    ///
    /// Field inherited from <see cref="EntityConfigScriptableObject"/> (sprite/material/scale) are
    /// IGNORED by the spawner — kept only for backward-compat. Always set <see cref="Prefab"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Luzart/Enemy/EnemyDefinition", order = 1)]
#if UNITY_EDITOR
    [UnityEditor.CanEditMultipleObjects]
#endif
    public class EnemyDefinition: EntityConfigScriptableObject
    {
        [Header("Prefab (REQUIRED — drag DATN GameObject prefab here)")]
        [Tooltip("Enemy GameObject prefab. Visual + scale + sprite + animator + collider are all baked into the prefab; " +
                 "this SO only adds gameplay-data side (waves, skills, HP scaling).")]
        [SerializeField] private GameObject prefab;

        [Header("Optional gameplay data")]
        [SerializeField] private AnimationConfig animationConfig;
        [SerializeField] private List<ZSkillConfig> skillConfig;

        public GameObject Prefab => prefab;
        public AnimationConfig AnimationConfig => animationConfig;
        public List<ZSkillConfig> SkillConfig => skillConfig;
    }
}