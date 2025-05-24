#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyEditor
{
	[CustomPropertyDrawer(typeof(EasyProperty))]
	public class EasyPropertyDrawer : PropertyDrawer
	{
		string _memberName;

		Type _type;
		Type _ownerType;
		Object _serializedObject;
		object _owner;
		EasyProperty _easyMember;
		FieldInfo _fieldInfo;
		PropertyInfo _propertyInfo;

		EasyRangeAttribute _rangeAttribute;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_serializedObject == null) return;

			label = _easyMember.usePropertyNameAsLabel
				? new(ObjectNames.NicifyVariableName(_easyMember.propertyName))
				: label;

			Undo.RecordObject(_serializedObject, "Inspector Member Changed");

			if (_fieldInfo != null)
				DrawField(position, property, label);
			else if (_propertyInfo != null)
				DrawProperty(position, property, label);
			else
			{
				EasyEditorUtility.HandleTypeError(position, label, $"No valid member named: {_easyMember.propertyName}");
				_memberName = null;
			}
		}

		void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
		{
			bool isExpanded = property.isExpanded;
			object oldValue = _propertyInfo.GetValue(_owner);
			bool savedEnabled = GUI.enabled;
			if (_propertyInfo.SetMethod == null)
				GUI.enabled = false;

			object newValue = oldValue;
			try
			{
				if (_rangeAttribute != null && (_type == typeof(int) || _type == typeof(float)))
					newValue = DrawSlider(position, _type, oldValue, label, _rangeAttribute.min, _rangeAttribute.max);
				else
					newValue = EditorHelper.AnythingField(position, _type, oldValue, label, ref isExpanded);
			}
			catch (InvalidCastException)
			{
				EasyEditorUtility.HandleTypeError(position, label, $" Type: {_type} is not supported type for DisplayMember!");
				_memberName = null;
			}



			GUI.enabled = savedEnabled;
			if (!Equals(oldValue, newValue))
			{
				try
				{
					_propertyInfo.SetValue(_owner, newValue);
				}
				catch (Exception)
				{
					property.SetValue(newValue);
				}
			}

			property.isExpanded = isExpanded;
		}

		void DrawField(Rect position, SerializedProperty property, GUIContent label)
		{
			bool isExpanded = property.isExpanded;
			object oldValue = _fieldInfo.GetValue(_owner);

			object newValue = oldValue;
			try
			{
				if (_rangeAttribute != null && (_type == typeof(int) || _type == typeof(float)))
					newValue = DrawSlider(position, _type, oldValue, label, _rangeAttribute.min, _rangeAttribute.max);
				else
					newValue = EditorHelper.AnythingField(position, _type, oldValue, label, ref isExpanded);
			}
			catch (InvalidCastException)
			{
				_memberName = null;
				EasyEditorUtility.HandleTypeError(position, label, $" Type: {_type} is not supported type for DisplayMember!");
				_memberName = null;

			}

			property.isExpanded = isExpanded;
			if (!Equals(oldValue, newValue))
				_fieldInfo.SetValue(_owner, newValue);

			property.isExpanded = isExpanded;
		}

		object DrawSlider(Rect position, Type type, object oldValue, GUIContent label, float min, float max)
		{
			if (type == typeof(int))
				return EditorGUI.IntSlider(position, label, (int)oldValue, Mathf.FloorToInt(min), Mathf.CeilToInt(max));
			else if (type == typeof(float))
				return EditorGUI.Slider(position, label, (float)oldValue, min, max);
			else
			{
				EasyEditorUtility.HandleTypeError(position, label, $" Type: {type} is not supported type for RangeAttribute!");
				return oldValue;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SetupMemberInfo(property);
			return AnythingHeight(_type, property);
		}

		void SetupMemberInfo(SerializedProperty property)
		{
			_easyMember = (EasyProperty)property.GetObjectOfProperty();
			if (_memberName != _easyMember.propertyName)
			{
				_owner = property.GetObjectWithProperty();
				_ownerType = _owner.GetType();
				_memberName = _easyMember.propertyName;

				if (TryGetFieldInfo(_ownerType, _memberName, out _fieldInfo))
					_type = _fieldInfo.FieldType;
				else if (TryGetPropertyInfo(_ownerType, _memberName, out _propertyInfo))
					_type = _propertyInfo.PropertyType;

				_serializedObject = property.serializedObject.targetObject;

				FieldInfo infoOfEasyMember = _ownerType.GetField(property.name, EasyEditorUtility.allMembersBindings);
				_rangeAttribute = Attribute.GetCustomAttribute(infoOfEasyMember, typeof(EasyRangeAttribute)) as EasyRangeAttribute;
			}

		}

		float AnythingHeight(Type type, SerializedProperty property)
		{
			if (type == typeof(Rect) ||
				type == typeof(RectInt))
				return 2 * EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			if (type == typeof(Bounds) ||
				type == typeof(BoundsInt))
				return 3 * EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing;
			if (type == typeof(Matrix4x4))
				return Nice4X4MatrixDrawer.PropertyHeight(property);  // Add Universal solution

			return EditorGUIUtility.singleLineHeight;
		}

		public static bool TryGetFieldInfo(Type ownerType, string name, out FieldInfo fieldInfo)
		{
			fieldInfo = ownerType.GetField(name, EasyEditorUtility.allMembersBindings);
			return fieldInfo != null;
		}

		public static bool TryGetPropertyInfo(Type ownerType, string name, out PropertyInfo propertyInfo)
		{
			PropertyInfo property = ownerType.GetProperty(name, EasyEditorUtility.allMembersBindings);

			if (property != null && property.GetMethod != null)
			{
				propertyInfo = property;
				return true;
			}

			propertyInfo = null;
			return false;
		}
	}
}
#endif