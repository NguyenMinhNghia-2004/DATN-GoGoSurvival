using UnityEngine;

namespace Luzart
{
    public abstract class View : MonoBehaviour, IView
    {
        object IView.DataObj => DoGetDataObj();

        void IView.Setup(object data)
        {
            DoInternalSetup(data);
        }

        void IView.Teardown()
        {
            DoInternalTeardown();
        }

        protected abstract object DoGetDataObj();
        protected abstract void DoInternalSetup(object data);
        protected abstract void DoInternalTeardown();
    }
}
