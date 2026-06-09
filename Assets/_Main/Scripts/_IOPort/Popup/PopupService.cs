using System;

namespace Luzart
{
    public class PopupService : AbstractScriptableService
    {
        public event System.Action<PopupLayer, Type, object> OnPopupShouldShow;
        public event System.Action<PopupLayer, Type> OnPopupShouldHide;
        public event System.Action OnAllPopupsShouldHide;

        public void ShowPopup<TPopup, TData>(PopupLayer popupLayer, TData popupData) where TPopup : Popup<TData>
        {
            OnPopupShouldShow?.Invoke(popupLayer, typeof(TPopup), popupData);
        }

        public void ShowPopup(PopupLayer popupLayer, Type popupType, object popupData)
        {
            OnPopupShouldShow?.Invoke(popupLayer, popupType, popupData);
        }

        public void HidePopup<TPopup>(PopupLayer popupLayer) where TPopup : Popup
        {
            OnPopupShouldHide?.Invoke(popupLayer, typeof(TPopup));
        }

        public void HidePopup(PopupLayer popupLayer, Type popupType)
        {
            OnPopupShouldHide?.Invoke(popupLayer, popupType);
        }

        public void HideAllPopups()
        {
            OnAllPopupsShouldHide?.Invoke();
        }
    }
}
