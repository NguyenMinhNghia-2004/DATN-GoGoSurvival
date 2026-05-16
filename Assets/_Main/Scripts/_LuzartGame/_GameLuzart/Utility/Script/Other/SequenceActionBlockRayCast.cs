using System;
using System.Collections;
using System.Collections.Generic;
using Luzart;
using UnityEngine;
public class SequenceActionBlockRayCast  : SequenceActionEvent
{
    public bool blockRayCast = true;
    public GameObject obBlock;
    public override void Init(Action callback)
    {
        obBlock?.SetActive(blockRayCast);
        callback?.Invoke();
    }
    public override void PreInit() { }
}
