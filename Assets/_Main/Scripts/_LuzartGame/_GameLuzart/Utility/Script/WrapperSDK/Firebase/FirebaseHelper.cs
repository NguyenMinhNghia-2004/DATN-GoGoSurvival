#if FIREBASE_ENABLE
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Firebase.Analytics;
internal static class FirebaseHelper
{
    public static object GetParameterValue(Parameter parameter)
    {
        IntPtr paramPtr = GetNativePtr(parameter);
        ParameterData paramData = Marshal.PtrToStructure<ParameterData>(paramPtr);
        if (paramData.parameterType == 1)
        {
            return paramData.parameterValue;
        }
        else if (paramData.parameterType == 5)
        {
            return Marshal.PtrToStringAnsi(paramData.parameterValue);
        }
        return "NỜ Ô NÔ ( BẮN STRING OR INT ĐI )";
    }
    public static string GetParameterName(Parameter parameter)
    {
        IntPtr paramPtr = GetNativePtr(parameter);
        return GetStringFromIntPtr(paramPtr);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ParameterData
    {
        public IntPtr parameterName;
        public int parameterType;
        public IntPtr parameterValue;
    }
    private static string GetStringFromIntPtr(IntPtr ptr)
    {
        IntPtr stringPtr = Marshal.ReadIntPtr(ptr);
        return Marshal.PtrToStringAnsi(stringPtr);
    }
    private static IntPtr GetNativePtr(Parameter param)
    {
        FieldInfo field = typeof(Parameter).GetField("swigCPtr", BindingFlags.NonPublic | BindingFlags.Instance);
        object handleRef = field.GetValue(param);
        PropertyInfo prop = handleRef.GetType().GetProperty("Handle");
        return (IntPtr)prop.GetValue(handleRef);
    }
}
#endif