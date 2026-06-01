using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// F.G.5 — Phase F replacement for <c>PlayerManager.HitBolts</c>. Auto-spawns
    /// a Bolt prefab on a fixed cooldown when LuzartPlayerController owns the
    /// player. Mirrors legacy logic 1:1 (instantiate at spawn-point, parent to
    /// ContainerWap, optional SFX) but reads its enable from MigrationFlags
    /// instead of <c>Bool.GameStart</c> + <c>GameManager.AvailabelWeapon</c>.
    ///
    /// <para>Gated by <see cref="Migration.MigrationFlags.UseLuzartPlayerController"/>.
    /// When OFF, this component is a no-op and PlayerManager.HitBolts runs as
    /// before.</para>
    ///
    /// <para>Cooldown defaults to 1s (matches the legacy reload bar implied
    /// rhythm). Bolt prefab + spawn point are reusing the same scene assets as
    /// the legacy code so visual fidelity is identical.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class LuzartBoltShooter : MonoBehaviour
    {
        [Header("Refs (drag the same assets PlayerManager uses)")]
        [Tooltip("Bolt prefab to instantiate.")]
        [SerializeField] private GameObject _boltPrefab;
        [Tooltip("Spawn point GameObject (legacy SpawenShoot). When wired we use this; " +
                 "otherwise we spawn at this transform.position. Legacy PM kept SpawenShoot " +
                 "glued to the player so the two are equivalent.")]
        [SerializeField] private GameObject _spawnPoint;
        [Tooltip("Parent GameObject for spawned bolts (legacy ContainerWap). Optional — " +
                 "without one the bolt becomes a scene root.")]
        [SerializeField] private GameObject _container;
        [Tooltip("Optional SFX to play on fire.")]
        [SerializeField] private AudioSource _fireSfx;

        [Header("Tuning")]
        [Tooltip("Seconds between bolts. Legacy used a reload bar with ~1s fill — keep parity.")]
        [SerializeField] private float _cooldown = 1f;

        private Migration.MigrationFlags _flags;
        private float _nextFireAt;

        private void OnEnable()
        {
            _nextFireAt = Time.time + _cooldown;
            DisableLegacyOnFlagOn();
        }

        /// <summary>F.G cleanup: legacy PlayerManager has been deleted from
        /// the project; nothing to disable. Kept as a no-op for OnEnable parity.</summary>
        private void DisableLegacyOnFlagOn()
        {
            // no-op
        }

        private bool TryGetFlags(out Migration.MigrationFlags flags)
        {
            if (_flags != null) { flags = _flags; return true; }
            var srm = SceneRootManager.Instance;
            _flags = srm != null ? srm.Domain?.Get<Migration.MigrationFlags>() : null;
            flags = _flags;
            return flags != null;
        }

        private void Update()
        {
            // SUPERSEDED 2026-06-01 — when LuzartPlayerEntityRoot is active, skill firing is
            // handled by ZSkillRuntime + ZSkillBehavior_CreateProjectile (the Luzart skill
            // pipeline). This MonoBehaviour was the Phase-F bridge that spawned the legacy
            // `Wapeon` prefab — its controller script was deleted in the W-nuke, leaving the
            // bolt as a stationary "Missing Script" GameObject. The user's symptom of
            // "kunai chỉ instantiate xong đứng yên" was this legacy bolt, NOT the Luzart
            // projectile. Disable when the new pipeline is wired.
            if (!TryGetFlags(out var flags) || !flags.UseLuzartPlayerController) return;
            if (flags.UseLuzartPlayerEntityRoot) return;  // skill pipeline owns firing now

            var gc = SceneRootManager.Instance?.Domain?.Get<GameController>();
            if (gc != null && !gc.MapReady) return;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null && !rb.simulated) return;

            if (Time.time < _nextFireAt) return;
            _nextFireAt = Time.time + _cooldown;

            FireOne();
        }

        private void FireOne()
        {
            if (_boltPrefab == null) return;
            Vector3 pos = _spawnPoint != null ? _spawnPoint.transform.position : transform.position;
            var go = Instantiate(_boltPrefab, pos, Quaternion.identity);
            if (_container != null) go.transform.SetParent(_container.transform, true);
            if (_fireSfx != null) _fireSfx.Play();
        }
    }
}
