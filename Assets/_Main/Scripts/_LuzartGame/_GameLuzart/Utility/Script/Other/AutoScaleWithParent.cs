using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class AutoScaleWithParent : MonoBehaviour
    {
        [SerializeField] private bool isWidth = false;
        [SerializeField] private bool isHeight = true;
        [SerializeField] private RectTransform targetRectTransform;
        [SerializeField] private Mode mode = Mode.OnEnable;
        private void OnEnable()
        {
            if(mode == Mode.OnEnable)
                ApplyScale();
        }
        private void Start()
        {
            if (mode == Mode.Start)
                ApplyScale();
        }
        [ContextMenu("Apply Scale")]
        public void ApplyScale()
        {
            RectTransform rt = transform as RectTransform;
            Vector2 sizeDelta = rt.sizeDelta;
            Vector2 targetSizeDelta = targetRectTransform.sizeDelta;
            float localScale = 1f;
            if (isWidth)
            {
                localScale = targetSizeDelta.x / sizeDelta.x;
            }
            else if (isHeight)
            {
                localScale = targetSizeDelta.y / sizeDelta.y;
            }
            rt.localScale = new Vector3(localScale, localScale, localScale);
        }
        public enum Mode
        {
            OnEnable = 0,
            Start = 1,
            None = 2,
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if(targetRectTransform == null)
            {
                targetRectTransform = transform.parent as RectTransform;
            }
        }
#endif
    }
}
