using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    public class ItemDefinitionView : ViewT<ItemResourcePoolData>
    {
        [SerializeField] protected Image _imIcon;
        [SerializeField] protected TMP_Text _txt;

        [SerializeField] protected string _preStr = "";
        [SerializeField] protected string _endStr = "";

        protected override void OnSetup()
        {
            base.OnSetup();
            SetupElement();
        }

        protected virtual void SetupElement()
        {
            SetIcon();
            SetText();
        }

        protected virtual void SetIcon()
        {
            if (_imIcon == null)
            {
                return;
            }
            _imIcon.sprite = Data.ResourcePool.Definition.GetMainImage();
        }

        protected virtual void SetText()
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
