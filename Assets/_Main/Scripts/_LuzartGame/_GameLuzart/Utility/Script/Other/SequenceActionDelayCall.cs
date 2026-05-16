using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luzart;
using System;
public class SequenceActionDelayCall : SequenceActionEvent
{
    public float timeDelay = 0.3f;
    public override void Init(Action callback)
    {
        GameUtil.Instance.WaitAndDo(timeDelay, CallDelay);
        void CallDelay()
        {
            callback?.Invoke();
        }
    }
    public override void PreInit()
    {
    }
}
