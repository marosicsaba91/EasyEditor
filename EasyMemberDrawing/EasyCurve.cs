using System;
using UnityEngine;

namespace EasyEditor
{
	[Serializable]
	public class EasyCurve
	{
		[NonSerialized] public string functionName;
		[NonSerialized] public bool useMemberNameAsLabel; 
		[NonSerialized] public bool fixPosition;
		[NonSerialized] public Rect fixRect;
		public float functionZoom = 1;
		public Vector2 functionOffset = Vector2.zero;

		public EasyCurve(string functionName, bool useMemberNameAsLabel = false)
		{
			this.functionName = functionName;
			this.useMemberNameAsLabel = useMemberNameAsLabel;
			fixRect = new();
			fixPosition = false;
		}
		public EasyCurve(string functionName, Rect fixRect, bool useMemberNameAsLabel = false)
		{
			this.functionName = functionName;
			this.useMemberNameAsLabel = useMemberNameAsLabel;
			this.fixRect = fixRect;
			fixPosition = true;
		} 
	}
}