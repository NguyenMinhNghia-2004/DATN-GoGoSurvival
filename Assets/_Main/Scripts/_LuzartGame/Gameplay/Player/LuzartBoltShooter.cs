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

        /// <summary>F.G.5/F.G.7: once we own bolt firing, silence the legacy
        /// PlayerManager entirely. Its remaining responsibilities (UI follow
        /// SmoothDamp on an already-deleted UI ref, Death GO toggle on a HUD
        /// that no longer reads it) are obsolete in the Luzart-only flow.</summary>
        private void DisableLegacyOnFlagOn()
        {
            if (!TryGetFlags(out var flags) || !flags.UseLuzartPlayerController) return;
            var pm = GetComponent<PlayerManager>();
            if (pm != null) pm.enabled = false;
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
            if (!TryGetFlags(out var flags) || !flags.UseLuzartPlayerController) return;

            // Same MapReady gate as LuzartPlayerController.
            var gc = SceneRootManager.Instance?.Domain?.Get<GameController>();
            if (gc != null && !gc.MapReady) return;

            // Don't fire after death.
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
