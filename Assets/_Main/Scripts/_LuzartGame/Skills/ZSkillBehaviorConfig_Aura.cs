using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Sustained-AoE config (Forcefield). Queries enemies within RangeFind every tickInterval
    /// và apply ATK damage. <see cref="VisualPrefab"/> = vòng tròn hiển thị aura, follow player
    /// + auto-scale theo stat RangeFind.
    /// </summary>
    public sealed class ZSkillBehaviorConfig_Aura : ZSkillBehaviorConfig
    {
        [Header("Aura")]
        [Tooltip("Seconds between damage ticks. Lower = more frequent damage but higher CPU.")]
        [SerializeField] private float _tickInterval = 0.5f;

        [Tooltip("Visual prefab cho vòng aura (vd Pf_Forcefield). Spawn 1 instance follow player, " +
                 "scale theo RangeFind. Để null = aura vô hình.")]
        [SerializeField] private GameObject _visualPrefab;

        [Tooltip("Sprite của visualPrefab tính theo Unity world units (PPU 256 → 2.56 unit). " +
                 "Tỷ lệ để 1 unit prefab phủ 1 unit RangeFind. Tăng = aura visual to hơn so với hit range.")]
        [SerializeField] private float _visualUnitSize = 2.56f;

        public float TickInterval => _tickInterval;
        public GameObject VisualPrefab => _visualPrefab;
        public float VisualUnitSize => _visualUnitSize;

        public override IZSkillBehavior CreateBehavior(IZSkill skill)
            => SpawnOrAdd<ZSkillBehavior_Aura, ZSkillBehaviorConfig_Aura>(skill, this);
    }
}
