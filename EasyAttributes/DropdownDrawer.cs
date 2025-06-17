#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyEditor
{
	[CustomPropertyDrawer(typeof(DropdownAttribute))]
	class DropdownDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			DropdownAttribute att = attribute as DropdownAttribute;
			Type fieldType = GetFieldType();
			IEnumerator enumerator = TryFindEnumerator(att, property.serializedObject.targetObject);
			if (enumerator == null)
			{
				EditorGUI.LabelField(position, label.text, "No Enumerable options found");
				return;
			}

			bool noneOption = att.enableNoneOption;

			List<object> items = new();
			List<string> names = new();
			if (noneOption)
			{
				items.Add(default);
				names.Add("None");
			}
			while (enumerator.MoveNext())
			{
				object current = enumerator.Current;
				items.Add(current);

				if (current == null)
					names.Add("None");
				if (current is Object o)
					names.Add(o.name);
				else
					names.Add(current.ToString());
			}

			object selected = property.GetObjectOfProperty();
			int selectedIndex = items.IndexOf(selected);

			bool selectedOptionNotFound = selectedIndex == -1 && (selected != default || !noneOption);

			if(selectedOptionNotFound)
				GUI.color = EditorHelper.ErrorRedColor;

			int newIndex = EditorGUI.Popup(position, label, selectedIndex, names.Select(s => new GUIContent(s)).ToArray()); 
			if (selectedIndex != newIndex)
			{
				object newSelected = newIndex < 0 ? null : items[newIndex];
				property.SetValue(newSelected);
			}

			if (selectedOptionNotFound)
			{
				Rect content = EditorHelper.ContentRect(position);
				string warningLabel = selected == null ? "None" : selected.ToString();
				EditorGUI.LabelField(content, warningLabel); 
				GUI.color = Color.white;
			}


		}

		Type GetFieldType()
		{
			Type fieldType = fieldInfo.FieldType;
			if (fieldType.IsArray)
				fieldType = fieldType.GetElementType();
			else if (fieldType.IsGenericType)
				fieldType = fieldType.GetGenericArguments()[0];
			return fieldType;
		}

		IEnumerator TryFindEnumerator(DropdownAttribute attribute, Object targetObject)
		{
			string memberName = attribute.nameOfOptions;
			Type targetType = targetObject.GetType();

			System.Reflection.FieldInfo field = targetType.GetField(memberName, EasyEditorUtility.allMembersBindings);
			if (field != null)
			{
				object value = field.GetValue(targetObject);
				return TryGetEnumerator(value);
			}
			System.Reflection.PropertyInfo property = targetType.GetProperty(memberName);
			if (property != null)
			{
				object value = property.GetValue(targetObject, null);
				return TryGetEnumerator(value);
			}

			System.Reflection.MethodInfo method = targetType.GetMethod(memberName);
			if (method != null)
			{
				object value = method.Invoke(targetObject, null);
				return TryGetEnumerator(value);
			}
			return null;
		}

		static IEnumerator TryGetEnumerator(object value)
		{
			if (value == null)
				return null;
			if (value is IEnumerable enumerable)
				return enumerable.GetEnumerator();
			if (value is IEnumerator enumerator)
				return enumerator;

			return null;
		}
	}
}
#endif