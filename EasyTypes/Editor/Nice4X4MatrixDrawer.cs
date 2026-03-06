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
			Rect foldoutRect = position;
			foldoutRect.width = EditorGUIUtility.labelWidth;
			foldoutRect.height = EditorGUIUtility.singleLineHeight;

			bool enabled = GUI.enabled;
			GUI.enabled = true;
			isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, label);
			GUI.enabled = enabled;
			 
			position.height = EditorGUIUtility.singleLineHeight;
			position.x += EditorGUIUtility.labelWidth;
			position.width -= EditorGUIUtility.labelWidth;

			if (isExpanded)
			{
				float spacing = 4;
				float cellWidth = (position.width - spacing * 3) / 4f;
				float savedLabelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 20f;

				for (int row = 0; row < 4; row++)
				{
					Vector4 rowVec = matrix.GetRow(row);
					for (int col = 0; col < 4; col++)
					{
						Rect cellRect = new(position.x + col * (cellWidth + spacing), position.y, cellWidth, position.height);
						rowVec[col] = EditorGUI.FloatField(cellRect, $"{row}/{col}", rowVec[col]);
					}
					matrix.SetRow(row, rowVec);
					position.y += EditorGUIUtility.singleLineHeight + spacing;
				}

				EditorGUIUtility.labelWidth = savedLabelWidth;
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