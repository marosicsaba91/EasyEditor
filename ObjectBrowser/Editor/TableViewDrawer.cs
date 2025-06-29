#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EasyEditor
{
	public class TableViewDrawer
	{
		public virtual bool ShowScript() => false;
		public virtual bool ShowProperty(string propertyName) => true;
		public virtual GUIContent GetTitle(string propertyName, string displayName) => new(displayName);
		public virtual void DrawProperty(Rect rect, Object target, SerializedProperty property, string propertyName) => OnLineField(rect, property);

		void OnLineField(Rect rect, SerializedProperty property)
		{
			SerializedPropertyType t = property.propertyType;

			if (t == SerializedPropertyType.Float)
				property.floatValue = EditorGUI.FloatField(rect, property.floatValue);
			else if (t == SerializedPropertyType.Integer)
				property.intValue = EditorGUI.IntField(rect, property.intValue);
			else if (t == SerializedPropertyType.Boolean)
				property.boolValue = EditorGUI.Toggle(rect, property.boolValue);
			else if (t == SerializedPropertyType.String)
				property.stringValue = EditorGUI.TextField(rect, property.stringValue);
			else if (t == SerializedPropertyType.Color)
				property.colorValue = EditorGUI.ColorField(rect, property.colorValue);
			else if (t == SerializedPropertyType.Vector2)
				property.vector2Value = EditorGUI.Vector2Field(rect, GUIContent.none, property.vector2Value);
			else if (t == SerializedPropertyType.Vector3)
				property.vector3Value = EditorGUI.Vector3Field(rect, GUIContent.none, property.vector3Value);
			else if (t == SerializedPropertyType.Vector4)
				property.vector4Value = EditorGUI.Vector4Field(rect, GUIContent.none, property.vector4Value);
			else if (t == SerializedPropertyType.LayerMask)
				property.intValue = EditorGUI.LayerField(rect, GUIContent.none, property.intValue);
			else if (property.isArray)
				EditorGUI.LabelField(rect, "List: " + property.arraySize.ToString());
			else
			{
				EditorGUI.PropertyField(rect, property, GUIContent.none, false);
				return;
			}
			// Apply the changes to the serialized object
			property.serializedObject.ApplyModifiedProperties();
		}

		public virtual bool OverridePropertyWidth(string propertyName, out float w)
		{
			w = 200;
			return false;
		}

		public virtual float GetColumPriority(string propertyName, float defaultPriority) => defaultPriority;
		public virtual int Compare(Object a, Object b) => string.Compare(a.name, b.name);
	}

	public abstract class TableViewDrawer<T> : TableViewDrawer where T : Object
	{
		public sealed override void DrawProperty(Rect rect, Object target, SerializedProperty property, string propertyName)
		{
			if (TryOverridePropertyDrawing(rect, (T)target, property, propertyName))
				return;
			else
				base.DrawProperty(rect, target, property, propertyName);
		}

		public virtual bool TryOverridePropertyDrawing(Rect rect, T target, SerializedProperty property, string propertyName) => false;
		public sealed override int Compare(Object a, Object b) => Compare(a as T, b as T);
		public virtual int Compare(T a, T b) => 0;
	}
}
#endif
