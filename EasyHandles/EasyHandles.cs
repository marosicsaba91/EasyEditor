using UnityEngine; 


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	public enum ForcedAxisMode
	{
		Non,
		Line,
		Plane
	}

	public static class EasyHandles
	{
		static readonly Color _focusedColorMultiplier = new(0.9f, 0.9f, 0.9f, 0.75f);
		static readonly Color _colorSelectedMultiplier = new(0.8f, 0.8f, 0.8f, 0.5f);
		static Color MultiplyColor(Color a, Color b) => new(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);

		public static float fullObjectSize = 1;
		public static float sizeMultiplier = 1;

		public static void PushMatrix(Matrix4x4 matrix) =>
#if UNITY_EDITOR
			Handles.matrix *= matrix;
#else
			DoNothing();
#endif

		public static void PopMatrix(Matrix4x4 matrix) =>
#if UNITY_EDITOR
			Handles.matrix *= matrix.inverse;
#else
			DoNothing();
#endif

		public static Matrix4x4 Matrix
		{
			get =>
#if UNITY_EDITOR
				Handles.matrix;
#else
				Matrix4x4.identity;
#endif
			set =>
#if UNITY_EDITOR
				Handles.matrix = value;
#else
				DoNothing();
#endif
		}
		public static Color Color
		{
			get =>
#if UNITY_EDITOR
				Handles.color;
#else
				Color.white;
#endif
			set =>
#if UNITY_EDITOR
				Handles.color = value;
#else
				DoNothing();
#endif
		}
		public static void Label(Vector3 position, string text)
		{
#if UNITY_EDITOR
			Handles.Label(position, text);
#endif
		}


		public static void Label(Vector3 position, string text, GUIStyle style)
		{
#if UNITY_EDITOR
			Handles.Label(position, text, style);
#endif
		}


		public static void ClearSettings()
		{
#if UNITY_EDITOR
			Handles.color = Color.white;
			Handles.matrix = Matrix4x4.identity;
#endif
			fullObjectSize = 1;
			sizeMultiplier = 1;
		}

		public static Vector3 PositionHandle(Vector3 position, HandleShape shape = HandleShape.Dot) =>
			PositionHandle(position, Quaternion.identity, shape);

		public static Vector3 PositionHandle(Vector3 position, Vector3 axis, HandleShape shape)
		{
			return PositionHandle(position, Quaternion.LookRotation(axis), ForcedAxisMode.Line, shape);
		}

		public static bool TryPositionHandle(Vector3 position, Vector3 axis, HandleShape shape, out Vector3 result)
		{
			result = PositionHandle(position, Quaternion.LookRotation(axis), ForcedAxisMode.Line, shape);
			return position != result;
		}

		public static Vector3 PositionHandle(Vector3 position, Vector3 axis, ForcedAxisMode mode = ForcedAxisMode.Line, HandleShape shape = HandleShape.Cone)
		{
			if (mode == ForcedAxisMode.Plane)
				return PositionHandle(position, Quaternion.LookRotation(axis), mode, shape);

			return PositionHandle(position, Quaternion.LookRotation(axis), mode, shape);
		}

		public static Vector3 PositionHandle(Vector3 position, Quaternion rotation, HandleShape shape = HandleShape.FullPosition)
		{
			return PositionHandle(position, rotation, ForcedAxisMode.Non, shape);
		}

		public static Quaternion RotationHandle(Vector3 position, Quaternion rotation)
		{
#if UNITY_EDITOR
			rotation = Handles.RotationHandle(rotation, position);
#endif
			return rotation;
		}

		public static void DrawLine(Vector3 start, Vector3 end)
		{
#if UNITY_EDITOR
			Handles.DrawLine(start, end);
#endif
		}

		// ----------- PRIVATE -------------

		static Vector3 PositionHandle(Vector3 position, Quaternion rotation, ForcedAxisMode mode, HandleShape shape)
		{
#if UNITY_EDITOR
			if (shape == HandleShape.FullPosition)
				position = Handles.PositionHandle(position, rotation);
			else
			{
				if (shape == HandleShape.SmallPosition)
					position = SmallPositionHandle(position, rotation, mode);
				else
				{
					Color color = Handles.color;
					Color selectedColor = MultiplyColor(color, _colorSelectedMultiplier);
					Color focusedColor = MultiplyColor(color, _focusedColorMultiplier);

					float offset = shape == HandleShape.Cone ? 0.5f : 0;
					position = DrawPositionHandle(
						position, rotation, shape, mode,
						color, selectedColor, focusedColor, offset);
				}
			}
#endif
			return position;
		}

		static Vector3 SmallPositionHandle(Vector3 position, Quaternion rotation, ForcedAxisMode mode)
		{
#if UNITY_EDITOR
			Color color = Handles.color;
#else
			Color color = Color.white;
#endif
			Vector3 Arrow(Vector3 dir, Color dirColor)
			{
				Quaternion rot = rotation * Quaternion.Euler(dir * 90);
				Color c = MultiplyColor(color, dirColor);
				Color sc = MultiplyColor(c, _colorSelectedMultiplier);
				Color fc = MultiplyColor(c, _focusedColorMultiplier);
				return DrawPositionHandle(position, rot, HandleShape.Cone, ForcedAxisMode.Line, c, sc, fc, 1.5f);
			}

			Color red = new(1, 0.5f, 0.5f);
			Color green = new(0.5f, 1, 0.5f);
			Color blue = new(0.5f, 0.5f, 1);

			Vector3 px = Arrow(Vector3.up, red);
			Vector3 py = Arrow(Vector3.left, green);
			Vector3 pz = Arrow(Vector3.forward, blue);
			Color selectedColor = MultiplyColor(color, _colorSelectedMultiplier);
			Color focusedColor = MultiplyColor(color, _focusedColorMultiplier);
			Vector3 p = DrawPositionHandle(position, rotation, HandleShape.Cube, mode, color, selectedColor, focusedColor);

			if (px != position)
				return px;
			if (py != position)
				return py;
			if (pz != position)
				return pz;
			return p;
		}

		static HandleEvent _lastEvent = HandleEvent.None;
		public static HandleEvent LastEvent => _lastEvent;

		static Vector3 DrawPositionHandle(
			Vector3 position, Quaternion rotation, HandleShape shape, ForcedAxisMode mode,
			Color color, Color focusedColor, Color selectedColor, float offset = 0)
		{
#if UNITY_EDITOR
			float size = 0.05f * Mathf.Abs(fullObjectSize);
			size *= sizeMultiplier;
			Handles.CapFunction capFunction = shape.ToCapFunction();
			size *= shape.GetSizeMultiplier();

			Vector3 inputPosition = position;

			if (offset != 0)
				position += rotation * Vector3.forward * (size * offset);

			HandleResult result = AdvancedHandles.Handle(
				position,
				rotation,
				size,
				capFunction,
				color,
				focusedColor, selectedColor);

			position = result.newPosition;
			_lastEvent = result.handleEvent;

			if (offset != 0)
				position += rotation * Vector3.back * (size * offset);

			if (mode == ForcedAxisMode.Line)
				position =
					Vector3.Dot(position - inputPosition, rotation * Vector3.forward) * (rotation * Vector3.forward)
					+ inputPosition;
			else if (mode == ForcedAxisMode.Plane)
				position =
					Vector3.ProjectOnPlane(position - inputPosition, rotation * Vector3.forward)
					+ inputPosition;


#endif
			return position;
		}
	}
}