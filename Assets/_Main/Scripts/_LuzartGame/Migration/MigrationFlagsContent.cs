using UnityEngine;

namespace Luzart.Migration
{
    /// <summary>
    /// Domain content wrapper for <see cref="MigrationFlags"/>. Registers the
    /// flag SO into the framework Domain so any code can resolve it via
    /// <c>domain.Get&lt;MigrationFlags&gt;()</c>.
    ///
    /// Wire this asset into <c>_GameBoot.DomainContentLoader.contents</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "MigrationFlagsContent", menuName = "GoGo/Migration Flags Content")]
    public class MigrationFlagsContent : AbstractScriptableContent
    {
        [SerializeField] private MigrationFlags _flags;

        public MigrationFlags Flags => _flags;

        protected override void DoInject(IDomain domain)
        {
            base.DoInject(domain);
            if (_flags != null)
                domain.Add(_flags);
        }
    }
}
