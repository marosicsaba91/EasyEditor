#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EasyEditor
{
	public abstract class Cell
	{
		public abstract void DrawCell(Rect position);
	}

	class Cell<TRow, TColumn> : Cell where TColumn : IColumn<TRow>
	{
		readonly Action _onChanged;
		readonly TRow _row;
		readonly TColumn _column;

		public Cell(
			TRow row,
			TColumn column,
			Action onChanged
		)
		{
			_row = row;
			_column = column;
			_onChanged = onChanged;
		}

		public override void DrawCell(Rect position) => _column.DrawCell(position, _row, _onChanged);

	}
}
#endif