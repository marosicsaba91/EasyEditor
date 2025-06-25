using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	public abstract class ObjectTableDrawer
	{
		public virtual bool ShowScript() => false;
		public virtual bool HideProperty(string propertyName) => false;
		public virtual GUIContent GetGUIContent(string propertyName, string displayName) => new(displayName);
		public abstract bool OverrideProperty_Base(Rect rect, Object target, SerializedProperty property, string propertyName);
	}

	public abstract class ObjectTableDrawer<T> : ObjectTableDrawer where T: Object
	{
		public override bool OverrideProperty_Base(Rect rect, Object target, SerializedProperty property, string propertyName)
			=> 	OverrideProperty(rect, (T) target, property, propertyName);

		public virtual bool OverrideProperty(Rect rect, T target, SerializedProperty property, string propertyName) => false;
	}
}