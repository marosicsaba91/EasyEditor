using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	public abstract class TableViewDrawer
	{
		public virtual bool ShowScript() => false;
		public virtual bool HideProperty(string propertyName) => false;
		public virtual GUIContent GetTitle(string propertyName, string displayName) => new(displayName);
		public abstract bool OverrideProperty_Base(Rect rect, Object target, SerializedProperty property, string propertyName);
	}

	public abstract class ObjectTableDrawer<T> : TableViewDrawer where T : Object
	{
		public override bool OverrideProperty_Base(Rect rect, Object target, SerializedProperty property, string propertyName)
			=> OverrideProperty(rect, (T)target, property, propertyName);

		public virtual bool OverrideProperty(Rect rect, T target, SerializedProperty property, string propertyName) => false;
	}
}