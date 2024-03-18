using System;
using UnityEngine;

namespace EasyEditor
{
	[Serializable]
	public class EasyCurve
	{
		[NonSerialized] public string functionName; 
		[NonSerialized] public bool useMemberNameAsLabel;
		public float functionZoom = 1;
		public Vector2 functionOffset = Vector2.zero;

		public EasyCurve(string functionName, bool useMemberNameAsLabel = false)
		{
			this.functionName = functionName;
			this.useMemberNameAsLabel = useMemberNameAsLabel;
		} 
	}
}