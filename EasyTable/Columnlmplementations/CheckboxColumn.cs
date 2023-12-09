
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace EasyInspector
{
	public class CheckboxColumn<TRow> : EditableColumn<TRow, bool>
	{
		public CheckboxColumn(
			Func<TRow, bool> valueGetter,
			Action<TRow, bool> valueSetter,
			ColumnInfo columnInfo = null)
			: base(valueGetter, valueSetter, columnInfo)
		{ }

		protected override bool DrawEditableCell(Rect position, bool value, GUIStyle style)
		{
			const float checkBoxWidth = 14;
			float x = position.x + (position.width - checkBoxWidth) / 2;
			Rect pos = new(x, position.y, checkBoxWidth, position.height);
			return EditorGUI.Toggle(pos, value, style);
		}

		protected override GUIStyle GetDefaultStyle() => new(GUI.skin.toggle)
		{
			alignment = TextAnchor.MiddleCenter
		};

		public override Alignment HeaderAlignment => Alignment.Center;

	}

}
#endif