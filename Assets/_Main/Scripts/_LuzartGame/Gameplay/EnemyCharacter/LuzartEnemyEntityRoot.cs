using System.Collections;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Phase F replacement for the legacy <c>EnemyManager</c> MonoBehaviour +
    /// <c>DATNEnemyEntityAdapter</c> bridge on the Zombie prefab.
    ///
    /// <para>Flag-gated by <see cref="Migration.MigrationFlags.UseLuzartEnemyEntityRoot"/>.
    /// When OFF (default): every callback returns early — legacy stack drives the
    /// enemy. When ON: this component auto-disables the legacy <c>EnemyManager</c>
    /// + <c>DATNEnemyEntityAdapter</c> on the same GameObject (to avoid double
    /// damage/HP) and takes over movement + HP + death + drops.</para>
    ///
    /// <para>HP / speed scaling still read from the legacy <see cref="EnemyData"/>
    /// SO (already wired on Zombie prefab). Phase post-migration can author a
    /// proper <see cref="EnemyDefinition"/> for stat-config parity.</para>
    /// </summary>
    public class LuzartEnemyEntityRoot : MonoBehaviour
    {
        // W3 nuke: legacy EnemyData asset SO + class deleted. Stats now come from
        // _defaultHP / _defaultMoveSpeed inline until a Luzart-native enemy stats config lands.
        [SerializeField] private float _defaultHP = 100f;
        [SerializeField] private float _defaultMoveSpeed = 2f;
        [Tooltip("Hold position once within this distance of the player (melee). Re-evaluated " +
                 "every frame, so enemies resume chasing the instant the player moves away.")]
        [SerializeField] private float _stopFollowDistance = 0.6f;

        [Header("Death drops (set by Phase F.F-attach)")]
        [SerializeField] private GameObject _bloodLocalisationParent;
        [SerializeField] private GameObject _blueDiamond;
        [SerializeField] private GameObject _redDiamond;
        [SerializeField] private GameObject _greenDiamond;

        // Visual + audio cached at boot
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private AudioSource _audioSource;
        private Transform _playerTransform;

        // Framework character (the IEntity in Domain)
        private LuzartEnemyCharacter _character;
        private EntityManager _entityManager;

        // State
        private bool _active;
        private bool _isDead;
        private float _currentHP;
        private float _maxHP;
        private int _diamondType;

        private void Awake()
        {
            var srm = SceneRootManager.Instance;
            if (srm == null || srm.Domain == null) return;
            var flags = srm.Domain.Get<Migration.MigrationFlags>();
            if (flags == null || !flags.UseLuzartEnemyEntityRoot) return;

            _active = true;

            // F.G cleanup: legacy EnemyManager + DATNEnemyEntityAdapter components have
            // been removed from the Zombie prefab. Nothing legacy to disable.

            // Cache visual refs (visual freeze: prefab structure unchanged).
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) _playerTransform = playerGo.transform;

            // Init HP from EnemyData per current framework wave.
            int wave = ResolveWave();
            _maxHP = _defaultHP; // W3 nuke: legacy EnemyData removed, no per-wave HP scaling for now.
            _currentHP = _maxHP;
            _diamondType = Random.Range(1, 4); // 1..3 inclusive

            // Register framework entity.
            _entityManager = srm.Domain.Get<EntityManager>();
            _character = new LuzartEnemyCharacter(name + "_" + GetInstanceID());
            _character.Inject(srm.Domain);
            _character.Initialize();
            _character.Transform.SetPosition(transform.position);
            _character.Start();
            _entityManager?.Add(_character);

            // Bridge: lets TargetProvider's Physics2D query resolve hit Collider2D
            // back to this enemy entity. Zombie.prefab already carries CircleCollider2D —
            // EntityRef is a tiny component that maps it to _character.
            var refComp = GetComponent<EntityRef>();
            if (refComp == null) refComp = gameObject.AddComponent<EntityRef>();
            refComp.Entity = _character;
        }

        private int ResolveWave()
        {
            var gc = SceneRootManager.Instance?.Domain?.Get<GameController>();
            return gc != null ? Mathf.Max(1, gc.IndexWave.Value + 1) : 1;
        }

        private void Update()
        {
            if (!_active || _isDead || _character == null) return;
            _character.Transform.SetPosition(transform.position);
            _character.OnUpdate(Time.deltaTime);

            if (_playerTransform == null) return;

            // Re-evaluate distance every frame — NO sticky follow flag. The old code set
            // _followPlayer=false on OnTriggerEnter2D(Player) and only re-enabled it on
            // OnTriggerExit2D; a missed/late trigger-exit (common while the player runs in a
            // Survivor.io game) left enemies frozen far from the player. Now: close the gap
            // until melee range, then hold; resume the instant the player moves away.
            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist > _stopFollowDistance)
            {
                float speed = _defaultMoveSpeed; // W3 nuke: legacy EnemyData removed.
                transform.position = Vector2.MoveTowards(
                    transform.position, _playerTransform.position, speed * Time.deltaTime);
            }
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = _playerTransform.position.x < transform.position.x;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active || _isDead) return;

            // Backward-compat with legacy weapon tags (Bolt/ball/Fire/Spiner) —
            // 1-shot kill. Phase F.G will replace this with damage-from-Stats.ATK.
            if (other.CompareTag("Bolt") || other.CompareTag("ball")
                || other.CompareTag("Fire") || other.CompareTag("Spiner"))
            {
                TakeDamage(_maxHP);
            }
            else if (other.CompareTag("Player"))
            {
                // Melee-contact SFX only. Movement no longer gates on this trigger (see Update):
                // the follow/hold decision is a per-frame distance check, so a missed trigger-exit
                // can't freeze the enemy anymore.
                if (_audioSource != null) _audioSource.Play();
            }
        }

        /// <summary>
        /// Public damage API used by future ZSkillBehavior_* impls (Phase F.G).
        /// Legacy weapon tags also flow through here via OnTriggerEnter2D.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (_isDead) return;
            _currentHP -= damage;
            if (_currentHP <= 0f) Die();
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            if (_animator != null) _animator.Play("ZombieDeath");

            // Route kill count to framework GameController.AddEnemyDead.
            var gc = SceneRootManager.Instance?.Domain?.Get<GameController>();
            if (gc != null) gc.AddEnemyDead(1);

            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            yield return new WaitForSeconds(0.5f);
            DropDiamond();
            Destroy(gameObject);
        }

        private void DropDiamond()
        {
            // Resolve drop parent — Inspector ref first, fallback to scene's BloodManager
            // GO (legacy EnemyManager uses the same pattern via GameObject.Find).
            var parent = _bloodLocalisationParent;
            if (parent == null) parent = GameObject.Find("BloodManager");
            if (parent == null) return;

            GameObject prefab = _diamondType switch
            {
                1 => _blueDiamond,
                2 => _redDiamond,
                3 => _greenDiamond,
                _ => _blueDiamond
            };
            if (prefab == null) return;
            var d = Instantiate(prefab, transform.position, transform.rotation);
            d.transform.SetParent(parent.transform);
        }

        private void OnDestroy()
        {
            if (!_active || _character == null) return;
            _entityManager?.Remove(_character);
            _character.Stop();
            _character.Terminate();
            _character = null;
        }
    }

    /// <summary>
    /// Lightweight EnemyCharacter mirroring <c>DATNEnemyCharacter</c>: skips heavy
    /// behaviors (Render/Animation/Collider) because the Unity prefab already
    /// drives those. Stats + Transform are set up by EntityBase.Inject.
    /// </summary>
    public class LuzartEnemyCharacter : EnemyCharacter
    {
        public LuzartEnemyCharacter(string id) : base(null, id) { }

        public override void Initialize()
        {
            // Intentionally skip base.Initialize(). Spatial-hash registration is no
            // longer needed — TargetProvider now uses Unity Physics2D + the
            // EntityRef MonoBehaviour wired on the Zombie.prefab GameObject.
        }

        public override void Start()
        {
            // Skip HP-changed subscription — LuzartEnemyEntityRoot.TakeDamage owns
            // visual HP for now via legacy SpriteRenderer + animator.
        }
    }
}
