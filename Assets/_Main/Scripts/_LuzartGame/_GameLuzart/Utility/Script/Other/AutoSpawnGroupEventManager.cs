//namespace Luzart
//{
//    using System.Collections;
//    using System.Collections.Generic;
//    using UnityEngine;
//    public class AutoSpawnGroupEventManager 
//    {
//        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//        public static void InitGroupMethod()
//        {
//            var groupEventManager = GameObject.Find("GroupEventManager");
//            if(groupEventManager != null)
//            {
//                return;
//            }
//            groupEventManager = Resources.Load<GameObject>("GroupEventManager");
//            GameObject obj2 = Object.Instantiate(groupEventManager);
//        }
//    }
//}
