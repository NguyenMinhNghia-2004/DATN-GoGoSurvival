using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    public class ItemViewWithLevelInventory : MonoBehaviour
    {
        [SerializeField] private Image imIcon;
        [SerializeField] private Image imBg;
        [SerializeField] private TMP_Text txtLevel;
        [SerializeField] private TMP_Text txtStat;

        private RaritySpriteResolver _itemIconResolver;
        private ItemConfig _itemConfig;
        private int _level = -1;

        public void Setup(ItemConfig itemConfig, int level = -1)
        {
            this._itemConfig = itemConfig;
            this._level = level;
            SetBackground();
            SetIcon();
            SetLevel();
            SetStat();
        }

        private void SetStat()
        {
            if (txtStat == null) return;
            ItemStatUtil.GetAtkHp(_itemConfig, out double atk, out double hp);
            txtStat.text = $"ATK +{ItemStatUtil.Fmt(atk)}\nHP +{ItemStatUtil.Fmt(hp)}";
        }

        private void SetBackground()
        {
            if (imBg == null)
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
                imBg.sprite = _itemIconResolver.GetSpriteByRarity(_itemConfig.Rarity);
        }

        private void SetIcon()
        {
            if (imIcon == null)
            {
                return;
            }
            imIcon.sprite = _itemConfig.Sprite;
        }

        private void SetLevel()
        {
            if (txtLevel == null)
            {
                return;
            }
            // NOTE: IO_Training source has this inverted (_level < 0), which hides the
            // level whenever a real level is supplied. Corrected so levels are visible.
            bool isShowLevel = _level >= 0;
            txtLevel.gameObject.SetActive(isShowLevel);
            txtLevel.text = $"Lv.{_level}";
        }
    }
}
