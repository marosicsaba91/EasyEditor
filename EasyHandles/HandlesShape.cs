
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	public enum HandleShape
	{
		// 3D
		Sphere,
		Cube,
		Cone,
		Cylinder,

		// 2D
		Dot,
		EmptyCircle,
		EmptyRectangle,

		// Complex Gizmos
		FullPosition,
		SmallPosition,
	}

	public static class HandleShapeHelper
	{

#if UNITY_EDITOR
		public static Handles.CapFunction ToCapFunction(this HandleShape shape) => shape switch
		{
			// 3D
			HandleShape.Cube => Handles.CubeHandleCap,
			HandleShape.Sphere => Handles.SphereHandleCap,
			HandleShape.Cone => Handles.ConeHandleCap,
			HandleShape.Cylinder => Handles.CylinderHandleCap,
			// 2D
			HandleShape.EmptyCircle => Handles.CircleHandleCap,
			HandleShape.Dot => Handles.DotHandleCap,
			HandleShape.EmptyRectangle => Handles.RectangleHandleCap,
			_ => Handles.DotHandleCap,
		};
#endif

		public static float GetSizeMultiplier(this HandleShape shape) => shape switch
		{
			HandleShape.Cube => 1f,
			HandleShape.Sphere => 1f,
			HandleShape.Cone => 1.25f,
			HandleShape.Cylinder => 1f,
			HandleShape.EmptyCircle => 0.5f,
			HandleShape.EmptyRectangle or HandleShape.Dot => 0.5f,
			_ => 1,
		};
	}
}

