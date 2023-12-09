using System.Collections.Generic;

namespace EasyInspector
{
	public class TreeNode<T>
	{
		public readonly T node;
		public readonly IList<TreeNode<T>> children;

		public TreeNode(T node, IList<TreeNode<T>> children)
		{
			this.node = node;
			this.children = children;
		}
	}
}