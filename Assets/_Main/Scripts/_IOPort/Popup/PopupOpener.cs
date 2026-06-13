using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Luzart
{
    public sealed class PopupOpener : ViewT<IContent>
    {
        [SerializeField] Transform popupsParent;

        private Dictionary<(PopupLayer layer, Type type), List<Popup>> activePopups = new Dictionary<(PopupLayer, Type), List<Popup>>();

        protected override void OnSetup()
        {
            base.OnSetup();

            var popupService = Data.MyDomain.GetService<PopupService>();
            popupService.OnPopupShouldShow += PopupService_OnPopupRequested;
            popupService.OnPopupShouldHide += PopupService_OnPopupHideRequested;
            popupService.OnAllPopupsShouldHide += PopupService_OnAllPopupsHideRequested;
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            var popupService = Data.MyDomain.GetService<PopupService>();
            popupService.OnPopupShouldShow -= PopupService_OnPopupRequested;
            popupService.OnPopupShouldHide -= PopupService_OnPopupHideRequested;
            popupService.OnAllPopupsShouldHide -= PopupService_OnAllPopupsHideRequested;
        }

        private void PopupService_OnPopupRequested(PopupLayer popupLayer, Type popupType, object popupData)
        {
            var popupPrefabResolver = Data.MyDomain.Get<PopupVisualResolver>();
            var newView = popupPrefabResolver.ResolvePopupPrefab(popupLayer, popupType);
            var instantiatedPopup = GameObject.Instantiate(newView, popupsParent);

            instantiatedPopup.SetPopupLayer(popupLayer);
            instantiatedPopup.Setup(popupData);

            var key = (popupLayer, popupType);
            if (!activePopups.ContainsKey(key))
                activePopups[key] = new List<Popup>();

            activePopups[key].Add(instantiatedPopup);

            instantiatedPopup.OnPopupClosed += (popup) => OnPopupClosed(popup, key);
        }

        private void OnPopupClosed(Popup popup, (PopupLayer layer, Type type) key)
        {
            if (activePopups.TryGetValue(key, out var popups))
            {
                popups.Remove(popup);
                if (popups.Count == 0)
                {
                    activePopups.Remove(key);
                }
            }
        }

        private void PopupService_OnPopupHideRequested(PopupLayer popupLayer, Type popupType)
        {
            var key = (popupLayer, popupType);
            if (activePopups.TryGetValue(key, out var popups))
            {
                foreach (var popup in popups.ToList())
                {
                    if (popup != null)
                    {
                        popup.Teardown();
                        GameObject.Destroy(popup.gameObject);
                    }
                }
                activePopups[key].Clear();
            }
        }

        private void PopupService_OnAllPopupsHideRequested()
        {
            foreach (var kvp in activePopups.ToList())
            {
                foreach (var popup in kvp.Value.ToList())
                {
                    if (popup != null)
                    {
                        popup.Teardown();
                        GameObject.Destroy(popup.gameObject);
                    }
                }
                kvp.Value.Clear();
            }
            activePopups.Clear();
        }

        public int GetActivePopupCount()
        {
            int count = 0;
            foreach (var kvp in activePopups)
            {
                count += kvp.Value.Count(p => p != null);
            }
            return count;
        }

        public bool IsPopupActive<T>(PopupLayer layer) where T : Popup
        {
            var key = (layer, typeof(T));
            return activePopups.ContainsKey(key) && activePopups[key].Any(p => p != null);
        }

        public T GetActivePopup<T>(PopupLayer layer) where T : Popup
        {
            var key = (layer, typeof(T));
            if (activePopups.TryGetValue(key, out var popups))
            {
                return popups.FirstOrDefault(p => p != null) as T;
            }
            return null;
        }
    }
}
