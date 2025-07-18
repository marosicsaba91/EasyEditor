using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	public enum RelativeCategory
	{
		Child, Parent, Component
	}

	/// <summary>
	/// Display a group of relatives (Children, Parents, Components on the same GameObject) of a MonoBehaviour in the inspector.
	/// </summary>

	[Serializable]
	public class RealtiveGroupDisplay
	{
		internal readonly RelativeCategory relativeCategory;
		internal readonly Type relativeTpye;
		internal Component[] relatives;

		public RealtiveGroupDisplay(RelativeCategory relativeCategory, Type relativeTpye)
		{
			this.relativeCategory = relativeCategory;
			this.relativeTpye = relativeTpye;
		}

		internal void SetRelatives(MonoBehaviour self, bool force)
		{
			if (relatives == null || relatives.Length == 0 || force)
				relatives = relativeCategory switch
				{
					RelativeCategory.Child => self.GetComponentsInChildren(relativeTpye, true),
					RelativeCategory.Parent => self.GetComponentsInParent(relativeTpye, true),
					RelativeCategory.Component => self.GetComponents(relativeTpye),
					_ => throw new ArgumentOutOfRangeException(nameof(relativeCategory), relativeCategory, null)
				};
		}
	}
}


#if UNITY_EDITOR
namespace EasyEditor.Internal
{
	[CustomPropertyDrawer(typeof(RealtiveGroupDisplay))]
	public class RelativeGroupDisplayDrawer : PropertyDrawer
	{
		MonoBehaviour container;
		RealtiveGroupDisplay target;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (container == null)
			{
				EditorGUI.LabelField(position, label.text, "RealtiveGroupDisplay should be in a MonoBehaviour.");
				return;
			}
			if (target == null)
			{
				EditorGUI.LabelField(position, label.text, "Something went wrong!");
				return;
			}

			Rect headerRect = position.SliceOutLine();
			property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label.text);
			Rect headerMessageRect = headerRect.SliceOut(EditorHelper.ContentWidth(position), Side.Right);
			Rect researchButtonRect = headerMessageRect.SliceOut(70, Side.Right);
			string headerMessage = $"Found: {target.relatives.Length} {target.relativeTpye} as {target.relativeCategory}";
			GUI.Label(headerMessageRect, headerMessage);
			if (GUI.Button(researchButtonRect, "Research"))
			{
				target.SetRelatives(container, true);
				property.serializedObject.ApplyModifiedProperties();
			}
			if (!property.isExpanded)
				return;

			GUI.enabled = false;
			position.SliceOut(EditorHelper.LabelWidth, Side.Left);
			for (int i = 0; i < target.relatives.Length; i++)
			{
				Rect relativeRect = position.SliceOutLine();
				EditorGUI.ObjectField(relativeRect, target.relatives[i], target.relativeTpye, true);
			}
			GUI.enabled = true;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			container = property.GetObjectWithProperty() as MonoBehaviour;
			target = property.GetObjectOfProperty() as RealtiveGroupDisplay;

			if (container == null)
				return EditorGUIUtility.singleLineHeight;

			if (target == null)
				return EditorGUIUtility.singleLineHeight;

			target.SetRelatives(container, false);
			if (target.relatives == null || target.relatives.Length == 0 || !property.isExpanded)
				return EditorGUIUtility.singleLineHeight;

			return EditorGUIUtility.singleLineHeight * (target.relatives.Length + 1);
		}
	}

}
#endif