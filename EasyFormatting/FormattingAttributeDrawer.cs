#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	[CustomPropertyDrawer(typeof(FormattingAttribute), true)]
	public class FormattingAttributeDrawer : PropertyDrawer
	{
		bool _initialized = false;
		object _owner;
		PropertyDrawer _customPropertyDrawer;
		ShowIfAttribute _showIfAttribute;
		HideIfAttribute _hideIfAttribute;
		ReadOnlyAttribute _readOnlyAttribute;
		EnableIfAttribute _enableIfAttribute;
		DisableIfAttribute _disableIfAttribute;
		ColorAttribute _colorAttribute;
		LabelAttribute _labelAttribute;
		OnValueChangeAttribute _valueChangeCallbackAttribute;

		bool _toShow = true;
		bool _enabled = true;
		Color _color;
		string _label;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			_owner = property.GetObjectWithProperty();

			if (!_initialized)
			{
				object[] attributes = fieldInfo.GetCustomAttributes(typeof(FormattingAttribute), true);
				_showIfAttribute = Find<ShowIfAttribute>();
				_hideIfAttribute = Find<HideIfAttribute>();
				_enableIfAttribute = Find<EnableIfAttribute>();
				_disableIfAttribute = Find<DisableIfAttribute>();
				_readOnlyAttribute = Find<ReadOnlyAttribute>();
				_colorAttribute = Find<ColorAttribute>();
				_labelAttribute = Find<LabelAttribute>();
				_valueChangeCallbackAttribute = Find<OnValueChangeAttribute>();
				T Find<T>() => (T)attributes.FirstOrDefault(o => o.GetType() == typeof(T));

				_showIfAttribute?.Initialize(_owner);
				_hideIfAttribute?.Initialize(_owner);
				_enableIfAttribute?.Initialize(_owner);
				_disableIfAttribute?.Initialize(_owner);
				_colorAttribute?.Initialize(_owner);
				_labelAttribute?.Initialize(_owner);
				_valueChangeCallbackAttribute?.Initialize(_owner);

				_customPropertyDrawer = GetPropertyDrawer(property);
				_initialized = true;
			}

			_toShow =
				(_showIfAttribute == null || _showIfAttribute.CheckConditions(_owner)) &&
				(_hideIfAttribute == null || !_hideIfAttribute.CheckConditions(_owner));

			_enabled = _readOnlyAttribute == null &&
					   (_enableIfAttribute == null || _enableIfAttribute.CheckConditions(_owner)) &&
					   (_disableIfAttribute == null || !_disableIfAttribute.CheckConditions(_owner));

			_color = GUI.color;

			_label = _labelAttribute != null ? _labelAttribute.GetLabel(_owner) : label.text;

			if (_colorAttribute != null)
				if (_colorAttribute.TryGetColor(_owner, out Color c))
					_color = c;

			if (!_toShow)
				return -EditorGUIUtility.standardVerticalSpacing;

			if (_customPropertyDrawer != null)
				return _customPropertyDrawer.GetPropertyHeight(property, label);



			return EditorGUI.GetPropertyHeight(property);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!_toShow) return;

			bool savedEnabled = GUI.enabled;
			Color savedColor = GUI.color;
			GUI.enabled = _enabled;
			GUI.color = _color;
			label.text = _label;

			object oldValue = default;
			if (_valueChangeCallbackAttribute != null)
				oldValue = property.GetValue();

			if (_customPropertyDrawer != null)
				_customPropertyDrawer.OnGUI(position, property, label);
			else
				EditorGUI.PropertyField(position, property, label, true);

			property.serializedObject.ApplyModifiedProperties();

			if (_valueChangeCallbackAttribute != null)
			{
				object newValue = property.GetValue();
				if (!Equals(oldValue, newValue))
					_valueChangeCallbackAttribute.CallBack(_owner, oldValue, newValue);
			}

			GUI.enabled = savedEnabled;
			GUI.color = savedColor;
		}

		PropertyDrawer GetPropertyDrawer(SerializedProperty property)
		{
			PropertyDrawer customDrawer = InspectorDrawingUtility.GetPropertyDrawerForProperty(property, fieldInfo, attribute);
			if (customDrawer != null)
				return customDrawer;

			PropertyAttribute[] attributes = fieldInfo
				.GetCustomAttributes(typeof(PropertyAttribute), false)
				.Cast<PropertyAttribute>()
				.ToArray();

			PropertyAttribute formattingAttribute = attributes.FirstOrDefault(a => a is not FormattingAttribute);

			if (formattingAttribute == null)
				return null;

			// Try to get drawer for any other Attribute than FormattingAttribute
			Type attributeType = formattingAttribute.GetType();
			Type customDrawerType = InspectorDrawingUtility.GetPropertyDrawerTypeForFieldType(attributeType);
			if (customDrawerType == null)
				return null;

			customDrawer = InspectorDrawingUtility.InstantiatePropertyDrawer(customDrawerType, fieldInfo, formattingAttribute);

			return customDrawer;
		}
	}
}
#endif