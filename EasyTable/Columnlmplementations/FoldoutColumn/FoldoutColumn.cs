#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace EasyInspector
{
	public abstract class FoldoutColumn<T> : Column<FoldableRow<T>>
	{
		protected FoldoutColumn(ColumnInfo columnInfo = null) : base(columnInfo) { }

		public override void DrawCell(Rect position, FoldableRow<T> row, GUIStyle style, Action onChanged)
		{
			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			position = DrawFoldout(position, row);
			DrawContent(position, row, style, onChanged);
			EditorGUI.indentLevel = indent;
		}

		public abstract void DrawContent(Rect position, FoldableRow<T> row, GUIStyle style, Action onChanged);

		protected static Rect DrawFoldout(Rect position, FoldableRow<T> row)
		{
			const float indentW = 14;
			const float dropdownW = 14;
			float minimumW = EditorHelper.LabelStartX - 16;

			float fullW = position.width;
			float shift = minimumW + row.level * indentW;
			position.x += shift;
			position.width = dropdownW;
			if (row.ChildCount > 0)
			{
				bool newOpened = EditorGUI.Foldout(
					position,
					row.isRowOpen,
					GUIContent.none,
					FoldoutStyle);
				if (newOpened != row.isRowOpen)
					row.OnFold(newOpened, Event.current.alt);
			}

			position.x += dropdownW;
			position.width = fullW - dropdownW - shift;

			return position;
		}

		protected override GUIStyle GetDefaultStyle() => new(GUI.skin.label);

		static GUIStyle FoldoutStyle => new(EditorStyles.foldout)
		{
			alignment = TextAnchor.MiddleLeft
		};
	}
}
#endif