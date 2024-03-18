using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	public class TagAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(TagAttribute))]
	public class TagAttributeDrawer : PropertyDrawer
	{ 
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				EasyMessageDrawer.DrawMessage(position, "", MessageType.Error);
				return;
			}

			property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
		} 
	}
#endif
}
