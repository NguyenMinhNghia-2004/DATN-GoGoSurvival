namespace Luzart
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Rendering;
    public class CalculateAutoSize : UnityEngine.MonoBehaviour
    {
        [Header("A")]
        public float pixelWidth1 = 850;
        public float pixelHeight1 = 2160;
        public float c1 = 15f;
        [Header("B")]
        public float pixelWidth2 = 1080;
        public float pixelHeight2 = 1920;
        public float c2 = 20f;
        [Header("Clamp")]
        public float clampMin = 15;
        public float claimMax = 20;
        public UnityEvent<float> actionSetFloat;
    #if false // ODIN_INSPECTOR
    #endif
        private float a;
    #if false // ODIN_INSPECTOR
    #endif
        private float b;
    #if false // ODIN_INSPECTOR
    #endif
        private float value;
        void Awake()
        {
            float a1 = pixelHeight1 / pixelWidth1;
            float a2 = pixelHeight2 / pixelWidth2;
            a = (c1 - c2) / (a1 - a2);
            b = c1 - a1 * a;
            float pixelCurrent = (float)Screen.height/(float)Screen.width;
            value = pixelCurrent * a + b;
            float valueClamp = Mathf.Clamp(value, clampMin, claimMax);
            actionSetFloat?.Invoke(valueClamp);
        }
    }
}
