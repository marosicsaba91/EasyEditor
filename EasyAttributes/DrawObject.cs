using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field)]
public class DrawObject : PropertyAttribute { }

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(DrawObject))]
public class DrawObjectDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if (property.propertyType != SerializedPropertyType.ObjectReference)
		{
			EditorGUI.LabelField(position, label.text, "Use DrawObject with Object");
			return;
		}

		// Foldout:
		Rect foldoutRect = new(position) { width = 50 };
		property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);

		EditorGUI.indentLevel++;
		EditorGUI.BeginProperty(position, label, property);
		EditorGUI.PropertyField(position, property, label, true);

		if (property.isExpanded && property.objectReferenceValue != null)
		{
			EditorGUI.indentLevel++;
			SerializedObject obj = new(property.objectReferenceValue);
			SerializedProperty prop = obj.GetIterator();
			prop.NextVisible(true);
			while (prop.NextVisible(false))
			{
				EditorGUILayout.PropertyField(prop, true);
			}
			obj.ApplyModifiedProperties();
			EditorGUI.indentLevel--;
		}

		position.y += EditorGUIUtility.standardVerticalSpacing;
		EditorGUI.indentLevel--;
		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		float h = EditorGUIUtility.singleLineHeight;

		if (property.objectReferenceValue != null && property.isExpanded)
			h += EditorGUIUtility.standardVerticalSpacing * 2;

		return h;
	}
}

#endif