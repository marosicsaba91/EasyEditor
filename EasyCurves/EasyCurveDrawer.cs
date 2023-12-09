#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine; 
using Object = UnityEngine.Object;

namespace EasyInspector
{
	[CustomPropertyDrawer(typeof(EasyCurve))]
	class EasyCurveDrawer : PropertyDrawer
	{
		string _memberName; 
		Type _ownerType;
		Object _serializedObject;
		object _owner;

		Func<float, float> _floatFunction;
		CurvePreview _curvePreview;
		EasyCurve _easyFunction;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_serializedObject == null) return;

			label = _easyFunction.useMemberNameAsLabel
				? new(ObjectNames.NicifyVariableName(_easyFunction.functionName))
				: label;

			// If member is a method
			Undo.RecordObject(_serializedObject, "Inspector Member Changed");
			if (_floatFunction != null)
				DrawFloatFunction(position, property, label);
			else
				HandleError(position, label, $"No valid float->float function: {_easyFunction.functionName}");
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

		void DrawFloatFunction(Rect position, SerializedProperty property, GUIContent label)
		{
			bool isExpanded = property.isExpanded;
			Rect content = EditorHelper.ContentRect(position);
			Rect labelRect = EditorHelper.LabelRect(position);
			property.isExpanded = EditorGUI.Foldout(labelRect, isExpanded, label);

			Vector2 size = new(1, 1);
			Rect area = new(-(size / 2f), size);
			_curvePreview.zoom = _easyFunction.functionZoom;
			_curvePreview.offset = _easyFunction.functionOffset;
			_curvePreview.Draw(content, _floatFunction, area, EditorHelper.functionColor, isExpanded);
			// property.isExpanded = isExpanded;
			_easyFunction.functionOffset = _curvePreview.offset;
			_easyFunction.functionZoom = _curvePreview.zoom;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SetupFunctionInfo(property);

			if (_floatFunction != null)
				return property.isExpanded ? 120 : EditorGUIUtility.singleLineHeight;

			return EditorGUIUtility.singleLineHeight;
		}

		void SetupFunctionInfo(SerializedProperty property)
		{
			_easyFunction = (EasyCurve)property.GetObjectOfProperty();

			if (_memberName != _easyFunction.functionName)
			{
				_owner = property.GetObjectWithProperty();
				_ownerType = _owner.GetType();
				_memberName = _easyFunction.functionName;
				_serializedObject = property.serializedObject.targetObject;

				TryGetFunction(_owner, _ownerType, _memberName, out _floatFunction, out _curvePreview);
			}
		}

		public static bool TryGetFunction(
			object owner,
			Type ownerType,
			string name,
			out Func<float, float> function,
			out CurvePreview curve)
		{
			const BindingFlags binding =
				BindingFlags.Instance |
				BindingFlags.Static |
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.FlattenHierarchy;

			MethodInfo func = ownerType
				.GetMethods(binding)
				.FirstOrDefault(m =>
					m.Name == name &&
					(m.ReturnType == typeof(float) || m.ReturnType == typeof(int)) &&
					m.GetParameters().Length == 1 &&
					m.GetParameters()[0].ParameterType == typeof(float));

			if (func != null)
			{
				float Function(float f)
				{
					object result = func.Invoke(owner, new object[] { f });
					Type resultType = result.GetType();
					if (resultType == typeof(int))
						return (int)result;
					return (float)result;
				}

				function = Function;
				curve = new CurvePreview();
				return true;
			}

			function = null;
			curve = null;
			return false;
		}

	}


}
#endif