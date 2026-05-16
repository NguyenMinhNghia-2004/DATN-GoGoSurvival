using UnityEngine;
namespace Luzart
{
    [System.Serializable]
#if UNITY_EDITOR
    [UnityEditor.CanEditMultipleObjects]
#endif
    public class AbstractScriptableContent : ScriptableObject, IContent
    {
#if UNITY_EDITOR
        private void GenerateNewId()
        {
            _id = UnityEditor.GUID.Generate().ToString().Replace('-', '\0')[..8];
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        [SerializeField]
        protected string _id;
        public virtual string Id => _id;
        protected IDomain _domain;
        IDomain IContent.MyDomain => _domain;
        public IDomain MyDomain => _domain;
        void IContent.Initialize()
        {
            try
            {
                DoInitialize();
            }
            catch(System.Exception e)
            {
                Debug.LogError($"Initialize Bug : {e}");
            }
        }
        void IContent.Inject(IDomain domain)
        {
            DoInject(domain);
        }
        void IContent.Start()
        {
            DoStartContent();
        }
        void IContent.Stop()
        {
            DoStopContent();
        }
        void IContent.Terminate()
        {
            DoTerminate();
        }
        protected virtual void DoInitialize()
        {
        }
        protected virtual void DoInject(IDomain domain)
        {
            this._domain = domain;
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
#if UNITY_EDITOR
        protected virtual void OnEnable()
        {
            Editor_DoOnEnableScriptableObject();
        }
        protected virtual void Editor_DoOnEnableScriptableObject()
        {
            if (Application.isPlaying)
            {
                return;
            }
            if (!string.IsNullOrEmpty(_id))
            {
                return;
            }
            _id = UnityEditor.GUID.Generate().ToString().Replace('-', '\0')[..8];
            UnityEditor.EditorUtility.SetDirty(this);
        }
        protected void OnValidate()
        {
            Editor_DoOnValidateScriptableObject();
        }
        protected virtual void Editor_DoOnValidateScriptableObject()
        {
            if (Application.isPlaying)
            {
                return;
            }
            if (!string.IsNullOrEmpty(_id))
            {
                return;
            }
            _id = UnityEditor.GUID.Generate().ToString().Replace('-', '\0')[..8];
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}