
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	public static partial class EasyHandles
	{
		public enum Shape
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

#if UNITY_EDITOR
		static Handles.CapFunction ToCapFunction(this Shape shape)
		{
			switch (shape)
			{
				// 3D
				case Shape.Cube:
					return Handles.CubeHandleCap;
				case Shape.Sphere:
					return Handles.SphereHandleCap;
				case Shape.Cone:
					return Handles.ConeHandleCap;

				case Shape.Cylinder:
					return Handles.CylinderHandleCap;

				// 2D
				case Shape.EmptyCircle:
					return Handles.CircleHandleCap;
				case Shape.Dot:
					return Handles.DotHandleCap;
				case Shape.EmptyRectangle:
					return Handles.RectangleHandleCap;
			}

			return Handles.DotHandleCap;
		}
#endif

		static float GetSizeMultiplier(this Shape shape)
		{
			switch (shape)
			{
				case Shape.Cube:
					return 1f;
				case Shape.Sphere:
					return 1f;
				case Shape.Cone:
					return 1.25f;
				case Shape.Cylinder:
					return 1f;
				case Shape.EmptyCircle:
					return 0.5f;
				case Shape.EmptyRectangle:
				case Shape.Dot:
					return 0.5f;
			}
			return 1;
		}
	}
}

