using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Domain content wrapper for <see cref="LevelCatalog"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalogContent", menuName = "GoGo/Level Catalog Content")]
    public class LevelCatalogContent : AbstractScriptableContent
    {
        [SerializeField] private LevelCatalog _catalog;
        public LevelCatalog Catalog => _catalog;

        protected override void DoInject(IDomain domain)
        {
            base.DoInject(domain);
            if (_catalog != null)
                domain.Add(_catalog);
        }
    }
}
