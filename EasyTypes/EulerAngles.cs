using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyInspector
{



	public class EulerAngles : PropertyAttribute
	{

	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(EulerAngles))]
	public class FloatRangeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			object o = property?.GetObjectOfProperty();
			if (o == null || !(o is Quaternion quaternion))
				return;

			Vector3 eulerAngles = quaternion.eulerAngles;
			Vector3 newEuler = EditorGUI.Vector3Field(position, label, eulerAngles);

			if (eulerAngles != newEuler)
				property.SetValue(Quaternion.Euler(newEuler));
		}
	}
#endif

}