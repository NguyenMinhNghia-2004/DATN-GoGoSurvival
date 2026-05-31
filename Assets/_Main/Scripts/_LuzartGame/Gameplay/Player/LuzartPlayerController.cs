using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Phase F replacement for DATN's <c>PlayerManager</c> + <c>JoystickManager</c>.
    /// Reads joystick → drives Rigidbody2D → sets animation params → applies
    /// enemy-touch damage via the framework <see cref="StatsBehavior"/> (with
    /// a fallback to legacy <c>GameManager.Health</c> if the framework path is
    /// unavailable).
    ///
    /// <para>Strangler-fig: gated by <see cref="Migration.MigrationFlags.UseLuzartPlayerController"/>.
    /// While off, every method is a no-op so the legacy MonoBehaviours next to it
    /// keep driving the player.</para>
    ///
    /// <para>Wiring expectations:
    /// <list type="bullet">
    /// <item><c>_rb</c>: Rigidbody2D on Player GO (auto-resolved in Reset).</item>
    /// <item><c>_animator</c>: Animator under Player/Body (auto-resolved in Reset).</item>
    /// <item><c>_joystick</c>: the legacy <c>movementJoystick</c> in the joystick UI prefab.
    /// Typed as MonoBehaviour to avoid an asmdef hard ref to the legacy script.
    /// Resolved at runtime via reflection on <c>joystickVec</c> field.</item>
    /// <item><c>_gunForFlip</c>: optional Gun GO whose localScale mirrors player facing.
    /// Phase F.G removes this once the legacy weapon visual goes.</item>
    /// </list>
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class LuzartPlayerController : MonoBehaviour
    {
        private const float PlayerSpeedFallback = 4f;
        private const float EnemyTouchDamage = 0.5f;
        private const string AnimMoving = "CharacterBody";
        private const string AnimIdle = "0";

        [Header("Refs")]
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Animator _animator;
        [Tooltip("Drag the joystick UI GameObject (legacy movementJoystick component). Reflection reads its joystickVec.")]
        [SerializeField] private MonoBehaviour _joystick;
        [Tooltip("Legacy Gun GO whose localScale mirrors player facing flip. Optional.")]
        [SerializeField] private GameObject _gunForFlip;

        [Header("Tuning")]
        [SerializeField] private float _gunFlipScale = 0.2446888f;

        private System.Reflection.FieldInfo _joystickVecField;
        private Migration.MigrationFlags _flags;
        private LuzartPlayerCharacter _character;
        private ClassicModeController _classicMode;

        private void Reset()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            CacheJoystickReflection();
            DisableLegacyOnFlagOn();
        }

        /// <summary>F.G cleanup: legacy JoystickManager + PlayerManager have been
        /// deleted from the project. This method is now a no-op kept for parity
        /// with OnEnable's call site (which OS-level can't be easily refactored).</summary>
        private void DisableLegacyOnFlagOn()
        {
            // no-op
        }

        private void CacheJoystickReflection()
        {
            if (_joystick == null) { _joystickVecField = null; return; }
            _joystickVecField = _joystick.GetType()
                .GetField("joystickVec",
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic);
        }

        private bool TryGetFlags(out Migration.MigrationFlags flags)
        {
            if (_flags != null) { flags = _flags; return true; }
            var srm = SceneRootManager.Instance;
            _flags = srm != null ? srm.Domain?.Get<Migration.MigrationFlags>() : null;
            flags = _flags;
            return flags != null;
        }

        /// <summary>Lazy-resolve ClassicModeController from Domain. Cached on first hit.
        /// Returns false on the very first frames before SceneRootManager has bootstrapped,
        /// in which case the Update gate falls through to the legacy MapReady check.</summary>
        private bool TryGetClassicMode(out ClassicModeController mode)
        {
            if (_classicMode != null) { mode = _classicMode; return true; }
            var srm = SceneRootManager.Instance;
            _classicMode = srm != null ? srm.Domain?.Get<ClassicModeController>() : null;
            mode = _classicMode;
            return mode != null;
        }

        private LuzartPlayerCharacter ResolveCharacter()
        {
            if (_character != null) return _character;
            var srm = SceneRootManager.Instance;
            var pc = srm != null ? srm.Domain?.Get<PlayerCharacter>() : null;
            _character = pc as LuzartPlayerCharacter;
            return _character;
        }

        private Vector2 ReadJoystick()
        {
            if (_joystick == null || _joystickVecField == null) return Vector2.zero;
            var v = _joystickVecField.GetValue(_joystick);
            return v is Vector2 vec ? vec : Vector2.zero;
        }

        private float ResolveSpeed()
        {
            var character = ResolveCharacter();
            if (character != null && character.Stats != null)
            {
                var statSpeed = character.Stats.Get(StatType.Speed);
                if (statSpeed != null) return (float)statSpeed.Value;
            }
            return PlayerSpeedFallback;
        }

        private bool _dbgLoggedActive;
        private bool _dbgLoggedFirstMove;
        private int _dbgFlagsCheckCount;
        private void Update()
        {
            if (!TryGetFlags(out var flags) || !flags.UseLuzartPlayerController)
            {
                if (_dbgFlagsCheckCount++ < 3) Debug.Log($"[DBG-CHAIN] E: PlayerController gate: flags={(flags==null?"NULL":"OK use="+flags.UseLuzartPlayerController)}");
                return;
            }
            // Note: legacy required Boolean.GameStart to be true to move. Phase F
            // hooks the framework GameController.MapReady property (already
            // bridged from UIManager.MapReady in Phase D).
            var gc = SceneRootManager.Instance?.Domain?.Get<GameController>();
            if (gc != null && !gc.MapReady) return;

            // Run-state gate: when ClassicMode is Idle (pre-Play) or Ended (post-Win/Lose),
            // input must not drive the Rigidbody — otherwise the player keeps moving under
            // the Win/Lose/MainMenu screens. This MonoBehaviour isn't a Domain Content so it
            // can't receive IRunParticipant callbacks; check state per frame instead.
            if (TryGetClassicMode(out var mode) && !mode.IsPlaying)
            {
                if (_rb != null) _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (!_dbgLoggedActive)
            {
                _dbgLoggedActive = true;
                Debug.Log($"[DBG-CHAIN] E: PlayerController gates OPEN: gc.MapReady={gc?.MapReady}, mode.IsPlaying={mode?.IsPlaying}, _joystick={(_joystick==null?"NULL":_joystick.GetType().Name)}, _joystickVecField={(_joystickVecField==null?"NULL":"OK")}, _rb={(_rb==null?"NULL":"OK")}");
            }

            var v = ReadJoystick();
            // Log once when v transitions from zero → nonzero (i.e., user starts dragging)
            if (!_dbgLoggedFirstMove && v.sqrMagnitude > 0.0001f)
            {
                _dbgLoggedFirstMove = true;
                Debug.Log($"[DBG-CHAIN] E: First nonzero joystick v=({v.x:F2},{v.y:F2}) speed={ResolveSpeed():F1}");
            }
            if (_rb != null)
            {
                // F.G fix: legacy gated on v.y != 0 only — pure horizontal joystick
                // (y=0, x!=0) fell into the else branch and zeroed velocity. Player
                // appeared stuck whenever the player pushed straight left or right.
                if (v.sqrMagnitude > 0f)
                {
                    float speed = ResolveSpeed();
                    _rb.linearVelocity = new Vector2(v.x * speed, v.y * speed);
                }
                else
                {
                    _rb.linearVelocity = Vector2.zero;
                }
            }

            UpdateAnimation(v);
            UpdateFacing(v);
        }

        private void UpdateAnimation(Vector2 v)
        {
            if (_animator == null) return;
            bool moving = _rb != null && _rb.linearVelocity.sqrMagnitude > 0f;
            _animator.Play(moving ? AnimMoving : AnimIdle);
        }

        private void UpdateFacing(Vector2 v)
        {
            if (v.x < 0f)
            {
                var s = transform.localScale;
                transform.localScale = new Vector3(-1f, s.y, s.z);
                if (_gunForFlip != null && _gunForFlip.activeSelf)
                    _gunForFlip.transform.localScale = new Vector3(-_gunFlipScale, _gunFlipScale, _gunFlipScale);
            }
            else if (v.x > 0f)
            {
                var s = transform.localScale;
                transform.localScale = new Vector3(1f, s.y, s.z);
                if (_gunForFlip != null && _gunForFlip.activeSelf)
                    _gunForFlip.transform.localScale = new Vector3(_gunFlipScale, _gunFlipScale, _gunFlipScale);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!TryGetFlags(out var flags) || !flags.UseLuzartPlayerController) return;
            if (!other.CompareTag("Enemy")) return;

            // F.G cleanup: framework HP only — legacy GameManager.Health fallback removed.
            var character = ResolveCharacter();
            if (character == null || character.Stats == null) return;
            var hp = character.Stats.GetRuntime(StatType.Runtime_HP);
            hp.Set(hp.Value - EnemyTouchDamage);
        }
    }
}
