
using System.Reflection; 
using UnityEngine;

namespace EasyEditor
{
	static class EasyEditorUtility
	{
		internal static void HandleTypeError(Rect position, GUIContent label, string message)
		{
#if UNITY_EDITOR
			Rect labelPos = position;
			labelPos.width = EditorHelper.LabelWidth;
			Rect contentPos = EditorHelper.ContentRect(position);
			UnityEditor.EditorGUI.LabelField(labelPos, label);
			EditorHelper.DrawErrorBox(contentPos);
			UnityEditor.EditorGUI.LabelField(contentPos, message);
#endif
		}

		public const BindingFlags allMembersBindings =
			BindingFlags.Instance |
			BindingFlags.Static |
			BindingFlags.Public |
			BindingFlags.NonPublic |
			BindingFlags.FlattenHierarchy;
	}
}
