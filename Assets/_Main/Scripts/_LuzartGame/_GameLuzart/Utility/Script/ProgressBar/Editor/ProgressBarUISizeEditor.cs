#if UNITY_EDITOR
using UnityEditor;
namespace Luzart
{
    [CustomEditor(typeof(ProgressBarUISize))]
    public class ProgressBarUISizeEditor : Editor
    {
        private float previewValue = 1f; // Không cần tồn tại trong script gốc
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ProgressBarUISize script = (ProgressBarUISize)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            previewValue = EditorGUILayout.Slider("Fill Amount", previewValue, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                script.SetSlider(previewValue, previewValue, 0f, null); // Gọi update ngay
                EditorApplication.QueuePlayerLoopUpdate();
            }
            script.width = script.rtContain.sizeDelta.x;
            script.height = script.rtFill.sizeDelta.y;
        }
    }
}
#endif