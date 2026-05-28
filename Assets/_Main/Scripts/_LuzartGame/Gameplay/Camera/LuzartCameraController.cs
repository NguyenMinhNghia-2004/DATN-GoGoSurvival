using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Phase F replacement for the legacy <c>CameraController</c>. Smooth-follows
    /// the player and ramps FOV while the Spinner weapon is active.
    ///
    /// <para>Resolves the player position via <see cref="SceneRootManager"/>.Domain
    /// → <see cref="PlayerCharacter"/>.Transform first; falls back to the legacy
    /// Inspector ref so this can be attached alongside the legacy CameraController
    /// during cutover. Gated by <see cref="Migration.MigrationFlags.UseLuzartPlayerEntityRoot"/>
    /// because before that flag flips, the PlayerCharacter in Domain is the
    /// stats-skipping DATNPlayerCharacter (still position-valid, OK to follow).</para>
    ///
    /// <para>Flag-free for now — when both legacy CameraController + this script
    /// are on the Camera GO, both run; Phase F.x cutover commit disables the
    /// legacy one.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class LuzartCameraController : MonoBehaviour
    {
        [Header("Follow target (Inspector fallback)")]
        [SerializeField] private GameObject _legacyPlayerRef;

        [Header("Spinner FOV ramp (legacy gameplay feel)")]
        [SerializeField] private GameObject _spinner;

        [Header("Follow tuning")]
        [SerializeField] private float _speedMove = 0.15f;
        [SerializeField] private Vector3 _offset = Vector3.zero;
        [SerializeField] private float _fovRampMax = 85f;
        [SerializeField] private float _fovRampPerFrame = 0.5f;

        private Vector3 _velocityCache;
        private Camera _camera;
        private bool _fovRampActive = true;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private bool TryGetPlayerPosition(out Vector3 position)
        {
            var srm = SceneRootManager.Instance;
            var player = srm != null ? srm.Domain?.Get<PlayerCharacter>() : null;
            if (player != null && player.Transform != null && player.Transform.Position != null)
            {
                // Framework Position is Vector2; preserve camera's current Z.
                var p = player.Transform.Position.Value;
                position = new Vector3(p.x, p.y, transform.position.z);
                return true;
            }
            if (_legacyPlayerRef != null)
            {
                position = _legacyPlayerRef.transform.position;
                return true;
            }
            position = Vector3.zero;
            return false;
        }

        private void LateUpdate()
        {
            if (TryGetPlayerPosition(out var target))
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position, target + _offset, ref _velocityCache, _speedMove);
            }
        }

        private void Update()
        {
            if (_camera == null || _spinner == null || !_fovRampActive) return;
            if (_spinner.activeSelf)
            {
                _camera.fieldOfView += _fovRampPerFrame;
                if (_camera.fieldOfView >= _fovRampMax)
                {
                    _camera.fieldOfView = _fovRampMax;
                    _fovRampActive = false;
                }
            }
        }
    }
}
