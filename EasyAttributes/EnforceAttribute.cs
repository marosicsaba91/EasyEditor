using System;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	/// <summary>
	/// Use this to enforce a specific interface for a UnityEngine.Object field
	/// or to add a navigable search window to it.
	/// </summary>
	public class EnforceAttribute : PropertyAttribute
	{
		public Type enforcedType;

		public EnforceAttribute(Type enforcedType)
		{
			this.enforcedType = enforcedType;
		}
	}
}

#if UNITY_EDITOR

namespace EasyEditor.Internal
{
	[CustomPropertyDrawer(typeof(EnforceAttribute))]

	public class TypeCheckDrawer : PropertyDrawer
	{
		static ObjectSearchProvider searchProvider;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.ObjectReference)
			{
				UnityEngine.Object obj = property.objectReferenceValue;
				EnforceAttribute att = (EnforceAttribute)attribute;
				Type enforcedType = att.enforcedType;
				if (obj != null && enforcedType != null && !enforcedType.IsAssignableFrom(obj.GetType()))
				{
					GUI.color = EditorHelper.ErrorRedColor;
					Debug.LogWarning($"Object of type {obj.GetType()} does not match the expected type {enforcedType}.", property.serializedObject.targetObject);
				}

				EditorGUI.BeginChangeCheck();
				Rect full = position;
				Rect slice = position.SliceOut(20, Side.Right);

				if (GUI.Button(slice, GUIContent.none))
				{
					if (searchProvider == null)
						searchProvider = ScriptableObject.CreateInstance<ObjectSearchProvider>();

					searchProvider.Setup(enforcedType, property);
					SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), searchProvider);
					Event.current.Use();
				}
				obj = EditorGUI.ObjectField(full, label, obj, enforcedType, true);
				GUI.color = Color.white;

				if (EditorGUI.EndChangeCheck())
				{
					property.objectReferenceValue = obj;
					property.serializedObject.ApplyModifiedProperties();
				}

				GUI.color = Color.white;
			}
			else
			{
				EditorGUI.LabelField(position, label.text, "Use TypeCheck with ObjectReference type.");
			}
		}
	}
}
#endif