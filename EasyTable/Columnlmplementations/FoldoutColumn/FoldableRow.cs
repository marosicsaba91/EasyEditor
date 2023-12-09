using System;
using System.Collections.Generic;

namespace EasyInspector
{
	public class FoldableRow<T>
	{
		public delegate void Fold(FoldableRow<T> element, bool open, bool batch);

		public readonly T element;
		public readonly IList<FoldableRow<T>> children;

		public readonly bool isRowOpen;
		public readonly int level;

		readonly Fold _onFold;

		public int ChildCount => children?.Count ?? 0;

		public void OnFold(bool open, bool batch)
		{
			_onFold.Invoke(this, open, batch);
		}

		public FoldableRow(
			T element,
			IList<FoldableRow<T>> children,
			bool isRowOpen,
			int level,
			Fold onFold)
		{
			this.element = element;
			this.children = children;
			this.isRowOpen = isRowOpen;
			this.level = level;
			_onFold = onFold;
		}


		public static List<FoldableRow<T>> GetRows<TContainer>(
			IEnumerable<TreeNode<T>> roots,
			List<TContainer> openedElements,
			Func<T, TContainer> converter)
		{
			List<FoldableRow<T>> rowList = new();
			ToOpenedList(roots, rowList, null, 0);
			return rowList;

			void ToOpenedList(
				IEnumerable<TreeNode<T>> grouped,
				ICollection<FoldableRow<T>> result,
				ICollection<FoldableRow<T>> parent,
				int indentLevel,
				bool addToResult = true)
			{
				foreach (TreeNode<T> node in grouped)
				{
					List<FoldableRow<T>> children = new();
					T element = node.node;
					bool isRowFoldable = (node.children?.Count ?? 0) != 0;

					bool isOpened = isRowFoldable && IsRowOpened(openedElements, element, converter);
					FoldableRow<T> row = new(
						element,
						children,
						isOpened,
						indentLevel,
						(r, open, batch) =>
							OpenCloseRow(openedElements, r, open, batch, converter));
					if (addToResult)
						result.Add(row);
					parent?.Add(row);
					bool add = addToResult && isOpened;
					if (node.children != null)
						ToOpenedList(node.children, result, children, indentLevel + 1, add);
				}
			}
		}

		static void OpenCloseRow<TContainer>(
			List<TContainer> openedElements,
			FoldableRow<T> row,
			bool open,
			bool batch,
			Func<T, TContainer> converter)
		{
			OpenCloseRow(openedElements, row, open, converter);
			if (!batch)
				return;
			foreach (FoldableRow<T> child in row.children)
				OpenCloseRow(openedElements, child, open, true, converter);
		}

		static void OpenCloseRow<TContainer>(
			ICollection<TContainer> openedElements,
			FoldableRow<T> row,
			bool open,
			Func<T, TContainer> converter)
		{
			if (open)
				openedElements.Add(converter(row.element));
			else
				openedElements.Remove(converter(row.element));
		}

		static bool IsRowOpened<TContainer>(
			List<TContainer> openedElements,
			T row,
			Func<T, TContainer> converter) => openedElements.Contains(converter(row));
	}
}