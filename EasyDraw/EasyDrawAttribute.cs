using System;
using UnityEngine;

# if UNITY_EDITOR 
using UnityEditor;
#endif

namespace EasyEditor
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)]
    public class EasyDrawAttribute : PropertyAttribute
    {
        public int index = 0;
        public EasyDrawAttribute(int index = 0) => this.index = index;
    }
}

# if UNITY_EDITOR
namespace EasyEditor.Internal
{
    [CustomPropertyDrawer(typeof(EasyDrawAttribute))]
    public class EasyDrawAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (EasyMonoBehaviourEditor.extraUIDrawing)
                EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (EasyMonoBehaviourEditor.extraUIDrawing)
                return EditorGUI.GetPropertyHeight(property, label, true);
            else
                return -EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
#endif