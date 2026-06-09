using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Registers <see cref="AbstractScriptableContent"/> assets (LevelConfig, StatsConfig, ...)
    /// into the runtime <see cref="Domain"/> so framework systems can resolve them via
    /// <c>_domain.Get&lt;T&gt;()</c>.
    ///
    /// SceneRootManager auto-discovers <see cref="AbstractMonoBehaviorContent"/> instances
    /// (scene MonoBehaviours) but NOT SO assets — those need an explicit registrar.
    /// Attach this on `_GameBoot`, fill <see cref="contents"/> with SO refs in Inspector.
    ///
    /// Execution order: must run BEFORE consumers (EnemySpawnerManager, GameController...).
    /// </summary>
    [DefaultExecutionOrder(-900)] // Right after SceneRootManager (-1000).
    public class DomainContentLoader : AbstractMonoBehaviorContent
    {
        [Header("SO assets to register in Domain")]
        [SerializeField] private List<AbstractScriptableContent> contents = new List<AbstractScriptableContent>();

        [Header("SO services to register in Domain (e.g. PopupService, SaveService)")]
        [SerializeField] private List<AbstractScriptableService> services = new List<AbstractScriptableService>();

        [Header("Camera registration")]
        [Tooltip("Register a Camera under id 'MainCamera' so behaviors can resolve it via _domain.Get<Camera>(\"MainCamera\").")]
        [SerializeField] private Camera mainCamera;

        public override void DoInject(IDomain domain)
        {
            base.DoInject(domain);
            for (int i = 0; i < contents.Count; i++)
            {
                var c = contents[i];
                if (c == null) continue;
                IContent ic = c;
                // Inject the SO so AbstractScriptableContent._domain is set.
                ic.Inject(domain);
                domain.Add(ic, ic.Id);
            }
            // Register services (PopupService, SaveService, ...). AddService injects them.
            for (int i = 0; i < services.Count; i++)
            {
                var s = services[i];
                if (s == null) continue;
                IService isv = s;
                domain.AddService(isv, isv.Id);
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
                domain.Add(mainCamera, "MainCamera");
        }

        public override void DoInitialize()
        {
            base.DoInitialize();
            for (int i = 0; i < contents.Count; i++)
            {
                var c = contents[i];
                if (c == null) continue;
                ((IContent)c).Initialize();
            }
        }

        public override void DoStart()
        {
            base.DoStart();
            // Start services first (e.g. SaveService.LoadAllData) so content Start sees loaded data.
            for (int i = 0; i < services.Count; i++)
            {
                var s = services[i];
                if (s == null) continue;
                ((IService)s).Initialize();
                ((IService)s).StartService();
            }
            for (int i = 0; i < contents.Count; i++)
            {
                var c = contents[i];
                if (c == null) continue;
                ((IContent)c).Start();
            }
        }

        public override void DoStop()
        {
            base.DoStop();
            for (int i = 0; i < contents.Count; i++)
            {
                var c = contents[i];
                if (c == null) continue;
                ((IContent)c).Stop();
            }
        }

        public override void DoTerminate()
        {
            base.DoTerminate();
            for (int i = 0; i < contents.Count; i++)
            {
                var c = contents[i];
                if (c == null) continue;
                ((IContent)c).Terminate();
            }
        }
    }
}
