#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyInspector
{
	[CustomPropertyDrawer(typeof(EasyMember))]
	public class EasyMemberDrawer : PropertyDrawer
	{
		string _memberName;

		Type _type;
		Type _ownerType;
		Object _serializedObject;
		object _owner;
		EasyMember _easyMember;
		MethodInfo _buttonMethodInfo;
		FieldInfo _fieldInfo;
		PropertyInfo _propertyInfo;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_serializedObject == null) return;

			label = _easyMember.useMemberNameAsLabel
				? new(ObjectNames.NicifyVariableName(_easyMember.memberName))
				: label;

			// If member is a method
			if (_buttonMethodInfo != null)
				DrawButton(position, label);
			else
			{
				Undo.RecordObject(_serializedObject, "Inspector Member Changed");
				if (_fieldInfo != null)
					DrawField(position, property, label);
				else if (_propertyInfo != null)
					DrawProperty(position, property, label);
				else
					HandleError(position, label, $"No valid member named: {_easyMember.memberName}");
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
				_memberName = null;
				HandleError(position, label, $" Type: {_type} is not supported type for DisplayMember!");
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
				HandleError(position, label, $" Type: {_type} is not supported type for DisplayMember!");
			}

			property.isExpanded = isExpanded;
			if (!Equals(oldValue, newValue))
				_fieldInfo.SetValue(_owner, newValue);

			property.isExpanded = isExpanded;
		}

		private void DrawButton(Rect position, GUIContent label)
		{
			if (GUI.Button(position, label))
				_buttonMethodInfo.Invoke(_owner, Array.Empty<object>());
		}

		void HandleError(Rect position, GUIContent label, string message)
		{
			Rect labelPos = position;
			labelPos.width = EditorHelper.LabelWidth;
			Rect contentPos = EditorHelper.ContentRect(position);
			EditorGUI.LabelField(labelPos, label);
			EditorHelper.DrawErrorBox(contentPos);
			EditorGUI.LabelField(contentPos, message);
			_memberName = null;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SetupMemberInfo(property);

			if (_buttonMethodInfo != null)
				return EditorGUIUtility.singleLineHeight;

			return AnythingHeight(_type, property);
		}

		void SetupMemberInfo(SerializedProperty property)
		{
			_easyMember = (EasyMember)property.GetObjectOfProperty();
			if (_memberName != _easyMember.memberName)
			{
				_owner = property.GetObjectWithProperty();
				_ownerType = _owner.GetType();
				_memberName = _easyMember.memberName;
				if (TryGetMethodInfo(_ownerType, _memberName, out _buttonMethodInfo))
					_type = _buttonMethodInfo.ReturnType;
				else if (TryGetFieldInfo(_ownerType, _memberName, out _fieldInfo))
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

		const BindingFlags binding =
			BindingFlags.Instance |
			BindingFlags.Static |
			BindingFlags.Public |
			BindingFlags.NonPublic |
			BindingFlags.FlattenHierarchy;

		public static bool TryGetMethodInfo(Type ownerType, string name, out MethodInfo methodInfo)
		{
			MethodInfo method = ownerType.GetMethod(name, binding);

			if (method != null && IsNullOrEmpty(method.GetParameters()))
			{
				methodInfo = method;
				return true;
			}

			methodInfo = null;
			return false;
		}

		static bool IsNullOrEmpty<T>(T[] array) => array == null || array.Length == 0;

		public static bool TryGetFieldInfo(Type ownerType, string name, out FieldInfo fieldInfo)
		{
			fieldInfo = ownerType.GetField(name, binding);
			return fieldInfo != null;
		}

		public static bool TryGetPropertyInfo(Type ownerType, string name, out PropertyInfo propertyInfo)
		{
			PropertyInfo property = ownerType.GetProperty(name, binding);

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