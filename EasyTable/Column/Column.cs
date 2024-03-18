#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EasyEditor
{
	public interface IColumn<in TRow>
	{
		float GetWidth(float relativeWidthMultiplier);
		bool IsVisible { get; }
		string Title { get; }
		float FixWidth { get; }
		float RelativeWidthWeight { get; }
		Alignment HeaderAlignment { get; }
		Action<Rect> CustomHeaderDrawer { get; }
		Cell GetCell(TRow row, Action onChanged);
		void DrawCell(Rect position, TRow row, Action onChanged);
	}

	public abstract class Column<TRow> : IColumn<TRow>
	{
		protected ColumnInfo columnInfo;

		public string Title => columnInfo.Title;
		public bool IsVisible => columnInfo.IsVisible;
		public float FixWidth => columnInfo.FixWidth;
		public float RelativeWidthWeight => columnInfo.RelativeWidthWeight;
		public Action<Rect> CustomHeaderDrawer => columnInfo.customHeaderDrawer;

		GUIStyle Style => columnInfo.style ?? GetDefaultStyle();
		public virtual Alignment HeaderAlignment => columnInfo.headerAlignment ?? GetDefaultAlignment();

		public float GetWidth(float relativeWidthMultiplier) => FixWidth + relativeWidthMultiplier * RelativeWidthWeight;

		protected Column(ColumnInfo columnInfo = null)
		{
			this.columnInfo = columnInfo;
		}

		public Cell GetCell(TRow row, Action onChanged) => new Cell<TRow, IColumn<TRow>>(row, this, onChanged);

		public void DrawCell(Rect position, TRow row, Action onChanged) => DrawCell(position, row, Style, onChanged);

		public abstract void DrawCell(Rect position, TRow row, GUIStyle style, Action onChanged);

		protected virtual GUIStyle GetDefaultStyle() => new(GUI.skin.label)
		{
			padding = new RectOffset(2, 2, 0, 0)
		};

		protected virtual Alignment GetDefaultAlignment() => Alignment.Left;
	}
}
#endif