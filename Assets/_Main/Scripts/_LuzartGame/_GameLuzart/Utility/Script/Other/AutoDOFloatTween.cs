using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class AutoDOFloatTween : MonoBehaviour
{
    public UnityEvent<float> unityEvent;
    [Header("Tween Settings")]
    public float startValue;
    public float endValue;
    public float duration;
    public Ease ease;
    public int loop = 0;
    public LoopType loopType = LoopType.Restart;
    public float delay = 0.5f;
    // Start is called before the first frame update
    private void OnEnable()
    {
        Sequence sequence = DOTween.Sequence();
        // Thêm đoạn DOVirtual vào sequence
        sequence.Append(DOVirtual.Float(startValue, endValue, duration, (value) =>
        {
            unityEvent?.Invoke(value);
        })
        .SetEase(ease)
        .SetId(this));
        // Thêm độ trễ sau mỗi vòng lặp
        sequence.AppendInterval(delay); // Thời gian trễ 1 giây (có thể thay đổi)
        sequence.SetLoops(loop, loopType);
    }
    private void OnDisable()
    {
        DOTween.Kill(this);
    }
}
