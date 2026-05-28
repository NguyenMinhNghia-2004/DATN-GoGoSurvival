using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Phase F foundation scaffold (Phase F.5). Replaces
    /// <c>DATNPlayerEntityAdapter</c> + the stats-skipping
    /// <c>DATNPlayerCharacter</c> shim.
    ///
    /// <para>When activated, creates a real <see cref="PlayerCharacter"/> from
    /// <see cref="StatsConfig"/> (so EquipmentManager + ZSkillUpgradeConfig
    /// modifiers actually drive in-game stats) and registers it via
    /// <c>domain.Add&lt;PlayerCharacter&gt;()</c>.</para>
    ///
    /// <para>Dormant until <c>MigrationFlags.UseLuzartPlayerEntityRoot</c> is
    /// true. Phase F.x overrides <c>DoInject</c> and <c>DoUpdate</c>.</para>
    /// </summary>
    public class LuzartPlayerEntityRoot : AbstractMonoBehaviorContent
    {
        [SerializeField] private StatsConfig _statsConfig;

        public StatsConfig StatsConfig => _statsConfig;

        // Phase F.x will override DoInject / DoUpdate here.
        // Keep base behaviour for now so adding the component is inert.
    }
}
