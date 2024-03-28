#if UNITY_EDITOR
using EasyEditor;
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
		object _owner;
		EasyButton _easyButton;
		MethodInfo _buttonMethodInfo;

		string _methodName;
		Type _ownerType;   

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SetupMemberInfo(property);
			if (_serializedObject == null) return;

			label = _easyButton.useMethodNameAsLabel
				? new(ObjectNames.NicifyVariableName(_easyButton.methodName))
				: label;
			
			if (_buttonMethodInfo != null)
				DrawButton(position, label);
			else
			{
				EasyEditorUtility.HandleTypeError(position, label, $"No valid member named: {_easyButton.methodName}"); 
				_methodName = null;
			}

		}

		void SetupMemberInfo(SerializedProperty property)
		{
			_easyButton = (EasyButton)property.GetObjectOfProperty();
			if (_methodName != _easyButton.methodName)
			{
				_owner = property.GetObjectWithProperty();
				_ownerType = _owner.GetType();
				_methodName = _easyButton.methodName;
				TryGetMethodInfo(_ownerType, _methodName, out _buttonMethodInfo);
				_serializedObject = property.serializedObject.targetObject;
			}
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

		void DrawButton(Rect position, GUIContent label)
		{
			if (GUI.Button(position, label))
				_buttonMethodInfo.Invoke(_owner, Array.Empty<object>());
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{ 

			return EditorGUIUtility.singleLineHeight;
		}
	}
}
#endif