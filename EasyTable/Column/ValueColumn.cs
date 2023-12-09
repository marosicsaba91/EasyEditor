#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EasyInspector
{
	public abstract class ValueColumn<TRow, TValue> : Column<TRow>
	{
		protected readonly Func<TRow, TValue> valueGetter;

		protected ValueColumn(Func<TRow, TValue> valueGetter, ColumnInfo columnInfo = null) : base(columnInfo)
		{
			this.valueGetter = valueGetter;
		}

		public sealed override void DrawCell(Rect position, TRow row, GUIStyle style, Action onChanged) =>
			DrawCell(position, valueGetter(row), style);

		protected abstract void DrawCell(Rect position, TValue value, GUIStyle style);
	}
}
#endif