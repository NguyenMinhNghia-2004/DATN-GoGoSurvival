using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    public abstract class Popup : MonoBehaviour
    {
        [SerializeField] protected Button closeButton;
        protected PopupLayer popupLayer;

        public event System.Action<Popup> OnPopupClosed;

        public abstract void Setup(object obj);

        protected virtual void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        protected virtual void OnCloseButtonClicked()
        {
            HideSelf();
        }

        public virtual void HideSelf()
        {
            OnPopupClosed?.Invoke(this);

            Teardown();
            Destroy(gameObject);
        }

        public virtual void Teardown()
        {
            OnPopupClosed = null;
        }

        public void SetPopupLayer(PopupLayer layer)
        {
            popupLayer = layer;
        }

        public PopupLayer GetPopupLayer() => popupLayer;

        public virtual bool CanBeClosedByBackAction() => true;
    }

    public abstract class Popup<T> : Popup
    {
        [SerializeField] ViewT<T> mainView;
        T _data;
        public T Data => _data;

        public override sealed void Setup(object obj)
        {
            this.Setup((T)obj);
        }

        public virtual void Setup(T data)
        {
            this._data = data;
            mainView?.Setup(data);
        }

        public override void Teardown()
        {
            mainView?.Teardown();
            base.Teardown();
        }
    }
}
