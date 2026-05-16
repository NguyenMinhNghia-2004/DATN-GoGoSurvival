namespace Luzart
{
    using Luzart;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    public class HomeSequenceAction : UnityEngine.MonoBehaviour
    {
        public SequenceActionEvent[] sequenceActionEvents;
        private void Start()
        {
            int length = sequenceActionEvents.Length;
            GameUtil.Instance.WaitAFrame(() =>
            {
                for (int i = 0; i < length; i++)
                {
                    int index = i;
                    sequenceActionEvents[index]?.PreInit();
                }
            });
            List<Action<Action>> listStep = new List<Action<Action>>();
            for (int i = 0; i < length; i++)
            {
                int index = i;
                var instance = sequenceActionEvents[index];
                listStep.Add(next => instance?.Init(next));
            }
            GameUtil.Instance.WaitFrame(2,() =>
            {
                GameUtil.StepToStep(listStep.ToArray());
            });
        }
    }
}
