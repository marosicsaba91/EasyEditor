using EasyEditor;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	static class EasyEditorUtility
	{
		internal static void HandleTypeError(Rect position, GUIContent label, string message)
		{
			Rect labelPos = position;
			labelPos.width = EditorHelper.LabelWidth;
			Rect contentPos = EditorHelper.ContentRect(position);
			EditorGUI.LabelField(labelPos, label);
			EditorHelper.DrawErrorBox(contentPos);
			EditorGUI.LabelField(contentPos, message);
		}

		public const BindingFlags allMembersBindings =
			BindingFlags.Instance |
			BindingFlags.Static |
			BindingFlags.Public |
			BindingFlags.NonPublic |
			BindingFlags.FlattenHierarchy;
	}
}
