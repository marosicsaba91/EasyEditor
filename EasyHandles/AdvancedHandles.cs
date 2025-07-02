#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	public class AdvancedHandles
	{
		static readonly int _sDragHandleHash = "DragHandleHash".GetHashCode();
		static Vector2 _sDragHandleMouseStart;
		static Vector2 _sDragHandleMouseCurrent;
		static Vector3 _sDragHandleWorldStart;
		static float _sDragHandleClickTime = 0;
		static int _sDragHandleClickID;
		static readonly float _sDragHandleDoubleClickInterval = 0.25f;

		// externally accessible to get the ID of the most recently processed DragHandle
		public static int lastDragHandleID;

		public static HandleResult Handle(Vector3 position, Quaternion rotation, float handleSize, Handles.CapFunction capFunction, Color color, Color focusedColor, Color colorSelected)
		{
			int id = GUIUtility.GetControlID(_sDragHandleHash, FocusType.Passive);
			lastDragHandleID = id;


			Vector3 screenPosition = Handles.matrix.MultiplyPoint(position);
			Matrix4x4 cachedMatrix = Handles.matrix;

			HandleEvent handleEventType = HandleEvent.None;

			switch (Event.current.GetTypeForControl(id))
			{
				case EventType.MouseDown:
					if (HandleUtility.nearestControl == id && (Event.current.button == 0 || Event.current.button == 1))
					{
						GUIUtility.hotControl = id;
						_sDragHandleMouseCurrent = _sDragHandleMouseStart = Event.current.mousePosition;
						_sDragHandleWorldStart = position;

						Event.current.Use();
						EditorGUIUtility.SetWantsMouseJumping(wantz: 1);

						if (Event.current.button == 0)
							handleEventType = HandleEvent.LmbPress;
						else if (Event.current.button == 1)
							handleEventType = HandleEvent.RmbPress;
					}
					break;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == id && (Event.current.button == 0 || Event.current.button == 1))
					{
						GUIUtility.hotControl = 0;
						Event.current.Use();
						EditorGUIUtility.SetWantsMouseJumping(wantz: 0);

						if (Event.current.button == 0)
							handleEventType = HandleEvent.LmbRelease;
						else if (Event.current.button == 1)
							handleEventType = HandleEvent.RmbRelease;
						position = ReCalculatePosition();

						if (Event.current.mousePosition == _sDragHandleMouseStart)
						{
							bool doubleClick = (_sDragHandleClickID == id) &&
								(Time.realtimeSinceStartup - _sDragHandleClickTime < _sDragHandleDoubleClickInterval);

							_sDragHandleClickID = id;
							_sDragHandleClickTime = Time.realtimeSinceStartup;

							if (Event.current.button == 0)
								handleEventType = doubleClick ? HandleEvent.LmbDoubleClick : HandleEvent.LmbClick;
							else if (Event.current.button == 1)
								handleEventType = doubleClick ? HandleEvent.RmbDoubleClick : HandleEvent.RmbClick;
						}
					}
					break;

				case EventType.MouseDrag:
					if (GUIUtility.hotControl == id)
					{
						position = ReCalculatePosition();

						if (Event.current.button == 0)
							handleEventType = HandleEvent.LmbDrag;
						else if (Event.current.button == 1)
							handleEventType = HandleEvent.RmbDrag;

						GUI.changed = true;
						Event.current.Use();
					}
					break;

				case EventType.Repaint:
					Color currentColor = Handles.color;
					Handles.color = (id == GUIUtility.hotControl) ? colorSelected :
						HandleUtility.nearestControl == id ? focusedColor : color;


					Quaternion matrixR = cachedMatrix.rotation;
					matrixR.Normalize();
					matrixR *= rotation;

					Handles.matrix = Matrix4x4.identity;
					capFunction(id, screenPosition, matrixR, handleSize, EventType.Repaint);
					Handles.matrix = cachedMatrix;

					Handles.color = currentColor;
					break;

				case EventType.Layout:
					Handles.matrix = Matrix4x4.identity;
					HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(screenPosition, handleSize));
					Handles.matrix = cachedMatrix;
					break;
			}

			return new() { clickPosition = _sDragHandleWorldStart, newPosition = position, handleEvent = handleEventType };
		}

		static Vector3 ReCalculatePosition()
		{
			_sDragHandleMouseCurrent += new Vector2(Event.current.delta.x, -Event.current.delta.y);
			Vector3 position2 = Camera.current.WorldToScreenPoint(Handles.matrix.MultiplyPoint(_sDragHandleWorldStart))
				+ (Vector3)(_sDragHandleMouseCurrent - _sDragHandleMouseStart);
			Vector3 position = Handles.matrix.inverse.MultiplyPoint(Camera.current.ScreenToWorldPoint(position2));

			if (Camera.current.transform.forward == Vector3.forward || Camera.current.transform.forward == -Vector3.forward)
				position.z = _sDragHandleWorldStart.z;
			if (Camera.current.transform.forward == Vector3.up || Camera.current.transform.forward == -Vector3.up)
				position.y = _sDragHandleWorldStart.y;
			if (Camera.current.transform.forward == Vector3.right || Camera.current.transform.forward == -Vector3.right)
				position.x = _sDragHandleWorldStart.x;
			return position;
		}
	}
}
#endif