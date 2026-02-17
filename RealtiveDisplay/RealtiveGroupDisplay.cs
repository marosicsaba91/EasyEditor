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
		MonoBehaviour _container;
		RealtiveGroupDisplay _target;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_container == null)
			{
				EditorGUI.LabelField(position, label.text, "RealtiveGroupDisplay should be in a MonoBehaviour.");
				return;
			}
			if (_target == null)
			{
				EditorGUI.LabelField(position, label.text, "Something went wrong!");
				return;
			}

			Rect headerRect = position.SliceOutLine();
			property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label.text);
			Rect headerMessageRect = headerRect.SliceOut(EditorHelper.ContentWidth(position), Side.Right);
			Rect researchButtonRect = headerMessageRect.SliceOut(70, Side.Right);
			string headerMessage = $"Found: {_target.relatives.Length} {_target.relativeTpye} as {_target.relativeCategory}";
			GUI.Label(headerMessageRect, headerMessage);
			if (GUI.Button(researchButtonRect, "Research"))
			{
				_target.SetRelatives(_container, true);
				property.serializedObject.ApplyModifiedProperties();
			}
			if (!property.isExpanded)
				return;

			GUI.enabled = false;
			position.SliceOut(EditorHelper.LabelWidth, Side.Left);
			for (int i = 0; i < _target.relatives.Length; i++)
			{
				Rect relativeRect = position.SliceOutLine();
				EditorGUI.ObjectField(relativeRect, _target.relatives[i], _target.relativeTpye, true);
			}
			GUI.enabled = true;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			_container = property.GetObjectWithProperty() as MonoBehaviour;
			_target = property.GetObjectOfProperty() as RealtiveGroupDisplay;

			if (_container == null)
				return EditorGUIUtility.singleLineHeight;

			if (_target == null)
				return EditorGUIUtility.singleLineHeight;

			_target.SetRelatives(_container, false);
			if (_target.relatives == null || _target.relatives.Length == 0 || !property.isExpanded)
				return EditorGUIUtility.singleLineHeight;

			return EditorGUIUtility.singleLineHeight * (_target.relatives.Length + 1);
		}
	}

}
#endif