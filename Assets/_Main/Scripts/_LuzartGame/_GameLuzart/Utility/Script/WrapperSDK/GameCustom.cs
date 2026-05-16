namespace Luzart
{
    //using BG_Library.Common;
    //using Sirenix.OdinInspector;
    //using System.Collections;
    //using System.Collections.Generic;
    //using BG_Library.NET;
    //using UnityEngine;
    //using Luzart;
    //public class GameCustom : MonoBehaviour
    //{
    //     public CustomConfig RemoteConfigCustom;
    //    public string remoteConfigJson = "";
    //    public static GameCustom Ins;
    //    private void Awake()
    //    {
    //        if (Ins == null)
    //            Ins = this;
    //        else
    //            Destroy(gameObject);
    //        DontDestroyOnLoad(gameObject);
    //        RemoteConfig.OnFetchComplete += Event_OnFetchComplete;
    //    }
    //    private void Start()
    //    {
    //        RemoteConfigCustom = JsonTool.DeserializeObject<CustomConfig>(NetConfigsSO.Ins.CustomConfigsDefault);
    //    }
    //    private void OnDestroy()
    //    {
    //        RemoteConfig.OnFetchComplete -= Event_OnFetchComplete;
    //    }
    //    private void Event_OnFetchComplete()
    //    {
    //        remoteConfigJson = RemoteConfig.Ins.custom_config;
    //        RemoteConfigCustom = JsonTool.DeserializeObject<CustomConfig>(RemoteConfig.Ins.custom_config);
    //        if (RemoteConfigCustom != null)
    //        {
    //            if(RemoteConfigCustom.dbEvent != null)
    //            {
    //                EventManager.Instance.SaveDataEventFirebase(RemoteConfigCustom.dbEvent);
    //            }
    //        }
    //    }
    //#if UNITY_EDITOR
    //    [Space]
    //    [Header("Tool To Json")]
    //    public CustomConfig configToolToJson;
    //    public string json;
    //    
    //    public void ToConfigCustomJson()
    //    {
    //        json = JsonUtility.ToJson(configToolToJson);
    //    }
    //    
    //    public void ToConfigCustom()
    //    {
    //        configToolToJson = JsonUtility.FromJson<CustomConfig>(json);
    //    }
    //#endif
    //}
    //[System.Serializable]
    //public class CustomConfig
    //{
    //    public int levelShowAdsInter = 4;
    //    public DB_EventJsonFirebase dbEvent;
    //}
}
