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

        private void Reset()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            CacheJoystickReflection();
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

        private void Update()
        {
            if (!TryGetFlags(out var flags) || !flags.UseLuzartPlayerController) return;
            // Note: legacy required Boolean.GameStart to be true to move. Phase F
            // hooks the framework GameController.MapReady property (already
            // bridged from UIManager.MapReady in Phase D).
            var gc = SceneRootManager.Instance?.Domain?.Get<GameController>();
            if (gc != null && !gc.MapReady) return;

            var v = ReadJoystick();
            if (_rb != null)
            {
                if (v.y != 0f)
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

            // Prefer framework HP path; fall back to legacy GameManager.Health.
            var character = ResolveCharacter();
            if (character != null && character.Stats != null)
            {
                var hp = character.Stats.GetRuntime(StatType.Runtime_HP);
                hp.Set(hp.Value - EnemyTouchDamage);
                return;
            }
            // Reach in via GameObject lookup (no hard ref to legacy assembly).
            var legacy = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (legacy != null) legacy.Health -= EnemyTouchDamage;
        }
    }
}
