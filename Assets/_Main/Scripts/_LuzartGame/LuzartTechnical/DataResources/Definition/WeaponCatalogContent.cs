using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Domain content wrapper for <see cref="WeaponCatalog"/>. Registers the
    /// catalog SO so framework code can resolve it via
    /// <c>domain.Get&lt;WeaponCatalog&gt;()</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponCatalogContent", menuName = "GoGo/Weapon Catalog Content")]
    public class WeaponCatalogContent : AbstractScriptableContent
    {
        [SerializeField] private WeaponCatalog _catalog;
        public WeaponCatalog Catalog => _catalog;

        protected override void DoInject(IDomain domain)
        {
            base.DoInject(domain);
            if (_catalog != null)
                domain.Add(_catalog);
        }
    }
}
