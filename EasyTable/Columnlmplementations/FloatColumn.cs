#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace EasyInspector
{
	public class FloatColumn<TRow> : EditableColumn<TRow, float>
	{
		public FloatColumn(
			Func<TRow, float> valueGetter,
			Action<TRow, float> valueSetter,
			ColumnInfo columnInfo = null)
			: base(valueGetter, valueSetter, columnInfo)
		{
		}

		protected override float DrawEditableCell(Rect position, float value, GUIStyle style) =>
			EditorGUI.FloatField(position, value, style);


		protected override GUIStyle GetDefaultStyle() => new(GUI.skin.label)
		{
			alignment = TextAnchor.MiddleRight
		};

		public override Alignment HeaderAlignment => Alignment.Right;
	}
}
#endif