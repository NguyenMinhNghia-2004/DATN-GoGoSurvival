using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// "Throw + ground patch" config (Molotov). Behavior throws a BombProjectile in a
    /// gravity arc; on land, instead of an instant AoE explosion it spawns a FirePatch
    /// MonoBehaviour that ticks damage over time within radius.
    ///
    /// <para>Different from <see cref="ZSkillBehaviorConfig_Bomb"/> in two ways:
    /// 1) it spawns a patch prefab on land, 2) the bomb itself does NOT explode.</para>
    /// </summary>
    public sealed class ZSkillBehaviorConfig_GroundPatch : ZSkillBehaviorConfig_Projectile
    {
        [Header("Ground Patch")]
        [Tooltip("Prefab with a FirePatch MonoBehaviour. Instantiated at the bomb's land position.")]
        [SerializeField] private GameObject _patchPrefab;

        [Tooltip("Duration of the fire patch (seconds).")]
        [SerializeField] private float _patchDuration = 3f;

        [Tooltip("DoT tick interval inside the patch (seconds).")]
        [SerializeField] private float _patchTickInterval = 0.4f;

        public GameObject PatchPrefab => _patchPrefab;
        public float PatchDuration => _patchDuration;
        public float PatchTickInterval => _patchTickInterval;

        public override IZSkillBehavior CreateBehavior(IZSkill skill)
            => SpawnOrAdd<ZSkillBehavior_GroundPatch, ZSkillBehaviorConfig_GroundPatch>(skill, this);
    }
}
