using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    public class ItemConfigView : ViewT<ItemConfigAndAmountData>
    {
        [SerializeField] private Image _imIcon;
        [SerializeField] private Image _imBg;
        [SerializeField] private TMP_Text _txt;

        [SerializeField] private string _preStr;
        [SerializeField] private string _endStr;

        private RaritySpriteResolver _itemIconResolver;
        private ItemConfig _itemConfig;

        protected override void OnSetup()
        {
            base.OnSetup();
            this._itemConfig = Data.ItemConfig;
            SetupElement();
        }

        public void SetupElement()
        {
            SetBackground();
            SetIcon();
            SetText();
        }

        private void SetBackground()
        {
            if (_imBg == null)
            {
                return;
            }
            if (!_imBg.gameObject.activeSelf)
            {
                return;
            }
            var allVisualResolver = SceneRootManager.Instance._domain.GetAll<IVisualResolver>();
            foreach (var resolver in allVisualResolver)
            {
                if (resolver is RaritySpriteResolver raritySpriteResolver)
                {
                    _itemIconResolver = raritySpriteResolver;
                    break;
                }
            }
            if (_itemIconResolver != null)
                _imBg.sprite = _itemIconResolver.GetSpriteByRarity(_itemConfig.Rarity);
        }

        private void SetIcon()
        {
            if (_imIcon == null)
            {
                return;
            }
            _imIcon.sprite = _itemConfig.Sprite;
        }

        private void SetText()
        {
            if (_txt == null)
            {
                return;
            }
            if (Data.Amount == -1)
            {
                _txt.gameObject.SetActive(false);
                return;
            }
            _txt.gameObject.SetActive(true);
            _txt.text = $"{_preStr}{Data.Amount}{_endStr}";
        }
    }
}
