#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EasyInspector
{
	public class LabelColumn<TRow> : ValueColumn<TRow, GUIContent>
	{
		public LabelColumn(Func<TRow, GUIContent> valueGetter, ColumnInfo columnInfo = null) : base(valueGetter, columnInfo) { }

		public LabelColumn(Func<TRow, string> valueGetter,
			ColumnInfo columnInfo = null) : base(row => new GUIContent(valueGetter(row)), columnInfo) { }

		protected override void DrawCell(Rect position, GUIContent value, GUIStyle style) =>
			GUI.Label(position, value, style);

		protected override GUIStyle GetDefaultStyle() => new(GUI.skin.label);
	}
}
#endif