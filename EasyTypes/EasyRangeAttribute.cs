using System;
using UnityEngine;

namespace EasyEditor
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class EasyRangeAttribute : PropertyAttribute
	{
		public readonly float min;
		public readonly float max;

		public EasyRangeAttribute(float min, float max)
		{
			this.min = min;
			this.max = max;
		}
	}
}