using UnityEngine;
namespace Luzart
{
    public abstract class AbstractMonoBehaviorContent : MonoBehaviour, IContent
    {
        [SerializeField]
        protected string _id;
        string IContent.Id => _id == null ? this.GetType().Name : _id;
        protected IDomain _domain;
        IDomain IContent.MyDomain => _domain;
        protected bool _isInitialized = false;
        protected bool _isStarted = false;
        void IContent.Initialize()
        {
            DoInitialize();
            this._isInitialized = true;
        }
        void  IContent.Inject(IDomain domain)
        {
            DoInject(domain);
        }
        void IContent.Start()
        {
            DoStart();
            _isStarted = true;
        }
        void IContent.Stop()
        {
            DoStop();
            _isStarted = false;
        }
        void IContent.Terminate()
        {
            DoTerminate();
            _isInitialized = false;
        }
        public virtual void DoTerminate()
        {
        }
        public virtual void DoStart()
        {
        }
        public virtual void DoStop()
        {
        }
        public virtual void DoInject(IDomain domain)
        {
            this._domain = domain;
        }
        public virtual void DoInitialize()
        {
        }
        private void Update()
        {
            if(_isStarted && _isInitialized)
            {
                DoUpdate(Time.deltaTime);
            }
        }
        protected virtual void DoUpdate(float dt)
        {
        }
        private void OnValidate()
        {
            _id = name;
        }
    }
}