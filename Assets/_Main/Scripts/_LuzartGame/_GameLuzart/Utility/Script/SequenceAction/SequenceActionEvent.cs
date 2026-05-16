namespace Luzart
{
    using Luzart;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    public abstract class SequenceActionEvent : UnityEngine.MonoBehaviour
    {
        public abstract void PreInit();
        public abstract void Init(Action callback);
    }
}
