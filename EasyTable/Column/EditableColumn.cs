#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EasyInspector
{
	public abstract class EditableColumn<TRow, TValue> : Column<TRow> where TValue : IEquatable<TValue>
	{
		protected readonly Func<TRow, TValue> valueGetter;
		protected readonly Action<TRow, TValue> valueSetter;

		protected EditableColumn(
			Func<TRow, TValue> valueGetter,
			Action<TRow, TValue> valueSetter,
			ColumnInfo columnInfo = null) : base(columnInfo)
		{
			this.valueGetter = valueGetter;
			this.valueSetter = valueSetter;
		}

		public override void DrawCell(Rect position, TRow row, GUIStyle style, Action onChanged)
		{
			TValue oldValue = valueGetter(row);
			TValue val = DrawEditableCell(position, oldValue, style);
			if (oldValue != null && !oldValue.Equals(val))
			{
				onChanged?.Invoke();
			}

			valueSetter?.Invoke(row, val);
		}

		protected abstract TValue DrawEditableCell(Rect position, TValue value, GUIStyle style);

	}
}
#endif