
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace EasyInspector
{
	public class TextFieldColumn<TRow> : EditableColumn<TRow, string>
	{
		public TextFieldColumn(
			Func<TRow, string> valueGetter,
			Action<TRow, string> valueSetter,
			ColumnInfo columnInfo = null)
			: base(valueGetter, valueSetter, columnInfo)
		{ }

		protected override string DrawEditableCell(Rect position, string value, GUIStyle style) =>
			EditorGUI.TextField(position, value, style);

		protected override GUIStyle GetDefaultStyle() => new(GUI.skin.textField) { };


	}

}
#endif