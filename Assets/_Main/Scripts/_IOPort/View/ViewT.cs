using UnityEngine;

namespace Luzart
{
    public abstract class ViewT<T> : View, IView<T>
    {
        [SerializeField] protected ViewChilding[] children;

        protected T _data;

        T IView<T>.DataT => _data;
        public T Data => _data;

        public void Setup(T data)
        {
            DoInternalSetup(data);
        }

        public void Teardown()
        {
            DoInternalTeardown();
        }

        void IView<T>.SetupT(T data)
        {
            DoInternalSetup(data);
        }

        protected override object DoGetDataObj()
        {
            return _data;
        }

        protected override void DoInternalSetup(object data)
        {
            try
            {
                if (data == null)
                {
                    throw new System.Exception($"Null data");
                }
                else if (data is not T dt)
                {
                    throw new System.Exception($"BAD type {data} must be {typeof(T)}");
                }
                else
                {
                    _data = dt;
                }

                OnSetup();

                if (children != null)
                {
                    for (int i = 0; i < children.Length; i++)
                    {
                        var c = children[i];
                        c.Setup(this);
                    }
                }
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"Error setup view {this} with data {data} \n Error : {e}", this);
#endif
                throw;
            }
        }

        protected sealed override void DoInternalTeardown()
        {
            if (children != null)
            {
                for (int i = children.Length - 1; i >= 0; i--)
                {
                    var c = children[i];
                    c.Teardown();
                }
            }

            OnTeardown();

            _data = default;
        }

        public virtual void Refresh() { }
        protected virtual void OnSetup() { }
        protected virtual void OnTeardown() { }
    }
}
