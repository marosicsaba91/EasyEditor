#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EasyInspector
{
	public class ColumnInfo
	{
		public string Title => titleGetter?.Invoke() ?? (title ?? string.Empty);
		public Func<string> titleGetter = null;
		public string title = string.Empty;

		public bool IsVisible => visibleGetter?.Invoke() ?? isVisible;
		public Func<bool> visibleGetter = null;
		public bool isVisible = true;

		public float FixWidth => fixWidthGetter?.Invoke() ?? fixWidth;
		public Func<float> fixWidthGetter = null;
		public float fixWidth = 0;
		public float RelativeWidthWeight => relativeWidthWeightGetter?.Invoke() ?? relativeWidthWeight;
		public Func<float> relativeWidthWeightGetter = null;
		public float relativeWidthWeight = 0;

		public GUIStyle style = null;
		public Alignment? headerAlignment = Alignment.Left;
		public Action<Rect> customHeaderDrawer = null;
	}
}
#endif