#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	[CustomPropertyDrawer(typeof(FlagFieldAttribute))]
	class FlagFieldDrawerDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Type type = fieldInfo.FieldType;
			if (type.IsArray)
				type = type.GetElementType();
			else if (type.IsGenericType)
				type = type.GetGenericArguments()[0];

			if (!type.IsSubclassOf(typeof(Enum)))
			{
				EditorGUI.LabelField(position, label.text, "Field should be an Enum");
				return;
			}

			int oldValueInt = property.enumValueFlag;
			Enum oldValue = (Enum)Enum.ToObject(type, property.enumValueFlag);
			Enum newValue = EditorGUI.EnumFlagsField(position, label, oldValue);
			property.enumValueFlag = Convert.ToInt32(newValue);

			if (oldValueInt != property.enumValueFlag)
				property.serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif