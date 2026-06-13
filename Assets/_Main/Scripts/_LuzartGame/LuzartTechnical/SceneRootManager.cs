using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Scene-root bootstrapper for the Luzart framework.
    /// Owns the <see cref="Domain"/> instance and orchestrates IContent lifecycle:
    /// <c>Inject → Initialize → Start (per frame Update) → Stop → Terminate</c>.
    ///
    /// Setup in scene:
    ///   1. Attach to a root GameObject (e.g., `_GameBoot`).
    ///   2. Drag child <see cref="AbstractMonoBehaviorContent"/> instances into <see cref="contents"/>.
    ///   3. They will be Inject + Initialize + Start when scene loads, in array order.
    ///
    /// Auto-discovery: any AbstractMonoBehaviorContent in scene NOT in <see cref="contents"/>
    /// is auto-added during Awake (set <see cref="autoDiscover"/> = true).
    ///
    /// Access from anywhere: <c>SceneRootManager.Instance._domain.Get&lt;FooService&gt;()</c>
    /// or <c>SceneRootManager.Instance.Domain</c> (typed accessor).
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Run before any other AbstractMonoBehaviorContent.
    public class SceneRootManager : MonoBehaviour
    {
        public static SceneRootManager Instance { get; private set; }

        [Header("Bootstrap")]
        [SerializeField] private bool autoDiscover = true;
        [SerializeField] private bool logVerbose = false;

        [Header("Manually-ordered content (run in this order)")]
        [SerializeField] private AbstractMonoBehaviorContent[] contents;

        // The Domain instance. Public field per framework convention
        // (UpgradeSkillManager already uses `SceneRootManager.Instance._domain.GetService<...>()`).
        public Domain _domain;

        public IDomain Domain => _domain;

        private readonly List<AbstractMonoBehaviorContent> _runtimeContents = new List<AbstractMonoBehaviorContent>();
        private bool _started = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SceneRootManager] Duplicate instance — destroying " + name);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _domain = new Domain();

            // 1) Seed declared contents.
            if (contents != null)
            {
                for (int i = 0; i < contents.Length; i++)
                {
                    var c = contents[i];
                    if (c == null) continue;
                    _runtimeContents.Add(c);
                }
            }

            // 2) Auto-discover.
            if (autoDiscover)
            {
                var all = FindObjectsOfType<AbstractMonoBehaviorContent>(includeInactive: false);
                for (int i = 0; i < all.Length; i++)
                {
                    var c = all[i];
                    if (c == null) continue;
                    if (_runtimeContents.Contains(c)) continue;
                    _runtimeContents.Add(c);
                }
            }

            // 3) Inject + Add to domain.
            for (int i = 0; i < _runtimeContents.Count; i++)
            {
                var c = _runtimeContents[i];
                if (c == null) continue;
                IContent ic = c;
                _domain.Add(ic, ic.Id);
                if (logVerbose) Debug.Log("[SceneRootManager] Registered content: " + c.GetType().Name + " (" + ic.Id + ")");
            }

            // 4) Initialize all.
            _domain.InitializeAll();
        }

        private void Start()
        {
            if (_domain == null) return;
            _domain.StartAll();
            _started = true;
            if (logVerbose) Debug.Log("[SceneRootManager] All contents started. Count=" + _runtimeContents.Count);
        }

        private void Update()
        {
            // EntityManager + content handle their own Update via AbstractMonoBehaviorContent.DoUpdate,
            // but pure-POCO IEntity instances created by EntityBluePrint are driven via _playerCharacter.OnUpdate.
            // Nothing to do here at the SceneRootManager level for now.
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                if (_domain != null)
                {
                    _domain.StopAll();
                    _domain.TerminateAll();
                }
                Instance = null;
            }
        }

        /// <summary>Manually register a runtime-spawned content (e.g., dynamically created EntityBluePrint).</summary>
        public void RegisterContent(AbstractMonoBehaviorContent c)
        {
            if (c == null) return;
            if (_runtimeContents.Contains(c)) return;
            _runtimeContents.Add(c);
            IContent ic = c;
            _domain.Add(ic, ic.Id);
            ic.Inject(_domain);
            ic.Initialize();
            if (_started) ic.Start();
        }

        /// <summary>Tear down + unregister a runtime-spawned content (the inverse of
        /// <see cref="RegisterContent"/>). Drives Stop → Terminate, removes it from the Domain
        /// (so <c>Domain.Get&lt;T&gt;()</c> stops returning the now-destroyed instance) and from
        /// the runtime list. Call this BEFORE destroying the GameObject that hosts the content.</summary>
        public void UnregisterContent(AbstractMonoBehaviorContent c)
        {
            if (c == null) return;
            IContent ic = c;
            if (_started) ic.Stop();
            ic.Terminate();
            if (_domain != null) _domain.Remove<AbstractMonoBehaviorContent>(c, ic.Id);
            _runtimeContents.Remove(c);
        }
    }
}
