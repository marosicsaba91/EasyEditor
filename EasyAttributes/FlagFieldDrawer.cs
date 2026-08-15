#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace EasyEditor.Editor
{
	[CustomPropertyDrawer(typeof(FlagFieldAttribute))]
	class FlagFieldDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Type type = GetEnumType();

			if (type is not { IsEnum: true })
			{
				EditorGUI.LabelField(position, label.text, "Field should be an Enum");
				return;
			}
			DrawFlagField(position, property, label);
		}

		Type GetEnumType()
		{
			Type type = fieldInfo.FieldType;
			if (type.IsArray)
				return type.GetElementType();

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
				return type.GetGenericArguments()[0];

			return type;
		}
		
		static void DrawFlagField(Rect position, SerializedProperty property, GUIContent label)
		{
			int oldValueInt = property.enumValueFlag;
			property.enumValueFlag = EditorGUI.MaskField(position, label, oldValueInt, property.enumDisplayNames);

			if (oldValueInt != property.enumValueFlag)
				property.serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif
