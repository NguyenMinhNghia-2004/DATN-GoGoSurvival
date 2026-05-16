using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
#if FIREBASE_ENABLE
using Firebase.Analytics;
#endif
using UnityEngine;
public class FirebaseEvent : MonoBehaviour
{
    #region Static methob
    public static void SetUserProperty(string propertiesName, string value)
    {
#if FIREBASE_ENABLE
        FirebaseAnalytics.SetUserProperty(propertiesName, value);
#endif
    }
    public static void LogEvent(string eventName)
    {
#if FIREBASE_ENABLE
        FirebaseAnalytics.LogEvent(eventName);
#if UNITY_EDITOR
        Debug.Log($"<color=blue>FIREBASE EVENT:</color> {eventName}");
#endif
#endif
    }
    public static void LogEvent(string eventName, params Parameter[] parameters)
    {
#if FIREBASE_ENABLE
        FirebaseAnalytics.LogEvent(eventName, parameters);
#if UNITY_EDITOR
        StringBuilder parameterInfo = new StringBuilder();
        parameterInfo.Append($"{eventName}: ");
        parameterInfo.Append("\n");
        for (int i = 0; i < parameters.Length; i++)
        {
            parameterInfo.Append($"{(i + 1)}. {FirebaseHelper.GetParameterName(parameters[i])} = {FirebaseHelper.GetParameterValue(parameters[i])}");
            if (i < parameters.Length - 1)
                parameterInfo.Append("\n");
        }
        Debug.Log($"<color=blue>FIREBASE EVENT:</color> {parameterInfo.ToString()}");
#endif
#endif
    }
    #endregion
}
#if !FIREBASE_ENABLE
public class Parameter
{
}
#endif