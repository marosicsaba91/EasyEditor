using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	public class FlexibleMultiLineAttribute : PropertyAttribute
	{
		public bool InLine { get; }
		public int MaxRows { get; }

		public FlexibleMultiLineAttribute(bool inLine = true, int maxRows = 10)
		{
			InLine = inLine;
			MaxRows = Mathf.Max(1, maxRows);
		}
	}
}

#if UNITY_EDITOR
namespace EasyEditor.Editor
{
	[CustomPropertyDrawer(typeof(FlexibleMultiLineAttribute))]
	class FlexibleMultiLineDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				EditorGUI.PropertyField(position, property, label, true);
				return;
			}

			EditorGUI.BeginProperty(position, label, property);
			FlexibleMultiLineAttribute settings = (FlexibleMultiLineAttribute)attribute;

			if (settings.InLine)
			{
				Rect textRect = EditorGUI.PrefixLabel(position, label);
				property.stringValue = EditorGUI.TextArea(textRect, property.stringValue ?? string.Empty);
			}
			else
			{
				Rect labelRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
				Rect textRect = new(
					position.x,
					labelRect.yMax + EditorGUIUtility.standardVerticalSpacing,
					position.width,
					position.height - EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

				EditorGUI.LabelField(labelRect, label);
				property.stringValue = EditorGUI.TextArea(textRect, property.stringValue ?? string.Empty);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.String)
				return EditorGUI.GetPropertyHeight(property, label, true);

			FlexibleMultiLineAttribute settings = (FlexibleMultiLineAttribute)attribute;
			float rowHeight = EditorGUIUtility.singleLineHeight;
			float width = Mathf.Max(
				80f,
				EditorGUIUtility.currentViewWidth - 40f - (settings.InLine ? EditorGUIUtility.labelWidth : 0f));

			string text = string.IsNullOrEmpty(property.stringValue) ? " " : property.stringValue;
			float textHeight = EditorStyles.textArea.CalcHeight(new GUIContent(text), width);
			textHeight = Mathf.Clamp(textHeight, rowHeight, rowHeight * (settings.MaxRows - 1) + 4);

			if (settings.InLine)
				return Mathf.Max(rowHeight, textHeight);

			return rowHeight + EditorGUIUtility.standardVerticalSpacing + textHeight;
		}
	}
}
#endif