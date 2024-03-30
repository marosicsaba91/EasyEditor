using UnityEngine;

namespace EasyEditor
{
	public struct HandleResult
	{
		public HandleEvent handleEvent;
		public Vector3 newPosition;
		public Vector3 clickPosition;
		public Vector3 dragPosition => newPosition - clickPosition;
	}
}