#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyEditor
{
	public class GUITable
	{
		protected static GUIStyle rowButtonStyle;
	}

	public class GUITable<TRowType> : GUITable
	{
		public List<IColumn<TRowType>> columns;

		// Optionally Modifiable Fields 
		public float rowHeight = EditorGUIUtility.singleLineHeight;
		public Func<int, TRowType, bool> isRowHighlightedGetter;
		public Action<int, TRowType> clickOnRow;
		public Func<Object> editedObjectGetter; // Makes it dirty if the table has been edited 
		public Func<string> emptyCollectionTextGetter = () => "Collection is empty";
		public bool drawHeader = true;
		public EditorWindow window;

		// Private Fields
		Vector2 _scrollPos;
		int _lastRowCount;
		Vector2Int _lastCellHoverIndex;
		Vector2Int _currentCellHoverIndex;
		bool _mouseMoved;

		public GUITable(List<IColumn<TRowType>> columns, EditorWindow window)
		{
			this.columns = columns;
			this.window = window;
		}

		public void Draw(Rect position, IReadOnlyList<TRowType> rows)
		{
			int rowCount = rows.Count;
			float areaH = HeaderHeight + rowHeight * rowCount;
			float areaW = position.width - (position.height >= areaH ? 0 : 13);
			Rect area = new(0f, 0f, areaW, areaH);

			float summaFixWidth = 0f;
			float summaRelativeWidth = 0f;
			foreach (IColumn<TRowType> column in columns.Where(column => column.IsVisible))
			{
				summaFixWidth += column.FixWidth;
				summaRelativeWidth += column.RelativeWidthWeight;
			}

			float leftoverWidth = areaW - summaFixWidth;

			float relativeWidthMultiplier =
				summaRelativeWidth == 0 ? 0 : leftoverWidth / summaRelativeWidth;

			bool wasScrolledToEnd = _lastRowCount != rowCount &&
									position.height + _scrollPos.y + 0.01f >= HeaderHeight + rowHeight * _lastRowCount;

			Vector2 newScrollPos = GUI.BeginScrollView(position, _scrollPos, area);
			bool manuallyScrolled = Math.Abs(newScrollPos.y - _scrollPos.y) > 0.01f;
			_scrollPos = newScrollPos;

			float currentY = HeaderHeight;
			if (rowHeight <= 0)
				rowHeight = EditorGUIUtility.singleLineHeight;

			_currentCellHoverIndex = Vector2Int.zero;
			_mouseMoved = Event.current.type == EventType.MouseMove;

			if (drawHeader)
				DrawHeaders(columns, relativeWidthMultiplier, _scrollPos.y);

			DrawRows(
				position.size,
				rows, currentY,
				relativeWidthMultiplier,
				OnRowsChanged);

			if (_mouseMoved && _currentCellHoverIndex != _lastCellHoverIndex)
			{
				window.Repaint();
				_lastCellHoverIndex = _currentCellHoverIndex;
			}


			void OnRowsChanged()
			{
				Object obj = editedObjectGetter?.Invoke();
				if (obj != null)
					EditorUtility.SetDirty(obj);
			}


			if (wasScrolledToEnd && !manuallyScrolled)
				_scrollPos.y = areaH - position.height;

			_lastRowCount = rowCount;
			GUI.enabled = true;
			GUI.EndScrollView();
		}

		void DrawRows(
			Vector2 size,
			IReadOnlyList<TRowType> rows,
			float currentY,
			float relativeWidthMultiplier,
			Action onRowsChanged)
		{
			int rowCount = rows.Count;
			if (rowCount == 0)
				GUI.Label(
					new Rect(5, currentY, size.x - 5, rowHeight),
					emptyCollectionTextGetter?.Invoke() ?? string.Empty);
			else
			{
				int firsElementToDraw = Mathf.Max(0, (int)(_scrollPos.y / rowHeight));
				int lastElementToDraw =
					Mathf.Min(firsElementToDraw + Mathf.CeilToInt(size.y / rowHeight), rows.Count - 1);

				currentY += firsElementToDraw * rowHeight;
				for (int i = firsElementToDraw; i <= lastElementToDraw; i++)
				{
					TRowType row = rows[i];
					bool isSelected = isRowHighlightedGetter?.Invoke(i, row) ?? false;
					List<Cell> cells = columns.Select(columnSelector => columnSelector.GetCell(row, onRowsChanged))
						.ToList();

					Rect rowPosition = new(0, currentY, size.x, rowHeight);
					bool clickedOnRow = DrawRow(
						rowPosition,
						i,
						columns,
						cells,
						relativeWidthMultiplier,
						i % 2 == 0,
						isSelected);
					currentY += rowHeight;

					if (clickedOnRow)
						clickOnRow?.Invoke(i, row);
				}
			}
		}

		bool DrawRow(
			Rect position,
			int rowIndex,
			IReadOnlyList<IColumn<TRowType>> columns,
			IReadOnlyList<Cell> cells,
			float relativeWidthMultiplier,
			bool isEven,
			bool isSelected)
		{
			EditorGUI.DrawRect(position, isEven
				? EditorHelper.tableEvenLineColor
				: EditorHelper.tableOddLineColor);

			if (isSelected)
				EditorGUI.DrawRect(position, EditorHelper.tableSelectedColor);

			if (clickOnRow != null && position.Contains(Event.current.mousePosition))
				EditorGUI.DrawRect(position, EditorHelper.tableHoverColor);

			float currentX = position.x;

			for (int i = 0; i < cells.Count; i++)
			{
				IColumn<TRowType> column = columns[i];
				Cell cell = cells[i];

				if (!column.IsVisible)
					continue;
				float width = column.GetWidth(relativeWidthMultiplier);
				Rect cellPosition = new(currentX, position.y, width, position.height);
				if (_mouseMoved && cellPosition.Contains(Event.current.mousePosition))
					_currentCellHoverIndex = new Vector2Int(rowIndex, i);

				cell.DrawCell(cellPosition);
				currentX += width;
			}

			if (clickOnRow == null)
				return false;

			rowButtonStyle ??= new GUIStyle(GUI.skin.label);
			bool click = GUI.Button(position, GUIContent.none, rowButtonStyle);

			return click;
		}

		void DrawHeaders(IEnumerable<IColumn<TRowType>> columns, float relativeWidthMultiplier, float currentY)
		{
			float currentX = 0;
			float height = HeaderHeight;
			IColumn<TRowType>[] columnArray = columns.ToArray();
			for (int i = 0; i < columnArray.Length; i++)
			{
				IColumn<TRowType> column = columnArray[i];
				if (!column.IsVisible)
					continue;

				float width = column.GetWidth(relativeWidthMultiplier);
				Rect headerRect = new(currentX, currentY, width + (i == columnArray.Length - 1 ? 0 : 1), height);
				currentX += width;
				Rect labelPosition = EditorHelper.DrawBox(headerRect);

				if (column.CustomHeaderDrawer == null)
				{
					GUIStyle style = column.HeaderAlignment.ToGUIStyle() ?? new GUIStyle(EditorStyles.miniLabel);
					GUI.Label(labelPosition, column.Title, style);
				}
				else
					column.CustomHeaderDrawer.Invoke(headerRect);
			}
		}

		float TableHeight(int rowCount) => HeaderHeight + RowsHeight(rowCount);
		float HeaderHeight => drawHeader ? 22 : 0;
		float RowsHeight(int rowCount) => rowHeight * Mathf.Max(1, rowCount);
	}
}
#endif