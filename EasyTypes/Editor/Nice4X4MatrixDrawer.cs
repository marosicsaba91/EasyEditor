#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	[CustomPropertyDrawer(typeof(Matrix4x4))]
	public class Nice4X4MatrixDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			object obj = property.GetObjectOfProperty();
			if (obj.GetType() != typeof(Matrix4x4))
				return;

			bool isExpanded = property.isExpanded;

			Matrix4x4 oldMatrix = (Matrix4x4)obj;
			Matrix4x4 newMatrix = Draw(position, label, (Matrix4x4)obj, ref isExpanded);
			if (oldMatrix != newMatrix)
				property.SetValue(newMatrix);

			property.isExpanded = isExpanded;
		}


		public static Matrix4x4 Draw(Rect position, GUIContent label, Matrix4x4 matrix, ref bool isExpanded)
		{
			// var matrixAttribute = (NiceMatrix4X4Attribute) attribute; 

			Rect foldoutRect = position;
			foldoutRect.width = EditorGUIUtility.labelWidth;
			foldoutRect.height = EditorGUIUtility.singleLineHeight;

			bool enabled = GUI.enabled;
			GUI.enabled = true;
			isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, label);
			GUI.enabled = enabled;


			GUIContent noLabel = GUIContent.none;
			position.height = EditorGUIUtility.singleLineHeight;
			position.x += EditorGUIUtility.labelWidth;
			position.width -= EditorGUIUtility.labelWidth;


			if (isExpanded)
			{
				EditorGUI.indentLevel++;
				matrix.SetRow(0, EditorGUI.Vector4Field(position, noLabel, matrix.GetRow(0)));
				position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				matrix.SetRow(1, EditorGUI.Vector4Field(position, noLabel, matrix.GetRow(1)));
				position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				matrix.SetRow(2, EditorGUI.Vector4Field(position, noLabel, matrix.GetRow(2)));
				position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				matrix.SetRow(3, EditorGUI.Vector4Field(position, noLabel, matrix.GetRow(3)));
				EditorGUI.indentLevel--;
			}
			else
			{
				string text = GetNiceString(matrix);

				EditorGUI.LabelField(position, text);
			}

			return matrix;
		}

		static string GetNiceString(Matrix4x4 matrix)
		{
			string text = "(";
			for (int y = 0; y < 4; y++)
				for (int x = 0; x < 4; x++)
				{
					if (x == 0 && y > 0)
						text += ")(";

					int i = x * 4 + y;
					float value = matrix[i];
					string s = value.ToString("0.##");
					text += s;
					if (x != 3)
						text += ", ";
				}
			text += ")";
			return text;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			bool isExpanded = property.isExpanded;
			float singleLineHeight = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;
			int lineCount = isExpanded ? 4 : 1;
			return singleLineHeight * lineCount + spacing * (lineCount - 1);
		}

		public static float PropertyHeight(bool isExpanded)
		{
			float singleLineHeight = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;
			int lineCount = isExpanded ? 4 : 1;
			return singleLineHeight * lineCount + spacing * (lineCount - 1);
		}
	}
}
#endif