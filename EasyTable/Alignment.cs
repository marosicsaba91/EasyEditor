#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EasyEditor
{
	public enum Alignment
	{
		Left,
		Center,
		Right
	}

	public static class AlignmentHelper
	{
		const int padding = 5;

		public static GUIStyle ToGUIStyle(this Alignment alignment, int fontSize = 12, FontStyle fontStyle = FontStyle.Normal)
		{
			GUIStyle result = new(GUI.skin.label)
			{
				alignment = alignment.ToTextAnchor(),
				fontSize = fontSize,
				fontStyle = fontStyle
			};

			if (alignment != Alignment.Center)
				result.padding = new RectOffset(padding, padding, 0, 0);

			return result;
		}

		public static TextAnchor ToTextAnchor(this Alignment alignment)
		{
			switch (alignment)
			{
				case Alignment.Left:
					return TextAnchor.MiddleLeft;
				case Alignment.Center:
					return TextAnchor.MiddleCenter;
				case Alignment.Right:
					return TextAnchor.MiddleRight;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
#endif