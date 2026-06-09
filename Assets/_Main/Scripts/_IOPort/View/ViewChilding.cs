using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Luzart
{
    [System.Serializable]
    public class ViewChilding
    {
        [SerializeField] string path;
        [SerializeField] View view;

        public void Setup(IView parentView)
        {
            var parentViewType = parentView?.GetType();
            var member = parentViewType.GetMember(path, BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();
            if (member == null)
            {
                throw new System.Exception($"No such member {path} in {parentViewType}");
            }

            var childData = (object)default;
            if (member is FieldInfo fi)
            {
                childData = fi.GetValue(parentView);
            }
            else if (member is PropertyInfo pi)
            {
                childData = pi.GetValue(parentView);
            }
            else if (member is MethodInfo mi)
            {
                childData = mi.Invoke(parentView, null);
            }
            else
            {
                throw new System.Exception();
            }
            ((IView)view).Setup(childData);
        }

        public void Teardown()
        {
            ((IView)view).Teardown();
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ViewChilding))]
        class _Drawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);
                var contentRect = EditorGUI.PrefixLabel(position, label);
                var pathRect = new Rect(contentRect) { width = contentRect.width * 0.5f };
                var objRect = new Rect(contentRect) { xMin = pathRect.xMax + 1 };
                DrawPath(pathRect, property.FindPropertyRelative(nameof(path)));
                EditorGUI.PropertyField(objRect, property.FindPropertyRelative(nameof(view)), GUIContent.none);
                EditorGUI.EndProperty();
            }

            void DrawPath(Rect pathRect, SerializedProperty pathProp)
            {
                if (EditorGUI.DropdownButton(pathRect, new GUIContent(pathProp.stringValue), FocusType.Passive))
                {
                    var viewObj = pathProp.serializedObject.targetObject;
                    var viewObjType = viewObj.GetType();

                    var menu = new GenericMenu();
                    var members = viewObjType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    foreach (var m in members)
                    {
                        if (m is PropertyInfo)
                        {
                            var mName = m.Name;
                            menu.AddItem(new GUIContent(mName), false, () =>
                            {
                                ModifyDetachedProperty(pathProp, mName);
                            });
                        }
                    }
                    menu.DropDown(pathRect);
                }
            }

            void ModifyDetachedProperty(SerializedProperty prop, string value)
            {
                string propPath = prop.propertyPath;
                SerializedObject so = prop.serializedObject;
                so.UpdateIfRequiredOrScript();
                so.FindProperty(propPath).stringValue = value;
                so.ApplyModifiedProperties();
            }
        }
#endif
    }
}
