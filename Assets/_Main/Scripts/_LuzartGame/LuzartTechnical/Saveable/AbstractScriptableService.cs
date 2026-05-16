using UnityEngine;
namespace Luzart
{
    public abstract class AbstractScriptableService : ScriptableObject, IService
    {
#if UNITY_EDITOR
        private void GenerateNewId()
        {
            id = UnityEditor.GUID.Generate().ToString().Replace('-', '\0')[..8];
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        [SerializeField] private string id;
        string IService.Id => id;
        protected IDomain _domain;
        IDomain IService.MyDomain => _domain;
        void IService.Inject(IDomain domain)
        {
            _domain = domain;
        }
        void IService.Initialize()
        {
            DoInitialize();
        }
        void IService.StartService()
        {
            DoStartContent();
        }
        void IService.StopService()
        {
            DoStopContent();
        }
        void IService.Terminate()
        {
            DoTerminate();
        }
        protected virtual void DoInitialize()
        {
        }
        protected virtual void DoStartContent()
        {
        }
        protected virtual void DoStopContent()
        {
        }
        protected virtual void DoTerminate()
        {
        }
    }
}