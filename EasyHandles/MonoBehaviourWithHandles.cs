using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	public abstract class MonoBehaviourWithHandles : MonoBehaviour
	{
		public abstract void OnDrawHandles(HandleDrawer drawer); 
	}

	public sealed class HandleDrawer
	{
		static HandleDrawer instance;
		internal static HandleDrawer Instance
		{
			get
			{
				instance ??= new HandleDrawer();
				return instance;
			}
		}

		public Color Color
		{
#if UNITY_EDITOR
			get => Handles.color;
#else
		get => Color.white;
#endif


#if UNITY_EDITOR
			set => Handles.color = value;
#else
		set => Gizmos.color = value;
#endif

		}

		public Vector3 PositionHandle(Vector3 testPosition, Quaternion identity)
		{
#if UNITY_EDITOR
			return Handles.PositionHandle(testPosition, identity);
#endif
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(MonoBehaviourWithHandles), true)]
	public class HandleDrawerMonoBehaviourEditor : UnityEditor.Editor
	{
		void OnSceneGUI()
		{
			if (target is MonoBehaviour monoBehaviour)
			{
				Undo.RecordObject(monoBehaviour, "HandleChanged");

				if (target is MonoBehaviourWithHandles handleable)
				{ 
					handleable.OnDrawHandles(HandleDrawer.Instance);

					EasyHandles.ClearSettings();
				}
			}
		}
	}
#endif
}