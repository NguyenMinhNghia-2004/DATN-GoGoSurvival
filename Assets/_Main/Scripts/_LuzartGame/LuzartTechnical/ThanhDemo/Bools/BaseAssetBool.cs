using Luzart;
using System;
namespace Luzart
{
    public abstract class BaseAssetBool : AbstractScriptableContent, IBool
    {
        bool IBool.Value => DoGetValue();
        private Action<IBool> _onValueChange = null;
        event Action<IBool> IBool.Changed
        {
            add
            {
                _onValueChange += value;
            }
            remove
            {
                _onValueChange -= value;
            }
        }
        protected abstract bool DoGetValue();
    }
}
