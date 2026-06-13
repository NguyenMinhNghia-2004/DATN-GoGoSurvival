using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Luzart
{
    public class RaritySpriteResolver : AbstractScriptableContent, IVisualResolver
    {
        [SerializeField] private List<SpriteRarity> _listSpriteRarity;

        private Dictionary<ERarity, Sprite> _dictSpriteRarity;

        protected override void DoInitialize()
        {
            base.DoInitialize();
            _dictSpriteRarity = _listSpriteRarity.ToDictionary(x => x.eRarity, x => x.sprite);
        }

        public Sprite GetSpriteByRarity(ERarity eRarity)
        {
            if (_dictSpriteRarity != null && _dictSpriteRarity.TryGetValue(eRarity, out var sprite))
            {
                return sprite;
            }
            return null;
        }

        public Sprite GetSprite(object data)
        {
            if (data is ERarity eRarity)
            {
                return GetSpriteByRarity(eRarity);
            }
            return null;
        }
    }

    [Serializable]
    public struct SpriteRarity
    {
        public ERarity eRarity;
        public Sprite sprite;
    }

    public interface IVisualResolver
    {
        public Sprite GetSprite(object data);
    }
}
