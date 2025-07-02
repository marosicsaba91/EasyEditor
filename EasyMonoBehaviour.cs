using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	// Extra Features:
	// - Handles can be drawn in the scene view without the need for a custom editor.
	// - Buttons & Properties Can Be drawn on the Inspector with th [EasyDraw] attribute

	public abstract class EasyMonoBehaviour : MonoBehaviour
	{
		public virtual void OnDrawHandles() { }
	}
}

#if UNITY_EDITOR
namespace EasyEditor.Internal
{
	[CustomEditor(typeof(EasyMonoBehaviour), true), CanEditMultipleObjects]
	public class EasyMonoBehaviourEditor : UnityEditor.Editor
	{
		readonly List<DrawableMember> _extraMembersToDraw = new();
		public static bool extraUIDrawing;

		void OnSceneGUI()
		{
			if (target is MonoBehaviour monoBehaviour)
			{
				Undo.RecordObject(monoBehaviour, "HandleChanged");

				if (target is EasyMonoBehaviour handleable)
				{ 
					handleable.OnDrawHandles();
					EasyHandles.ClearSettings();
				}
			}
		}

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
}