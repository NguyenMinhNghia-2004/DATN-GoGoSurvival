using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Survivor.io-style per-skill GameObject. Hosts a single <see cref="ZSkillConfig"/>
    /// and ticks its <see cref="IZSkillBehavior"/> list every frame.
    ///
    /// <para>Phase F foundation scaffold (Phase F.2). Created as a child of
    /// Player/Skills/ when the player picks a skill in the level-up popup.
    /// Phase F.7-F.18 will port the 12 legacy weapons into ZSkillBehavior_*
    /// implementations consumed via <c>ZSkillConfig.BehaviorConfigs</c>.</para>
    /// </summary>
    public class ZSkillRuntime : MonoBehaviour
    {
        [SerializeField] private ZSkillConfig _config;
        private IEntity _owner;
        private readonly List<IZSkillBehavior> _behaviors = new();
        private bool _bound;

        public ZSkillConfig Config => _config;
        public IEntity Owner => _owner;
        public IReadOnlyList<IZSkillBehavior> Behaviors => _behaviors;

        /// <summary>Bind to an owning entity + config. Called once after Instantiate.</summary>
        public void Bind(IEntity owner, ZSkillConfig config)
        {
            if (_bound)
            {
                Debug.LogWarning($"[ZSkillRuntime] Already bound: {name}");
                return;
            }
            _owner = owner;
            _config = config;
            // Phase F.7+ will instantiate concrete IZSkillBehavior from
            // config.BehaviorConfigs here. For now an empty list is fine —
            // Update is a no-op until real behaviors land.
            _bound = true;
        }

        private void Update()
        {
            if (!_bound) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < _behaviors.Count; i++)
            {
                _behaviors[i]?.Update(dt);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _behaviors.Count; i++)
            {
                (_behaviors[i] as System.IDisposable)?.Dispose();
            }
            _behaviors.Clear();
        }
    }
}
