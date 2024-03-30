using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	public abstract class MonoBehaviourWithHandles : MonoBehaviour
	{
		public abstract void OnDrawHandles();
		public virtual bool DrawHandlesInSelfSpace => false;
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
					if (handleable.DrawHandlesInSelfSpace)
						EasyHandles.PushMatrix(monoBehaviour.transform.localToWorldMatrix);

					handleable.OnDrawHandles();

					EasyHandles.ClearSettings();
				}
			}
		}
	}
#endif
}