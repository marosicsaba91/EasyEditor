#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EasyEditor
{
	public class ButtonColumn<TRow> : Column<TRow>
	{
		public readonly Func<TRow, GUIContent> buttonText;
		public readonly Action<TRow> onClick;
		public readonly bool isObjectChangedByClicking;

		public ButtonColumn(
			Func<TRow, GUIContent> buttonText,
			Action<TRow> onClick,
			ColumnInfo columnInfo = null) :
			base(columnInfo)
		{
			this.buttonText = buttonText;
			this.onClick = onClick;
		}

		public ButtonColumn(
			Func<TRow, string> buttonText,
			Action<TRow> onClick,
			bool isObjectChangedByClicking,
			ColumnInfo columnInfo = null) :
			base(columnInfo)
		{
			this.buttonText = row => new GUIContent(buttonText(row));
			this.onClick = onClick;
			this.isObjectChangedByClicking = isObjectChangedByClicking;
		}

		public override void DrawCell(Rect position, TRow row, GUIStyle style, Action onChanged)
		{
			if (!GUI.Button(position, buttonText(row), style))
				return;

			onClick?.Invoke(row);

			if (isObjectChangedByClicking)
				onChanged?.Invoke();
		}

		protected override GUIStyle GetDefaultStyle() => new(GUI.skin.button);

		public override Alignment HeaderAlignment => Alignment.Center;

	}
}
#endif