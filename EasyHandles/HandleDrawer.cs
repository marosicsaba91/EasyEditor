using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
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
}