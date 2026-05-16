namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    public class DontDestroyOnLoad : UnityEngine.MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
