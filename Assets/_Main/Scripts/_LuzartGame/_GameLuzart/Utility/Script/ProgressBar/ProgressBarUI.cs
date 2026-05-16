namespace Luzart
{
    using DG.Tweening;
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    public class ProgressBarUI : UnityEngine.MonoBehaviour
    {
        public Image imFill;
        public virtual void SetSlider(float prePercent, float targetPercent, float time = 0, Action onDone = null, Action<float> actionUpdate = null)
        {
            DestroyPreProgress();
            prePercent = Mathf.Clamp01(prePercent);
            targetPercent = Mathf.Clamp01(targetPercent);
            if (prePercent == targetPercent || time <= 0)
            {
                imFill.fillAmount = targetPercent;
                onDone?.Invoke();
                return;
            }
            GameUtil.Instance.StartLerpValue(this, prePercent, targetPercent, time, (x) =>
            {
                imFill.fillAmount = x;
                actionUpdate?.Invoke(x);
            }, onDone);
        }
        public virtual Tween SetSliderTween(float prePercent, float targetPercent, float time, Action onDone = null , Action<float> actionUpdate = null)
        {
            DestroyPreProgress();
            prePercent = Mathf.Clamp01(prePercent);
            targetPercent = Mathf.Clamp01(targetPercent);
            if (prePercent == targetPercent || time <= 0)
            {
                imFill.fillAmount = targetPercent;
                onDone?.Invoke();
                return DOVirtual.DelayedCall(0,null);
            }
            else
            {
                return DOVirtual.Float(prePercent, targetPercent, time, (x) =>
                {
                    imFill.fillAmount = x;
                    actionUpdate?.Invoke(x);
                }).OnComplete(()=> onDone?.Invoke()).SetId(this);
            }
        }
        private void OnDisable()
        {
            DestroyPreProgress();
        }
        protected virtual void DestroyPreProgress()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return; // Trong Editor mode, không cần xử lý
#endif
            this.DOKill(true);
            GameUtil.Instance.StopAllCoroutinesForBehaviour(this);
        }
    }
}
