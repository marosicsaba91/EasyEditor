#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyEditor
{
	[CustomPropertyDrawer(typeof(DropdownSOAttribute))]
	class DropdownSODrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Type type = fieldInfo.FieldType;
			if (type.IsArray)
				type = type.GetElementType();
			else if (type.IsGenericType)
				type = type.GetGenericArguments()[0];

			if (!type.IsSubclassOf(typeof(ScriptableObject)))
			{
				EditorGUI.LabelField(position, label.text, "Field should be subclass of ScriptableObject");
				return;
			}

			ScriptableObject so = property.objectReferenceValue as ScriptableObject;

			List<Object> all = TableViewCache.GetScriptableObjectsByType(type);
			List<string> names = new();
			foreach (Object obj in all)
			{
				ScriptableObject scriptableObject = obj as ScriptableObject;
				if (scriptableObject != null)
					names.Add(scriptableObject.name);
			}


			names.Insert(0, "None");

			int lastIndex = all.IndexOf(so) + 1;

			int newIndex = EditorGUI.Popup(position, label, lastIndex, names.Select(s => new GUIContent(s)).ToArray());
			if (lastIndex != newIndex)
			{
				Object newId = newIndex == 0 ? null : all[newIndex - 1];
				property.objectReferenceValue = newId;
			}
		}
	}
}
#endif