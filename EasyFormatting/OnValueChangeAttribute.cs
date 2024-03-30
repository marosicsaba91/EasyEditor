using System;
using System.Reflection;
using UnityEngine;

namespace EasyEditor
{
	public class OnValueChangeAttribute : FormattingAttribute
	{
		readonly string _callbackMember;
		MethodInfo _methodInfo;
		int _parameterLength;
		bool _initialized;

		public OnValueChangeAttribute(string callbackName)
		{
			_callbackMember = callbackName;
			_initialized = false;
		}


		public void Initialize(object owner)
		{
			if (_initialized)
				return;

#if UNITY_EDITOR
			const BindingFlags binding = InspectorDrawingUtility.bindings;
			_methodInfo = owner.GetType().GetMethod(_callbackMember, binding);
			if (_methodInfo != null)
				_parameterLength = _methodInfo.GetParameters().Length;
			_initialized = true;
#endif
		}

		public void CallBack(object target, object oldValue, object newValue)
		{
			if (_methodInfo == null)
				Debug.LogWarning("No callback method");
			else if (_parameterLength == 0)
				_methodInfo.Invoke(target, Array.Empty<object>());
			else if (_parameterLength == 1)
				_methodInfo.Invoke(target, new[] { oldValue });
			else if (_parameterLength == 2)
				_methodInfo.Invoke(target, new[] { oldValue, newValue });
			else
				Debug.LogWarning($"Too many parameters  to the callback function: {_methodInfo.Name}");
		}
	}
}