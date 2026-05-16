using System;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public sealed class AssetBool_Combined : BaseAssetBool
    {
        [SerializeField] CombineMode combineMode = CombineMode.Any;
        [SerializeField] List<BaseAssetBool> bools;
        public IReadOnlyList<IBool> Bool => bools;
        protected override bool DoGetValue()
        {
            switch (combineMode)
            {
                case CombineMode.All:
                    return IsModeAll();
                case CombineMode.Any:
                    return IsModeAny();
            }
            return true;
        }
        private bool IsModeAny()
        {
            for (int i = 0; i < Bool.Count; i++)
            {
                if (Bool[i].Value)
                {
                    return true;
                }
            }
            return false;
        }
        private bool IsModeAll()
        {
            for (int i = 0; i < Bool.Count; i++)
            {
                if (!Bool[i].Value)
                {
                    return false;
                }
            }
            return true;
        }
        enum CombineMode
        {
            Any = 0,
            All = 1,
        }
    }
}
