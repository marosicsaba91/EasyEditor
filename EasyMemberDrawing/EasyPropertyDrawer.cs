#if UNITY_EDITOR
using EasyEditor;
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
				EasyMemberUtility.HandleTypeError(position, label, $"No valid member named: {_easyMember.propertyName}");
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
				newValue = EditorHelper.AnythingField(position, _type, oldValue, label, ref isExpanded);
			}
			catch (InvalidCastException)
			{
				EasyMemberUtility.HandleTypeError(position, label, $" Type: {_type} is not supported type for DisplayMember!");
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
				newValue = EditorHelper.AnythingField(position, _type, oldValue, label, ref isExpanded);
			}
			catch (InvalidCastException)
			{
				_memberName = null;
				EasyMemberUtility.HandleTypeError(position, label, $" Type: {_type} is not supported type for DisplayMember!");
				_memberName = null;

			}

			property.isExpanded = isExpanded;
			if (!Equals(oldValue, newValue))
				_fieldInfo.SetValue(_owner, newValue);

			property.isExpanded = isExpanded;
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
			fieldInfo = ownerType.GetField(name, EasyMemberUtility.membersBindings);
			return fieldInfo != null;
		}

		public static bool TryGetPropertyInfo(Type ownerType, string name, out PropertyInfo propertyInfo)
		{
			PropertyInfo property = ownerType.GetProperty(name, EasyMemberUtility.membersBindings);

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