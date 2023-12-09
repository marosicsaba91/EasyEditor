// ---------------------------------------------------------------------------- 
// Author: Kaynn, Yeo Wen Qin
// https://github.com/Kaynn-Cahya
// Date:   11/02/2019
// ----------------------------------------------------------------------------

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyInspector
{
	public class SortingLayerAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SortingLayerAttribute))]
	public class SortingLayerAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.Integer)
			{
				if (!_checkedType)
					PropertyTypeWarning(property);
				EditorGUI.PropertyField(position, property, label);
				return;
			}

			string[] SortingLayerNames = GetSortingLayerNames();
			HandleSortingLayerSelectionUI(position, property, label, SortingLayerNames);
		}

		bool _checkedType;

		void PropertyTypeWarning(SerializedProperty property)
		{
			Debug.LogWarning(string.Format("Property <color=brown>{0}</color> in object <color=brown>{1}</color> is of wrong type. Expected: Int",
				property.name, property.serializedObject.targetObject));
			_checkedType = true;
		}

		void HandleSortingLayerSelectionUI(Rect position, SerializedProperty property, GUIContent label, string[] SortingLayerNames)
		{
			EditorGUI.BeginProperty(position, label, property);

			// To show which sorting layer is currently selected.
			int currentSortingLayerIndex;
			bool layerFound = TryGetSortingLayerIndexFromProperty(out currentSortingLayerIndex, SortingLayerNames, property);

			if (!layerFound)
			{
				// Set to default layer. (Previous layer was removed)
				Debug.Log(string.Format(
					"Property <color=brown>{0}</color> in object <color=brown>{1}</color> is set to the default layer. Reason: previously selected layer was removed.",
					property.name, property.serializedObject.targetObject));
				property.intValue = 0;
				currentSortingLayerIndex = 0;
			}

			int selectedSortingLayerIndex = EditorGUI.Popup(position, label.text, currentSortingLayerIndex, SortingLayerNames);

			// Change property value if user selects a new sorting layer.
			if (selectedSortingLayerIndex != currentSortingLayerIndex)
			{
				property.intValue = SortingLayer.NameToID(SortingLayerNames[selectedSortingLayerIndex]);
			}

			EditorGUI.EndProperty();
		}

		#region Util

		bool TryGetSortingLayerIndexFromProperty(out int index, string[] SortingLayerNames, SerializedProperty property)
		{
			// To keep the property's value consistent, after the layers have been sorted around.
			string layerName = SortingLayer.IDToName(property.intValue);

			// Return the index where on it matches.
			for (int i = 0; i < SortingLayerNames.Length; ++i)
			{
				if (SortingLayerNames[i].Equals(layerName))
				{
					index = i;
					return true;
				}
			}

			// The current layer was removed.
			index = -1;
			return false;
		}

		string[] GetSortingLayerNames()
		{
			string[] result = new string[SortingLayer.layers.Length];

			for (int i = 0; i < result.Length; ++i)
				result[i] = SortingLayer.layers[i].name;

			return result;
		}

		#endregion
	}
#endif
}