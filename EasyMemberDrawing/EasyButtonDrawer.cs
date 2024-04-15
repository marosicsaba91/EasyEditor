#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine; 
using Object = UnityEngine.Object;

namespace EasyEditor
{
	[CustomPropertyDrawer(typeof(EasyButton))]
	public class EasyButtonDrawer : PropertyDrawer
	{
		Object _serializedObject;
		EasyButton _easyButton;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			_easyButton = (EasyButton)property.GetObjectOfProperty();
			object owner = property.GetObjectWithProperty();

			bool methodFound = TryGetMethodInfo(owner.GetType(), _easyButton.methodName, out MethodInfo methodInfo);
			_serializedObject = property.serializedObject.targetObject; if (_serializedObject == null) return;

			label = _easyButton.useMethodNameAsLabel
				? new(ObjectNames.NicifyVariableName(_easyButton.methodName))
				: label;

			if (methodFound)
				DrawButton(position, label, owner, methodInfo);
			else
				EasyEditorUtility.HandleTypeError(position, label, $"No valid member named: {_easyButton.methodName}");

		}

		public static bool TryGetMethodInfo(Type ownerType, string name, out MethodInfo methodInfo)
		{
			MethodInfo method = ownerType.GetMethod(name, EasyEditorUtility.allMembersBindings);

			if (method != null && IsNullOrEmpty(method.GetParameters()))
			{
				methodInfo = method;
				return true;
			}

			methodInfo = null;
			return false;
		}

		static bool IsNullOrEmpty<T>(T[] array) => array == null || array.Length == 0;

		void DrawButton(Rect position, GUIContent label, object owner, MethodInfo methodInfo)
		{
			if (GUI.Button(position, label))
				methodInfo.Invoke(owner, Array.Empty<object>());
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}
#endif