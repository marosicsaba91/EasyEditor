using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	// Extra Features:
	// - Buttons & Properties Can Be drawn on the Inspector with th [EasyDraw] attribute

	public abstract class EasyScriptableObject : ScriptableObject { }
}

#if UNITY_EDITOR
namespace EasyEditor.Internal
{
	[CustomEditor(typeof(ScriptableObject), true), CanEditMultipleObjects]
	public class EasyScriptableObjectEditor : UnityEditor.Editor
	{
		readonly List<DrawableMember> _extraMembersToDraw = new();

		void OnEnable()
		{
			DrawableMemberHelper.FindDrawableMembers(target, serializedObject, _extraMembersToDraw);
		}
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DrawableMemberHelper.DrawExtraInspectorGUI(target, serializedObject, _extraMembersToDraw);
		}
	}

#endif
#if UNITY_EDITOR
#endif
}